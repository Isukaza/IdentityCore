using System;
using System.Threading.Tasks;

using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.DAL.PostgreSQL.Roles;

using IdentityCore.Managers;
using IdentityCore.Managers.Interfaces;

using IdentityCore.Tests.Mocks;
using Moq;
using NUnit.Framework;

namespace IdentityCore.Tests.Managers;

public class AuthenticationManagerTest
{
    private IAuthenticationManager _authManager;
    private Mock<IRefreshTokenManager> _refTokenManager;
    private Mock<IRefreshTokenDbRepository> _refTokenDbRepo;

    [SetUp]
    public void SetUp()
    {
        var mockConfiguration = Mocks.MockFactory.GetMockConfiguration();
        MockSettings.InitializeAllConfigurations(mockConfiguration);
        
        _refTokenManager = new Mock<IRefreshTokenManager>();
        _refTokenDbRepo = new Mock<IRefreshTokenDbRepository>();

        _authManager = new AuthenticationManager(_refTokenDbRepo.Object, _refTokenManager.Object);
    }

    #region CreateLoginTokensAsyncTest

    [Test]
    public async Task CreateLoginTokensAsync_ValidUser_CreatesTokensSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var refreshToken = new RefreshToken
        {
            RefToken = "valid_refresh_token",
            Expires = default
        };

        _refTokenManager
            .Setup(manager => manager.CreateRefreshToken(user))
            .Returns(refreshToken);

        _refTokenManager
            .Setup(manager => manager.AddTokenAsync(user, refreshToken))
            .ReturnsAsync(true);

