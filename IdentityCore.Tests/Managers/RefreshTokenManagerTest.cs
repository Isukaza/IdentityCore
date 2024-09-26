using System;
using System.Threading.Tasks;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers;
using IdentityCore.Managers.Interfaces;
using Moq;
using NUnit.Framework;

namespace IdentityCore.Tests.Managers;

public class RefreshTokenManagerTest
{
    private IRefreshTokenManager _refreshTokenManager;
    private Mock<IRefreshTokenDbRepository> _refTokenDbRepo;

    [SetUp]
    public void SetUp()
    {
        var mockConfiguration = Mocks.MockFactory.GetMockConfiguration();
        ConfigBase.SetConfiguration(mockConfiguration);

        _refTokenDbRepo = new Mock<IRefreshTokenDbRepository>();
        _refreshTokenManager = new RefreshTokenManager(_refTokenDbRepo.Object);
    }

    #region CreateRefreshTokenTest

    [Test]
    public void CreateRefreshToken_ValidUser_ReturnsRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testUser",
            Email = "test@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        // Act
        var result = _refreshTokenManager.CreateRefreshToken(user);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected result to be non-null.");
        Assert.That(result.RefToken, Is.Not.Null.Or.Empty, "Expected refresh token value to be non-null or non-empty.");
        Assert.That(result.Expires, Is.GreaterThan(DateTime.UtcNow),
            "Expected the expiration date to be in the future.");
        Assert.That(result.User, Is.EqualTo(user), "Expected the user in the refresh token to match the input user.");
    }

    [Test]
    public void CreateRefreshToken_NullUser_ReturnsNull()
    {
        // Act
        var result = _refreshTokenManager.CreateRefreshToken(null);

        // Assert
        Assert.That(result, Is.Null, "Expected result to be null when user is null.");
    }

    #endregion

    #region AddTokenAsyncTest

    [Test]
    public async Task AddTokenAsync_ValidUserAndToken_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testUser",
            Email = "test@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var refreshToken = new RefreshToken
        {
            RefToken = "validRefreshToken",
            Expires = DateTime.UtcNow.AddHours(1),
            User = user
        };

        _refTokenDbRepo
            .Setup(repo => repo.GetCountUserTokensAsync(user.Id))
            .ReturnsAsync(0);

        _refTokenDbRepo
            .Setup(repo => repo.CreateAsync(refreshToken))
            .ReturnsAsync(refreshToken);

        // Act
        var result = await _refreshTokenManager.AddTokenAsync(user, refreshToken);

        // Assert
        Assert.That(result, Is.True, "Expected AddTokenAsync to return true for valid user and token.");
    }

    [Test]
    public async Task AddTokenAsync_NullUser_ReturnsFalse()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            RefToken = "validRefreshToken",
            Expires = default
        };

        // Act
        var result = await _refreshTokenManager.AddTokenAsync(null, refreshToken);

        // Assert
        Assert.That(result, Is.False, "Expected AddTokenAsync to return false when user is null.");
    }

    [Test]
    public async Task AddTokenAsync_NullToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testUser",
            Email = "test@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        // Act
        var result = await _refreshTokenManager.AddTokenAsync(user, null);

        // Assert
        Assert.That(result, Is.False, "Expected AddTokenAsync to return false when refresh token is null.");
    }

    [Test]
    public async Task AddTokenAsync_UserExceedsMaxSessions_RemovesOldTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testUser",
            Email = "test@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var refreshToken = new RefreshToken
        {
            RefToken = "validRefreshToken",
            Expires = DateTime.UtcNow.AddHours(1),
            User = user
        };

        _refTokenDbRepo
            .Setup(repo => repo.GetCountUserTokensAsync(user.Id))
            .ReturnsAsync(RefToken.Configs.MaxSessions);

        _refTokenDbRepo
            .Setup(repo => repo.DeleteOldestSessionAsync(user.Id))
            .Returns(Task.CompletedTask);

        _refTokenDbRepo
            .Setup(repo => repo.CreateAsync(refreshToken))
            .ReturnsAsync(refreshToken);

        // Act
        var result = await _refreshTokenManager.AddTokenAsync(user, refreshToken);

        // Assert
        Assert.That(result, Is.True, "Expected AddTokenAsync to return true after removing oldest tokens.");
        _refTokenDbRepo.Verify(repo => repo.DeleteOldestSessionAsync(user.Id), Times.Once,
            "Expected oldest session to be deleted.");
    }

    #endregion

    #region UpdateTokenDbAsyncTest

    [Test]
    public async Task UpdateTokenDbAsync_ValidToken_ReturnsUpdatedTokenRef()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            RefToken = "oldRefreshToken",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _refTokenDbRepo
            .Setup(repo => repo.UpdateAsync(refreshToken))
            .ReturnsAsync(true);

        // Act
        var result = await _refreshTokenManager.UpdateTokenDbAsync(refreshToken);

        // Assert
        Assert.That(result, Is.Not.Empty, "Expected a non-empty refresh token to be returned.");
        Assert.That(result, Is.EqualTo(refreshToken.RefToken),
            "Expected the updated refresh token to match the token's reference.");
    }

    [Test]
    public async Task UpdateTokenDbAsync_NullToken_ReturnsEmptyString()
    {
        // Act
        var result = await _refreshTokenManager.UpdateTokenDbAsync(null);

        // Assert
        Assert.That(result, Is.Empty, "Expected an empty string when a null token is provided.");
    }

    [Test]
    public async Task UpdateTokenDbAsync_UpdateFails_ReturnsEmptyString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            RefToken = "oldRefreshToken",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _refTokenDbRepo
            .Setup(repo => repo.UpdateAsync(refreshToken))
            .ReturnsAsync(false);

        // Act
        var result = await _refreshTokenManager.UpdateTokenDbAsync(refreshToken);

        // Assert
        Assert.That(result, Is.Empty, "Expected an empty string when the update fails.");
    }

    #endregion

    #region ValidationRefreshTokenAsyncTest

    [Test]
    public async Task ValidationRefreshTokenAsync_ValidToken_ReturnsToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            RefToken = "validToken",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _refTokenDbRepo
            .Setup(repo => repo.GetTokenByUserIdAsync(userId, refreshToken.RefToken))
            .ReturnsAsync(refreshToken);

        // Act
        var result = await _refreshTokenManager.ValidationRefreshTokenAsync(userId, refreshToken.RefToken);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected a non-null result.");
        Assert.That(result.Success, Is.True, "Expected the operation to be successful.");
        Assert.That(result.Data, Is.EqualTo(refreshToken), "Expected the returned token to match the provided token.");
    }

    [Test]
    public async Task ValidationRefreshTokenAsync_NullToken_ReturnsInvalidInputData()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _refreshTokenManager.ValidationRefreshTokenAsync(userId, null);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected a non-null result.");
        Assert.That(result.Success, Is.False, "Expected the operation to be unsuccessful.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid input data"),
            "Expected the error message to indicate invalid input data.");
    }

    [Test]
    public async Task ValidationRefreshTokenAsync_TokenNotFound_ReturnsInvalidInputData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string token = "nonExistingToken";

        _refTokenDbRepo
            .Setup(repo => repo.GetTokenByUserIdAsync(userId, token))
            .ReturnsAsync((RefreshToken)null);

        // Act
        var result = await _refreshTokenManager.ValidationRefreshTokenAsync(userId, token);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected a non-null result.");
        Assert.That(result.Success, Is.False, "Expected the operation to be unsuccessful.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid input data"),
            "Expected the error message to indicate invalid input data.");
    }

    [Test]
    public async Task ValidationRefreshTokenAsync_TokenExpired_ReturnsTokenExpiredMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            RefToken = "expiredToken",
            Expires = DateTime.UtcNow.AddHours(-1)
        };

        _refTokenDbRepo
            .Setup(repo => repo.GetTokenByUserIdAsync(userId, refreshToken.RefToken))
            .ReturnsAsync(refreshToken);

        _refTokenDbRepo
            .Setup(repo => repo.DeleteAsync(refreshToken))
            .ReturnsAsync(true);

        // Act
        var result = await _refreshTokenManager.ValidationRefreshTokenAsync(userId, refreshToken.RefToken);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected a non-null result.");
        Assert.That(result.Success, Is.False, "Expected the operation to be unsuccessful.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Token expired"),
            "Expected the error message to indicate that the token has expired.");
    }

    #endregion
}