        // Act
        var result = await _authManager.CreateLoginTokensAsync(user);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected non-null result.");
        Assert.That(result.Success, Is.True, "Expected operation to be successful.");
        Assert.That(result.Data, Is.Not.Null, "Expected LoginResponse to be non-null.");
        Assert.That(result.Data.UserId, Is.EqualTo(user.Id), "User ID does not match.");
        Assert.That(result.Data.RefreshToken, Is.EqualTo(refreshToken.RefToken), "Refresh token does not match.");
        Assert.That(result.Data.Bearer, Is.Not.Null.Or.Empty, "Bearer token should not be null or empty.");
    }

    [Test]
    public async Task CreateLoginTokensAsync_NullUser_ReturnsErrorResult()
    {
        // Act
        var result = await _authManager.CreateLoginTokensAsync(null);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected non-null result.");
        Assert.That(result.Success, Is.False, "Expected operation to fail.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid input data"), "Expected error message.");
    }

    [Test]
    public async Task CreateLoginTokensAsync_RefreshTokenCreationFails_ReturnsErrorResult()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        _refTokenManager
            .Setup(manager => manager.CreateRefreshToken(user))
            .Returns((RefreshToken)null);

        // Act
        var result = await _authManager.CreateLoginTokensAsync(user);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected result to be non-null.");
        Assert.That(result.Success, Is.False, "Expected the result to be unsuccessful.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Error creating session"),
            "Expected error message to be 'Error creating session'.");
    }

    [Test]
    public async Task CreateLoginTokensAsync_TokenAdditionFails_ReturnsErrorResult()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var refreshToken = new RefreshToken
        {
            RefToken = "valid_refresh_token",
            Expires = default
        };

        _refTokenManager
            .Setup(manager => manager.CreateRefreshToken(user))
            .Returns(refreshToken);

        _refTokenManager
            .Setup(manager => manager.AddTokenAsync(user, refreshToken))
            .ReturnsAsync(false);

        // Act
        var result = await _authManager.CreateLoginTokensAsync(user);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected non-null result.");
        Assert.That(result.Success, Is.False, "Expected operation to fail.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Error creating session"), "Expected error message.");
    }

    #endregion

    #region RefreshLoginTokensAsyncTest

    [Test]
    public async Task RefreshLoginTokensAsync_SuccessfulTokenRefresh_ReturnsLoginResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var token = new RefreshToken
        {
            UserId = userId,
            RefToken = null,
            Expires = default,
            User = user
        };

        const string updatedTokenValue = "new_refresh_token";

        _refTokenManager
            .Setup(manager => manager.UpdateTokenDbAsync(token))
            .ReturnsAsync(updatedTokenValue);

        // Act
        var result = await _authManager.RefreshLoginTokensAsync(token);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.Success, Is.True, "Success should be true for successful token refresh");
        Assert.That(result.Data, Is.Not.Null, "Data in the result should not be null");
        Assert.That(result.Data.UserId, Is.EqualTo(userId),
            "UserId in the response should match the original token's UserId");
        Assert.That(result.Data.RefreshToken, Is.EqualTo(updatedTokenValue),
            "RefreshToken in the response should match the updated token value");
    }

    [Test]
    public async Task RefreshLoginTokensAsync_NullToken_ReturnsErrorResult()
    {
        // Act
        var result = await _authManager.RefreshLoginTokensAsync(null);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected result to be non-null.");
        Assert.That(result.Success, Is.False, "Expected the result to be unsuccessful.");
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid operation"),
            "Expected error message to be 'Invalid operation'.");
    }

    [Test]
    public async Task RefreshLoginTokensAsync_TokenUpdateFails_ReturnsErrorResult()
    {
        // Arrange
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            RefToken = null,
            Expires = default
        };

        _refTokenManager
            .Setup(manager => manager.UpdateTokenDbAsync(token))
            .ReturnsAsync((string)null);

        // Act
        var result = await _authManager.RefreshLoginTokensAsync(token);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.Success, Is.False, "Success should be false when token update fails");
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid operation"),
            "Error message should indicate invalid operation");
    }

    #endregion

    #region LogoutAsyncTest

    [Test]
    public async Task LogoutAsync_SuccessfulLogout_ReturnsEmptyString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string refreshToken = "some_refresh_token";
        var token = new RefreshToken
        {
            UserId = userId,
            RefToken = refreshToken,
            Expires = default
        };

        _refTokenDbRepo
            .Setup(repo => repo.GetTokenByUserIdAsync(userId, refreshToken))
            .ReturnsAsync(token);
        
        _refTokenDbRepo
            .Setup(repo => repo.DeleteAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _authManager.LogoutAsync(userId, refreshToken);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty), "Expected empty string when logout is successful");
    }
    
    [Test]
    public async Task LogoutAsync_EmptyRefreshToken_ReturnsInvalidTokenMessage()
    {
        // Act
        var result = await _authManager.LogoutAsync(Guid.NewGuid(), string.Empty);

        // Assert
        Assert.That(result, Is.EqualTo("Invalid refresh token"), "Expected message for empty refresh token");
    }

    [Test]
    public async Task LogoutAsync_NullRefreshToken_ReturnsInvalidTokenMessage()
    {
        // Act
        var result = await _authManager.LogoutAsync(Guid.NewGuid(), null);

        // Assert
        Assert.That(result, Is.EqualTo("Invalid refresh token"), "Expected message for null refresh token");
    }

    [Test]
    public async Task LogoutAsync_UserNotFound_ReturnsUserNotFoundMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string refreshToken = "some_refresh_token";

        _refTokenDbRepo
            .Setup(repo => repo.GetTokenByUserIdAsync(userId, refreshToken))
            .ReturnsAsync((RefreshToken)null);

        // Act
        var result = await _authManager.LogoutAsync(userId, refreshToken);

        // Assert
        Assert.That(result, Is.EqualTo("The user was not found or was deleted"),
            "Expected message when user not found");
    }

    [Test]
    public async Task LogoutAsync_DeleteFails_ReturnsDeletionErrorMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string refreshToken = "some_refresh_token";
        var token = new RefreshToken
        {
            UserId = userId,
            RefToken = refreshToken,
            Expires = default
        };

        _refTokenDbRepo
            .Setup(repo => repo.GetTokenByUserIdAsync(userId, refreshToken))
            .ReturnsAsync(token);

        _refTokenDbRepo.Setup(repo => repo.DeleteAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _authManager.LogoutAsync(userId, refreshToken);

        // Assert
        Assert.That(result, Is.EqualTo("Error during deletion"), "Expected error message when deletion fails");
    }

    #endregion
}