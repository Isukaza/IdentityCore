using System;
using System.Threading.Tasks;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.Managers;
using IdentityCore.Models.Request;

using Moq;
using NUnit.Framework;

namespace IdentityCore.Tests.Managers;

[TestFixture]
public class CfmTokenManagerTests
{
    private Mock<ICfmTokenCacheRepository> _mockCacheRepo;
    private CfmTokenManager _tokenManager;

    [SetUp]
    public void SetUp()
    {
        var mockConfiguration = Mocks.MockFactory.GetMockConfiguration();
        ConfigBase.SetConfiguration(mockConfiguration);

        _mockCacheRepo = new Mock<ICfmTokenCacheRepository>();
        _tokenManager = new CfmTokenManager(_mockCacheRepo.Object);
    }

    #region GetTokenAsyncTest

    [Test]
    public async Task GetTokenAsync_ValidTokenAndType_ReturnsToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenValue = UserHelper.GetToken(userId);
        const TokenType tokenType = TokenType.RegistrationConfirmation;
        var expectedToken = new RedisConfirmationToken
        {
            Value = tokenValue,
            TokenType = tokenType,
            UserId = Guid.NewGuid()
        };

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<RedisConfirmationToken>(tokenValue, tokenType))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _tokenManager.GetTokenAsync(tokenValue, tokenType);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected a non-null token.");
        Assert.That(result.Value, Is.EqualTo(expectedToken.Value), "Token value does not match.");
        Assert.That(result.TokenType, Is.EqualTo(expectedToken.TokenType), "Token type does not match.");
    }

    [Test]
    public async Task GetTokenAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        const string tokenValue = "invalidToken";
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<RedisConfirmationToken>(tokenValue, tokenType))
            .ReturnsAsync((RedisConfirmationToken)null);

        // Act
        var result = await _tokenManager.GetTokenAsync(tokenValue, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected null result for invalid token.");
    }

    [Test]
    public async Task GetTokenAsync_UnknownTokenType_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenValue = UserHelper.GetToken(userId);
        const TokenType tokenType = TokenType.Unknown;

        // Act
        var result = await _tokenManager.GetTokenAsync(tokenValue, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected null result for unknown token type.");
    }

    [Test]
    public async Task GetTokenAsync_ValidTokenButTypeMismatch_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenValue = UserHelper.GetToken(userId);
        const TokenType tokenType = TokenType.EmailChangeNew;
        var expectedToken = new RedisConfirmationToken
        {
            Value = tokenValue,
            TokenType = TokenType.RegistrationConfirmation,
            UserId = userId
        };

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<RedisConfirmationToken>(tokenValue, tokenType))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _tokenManager.GetTokenAsync(tokenValue, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected null result for type mismatch.");
    }

    #endregion

    #region GetTokenByUserIdAsyncTest

    [Test]
    public async Task GetTokenByUserIdAsync_ValidUserIdAndTokenType_ReturnsToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenValue = UserHelper.GetToken(userId);
        const TokenType tokenType = TokenType.RegistrationConfirmation;
        var expectedToken = new RedisConfirmationToken
        {
            Value = tokenValue,
            TokenType = tokenType,
            UserId = userId
        };

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<string>(userId.ToString(), tokenType))
            .ReturnsAsync(tokenValue);
        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<RedisConfirmationToken>(tokenValue, tokenType))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _tokenManager.GetTokenByUserIdAsync(userId, tokenType);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected a non-null token.");
        Assert.That(result.Value, Is.EqualTo(expectedToken.Value), "Token value does not match.");
        Assert.That(result.TokenType, Is.EqualTo(expectedToken.TokenType), "Token type does not match.");
        Assert.That(result.UserId, Is.EqualTo(expectedToken.UserId), "User ID does not match.");
    }

    [Test]
    public async Task GetTokenByUserIdAsync_InvalidUserId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<string>(userId.ToString(), tokenType))
            .ReturnsAsync((string)null);

        // Act
        var result = await _tokenManager.GetTokenByUserIdAsync(userId, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected null result for invalid user ID.");
    }

    [Test]
    public async Task GetTokenByUserIdAsync_ValidUserIdButInvalidToken_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string tokenValue = "invalidToken";
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<string>(userId.ToString(), tokenType))
            .ReturnsAsync(tokenValue);
        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<RedisConfirmationToken>(tokenValue, tokenType))
            .ReturnsAsync((RedisConfirmationToken)null);

        // Act
        var result = await _tokenManager.GetTokenByUserIdAsync(userId, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected null result for valid user ID but invalid token.");
    }

    [Test]
    public async Task GetTokenByUserIdAsync_ValidUserIdButEmptyTokenValue_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo
            .Setup(repo => repo.GetTokenByTokenType<string>(userId.ToString(), tokenType))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _tokenManager.GetTokenByUserIdAsync(userId, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected null result for valid user ID but empty token value.");
    }

    #endregion

    #region GetNextAttemptTimeTest

    [Test]
    public void GetNextAttemptTime_NullToken_ReturnsInvalidToken()
    {
        // Act
        var result = _tokenManager.GetNextAttemptTime(null);

        // Assert
        Assert.That(result, Is.EqualTo("Invalid token"), "Expected 'Invalid token' when token is null");
    }

    [Test]
    public void GetNextAttemptTime_TokenWithinMinInterval_ReturnsEmptyString()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            UserId = Guid.NewGuid(),
            Value = "token",
            TokenType = TokenType.RegistrationConfirmation,
            AttemptCount = 1,
            Modified = DateTime.UtcNow.Add(-MailConfig.Values.MinIntervalBetweenAttempts.Add(TimeSpan.FromMinutes(1)))
        };

        // Act
        var result = _tokenManager.GetNextAttemptTime(token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "Expected empty string when token is within the minimum interval");
    }

    [Test]
    public void GetNextAttemptTime_TokenBelowMaxAttemptsButPastMinInterval_ReturnsEmptyString()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            UserId = Guid.NewGuid(),
            Value = "token",
            TokenType = TokenType.EmailChangeOld,
            AttemptCount = 1,
            Modified = DateTime.UtcNow.Add(-MailConfig.Values.MinIntervalBetweenAttempts).Add(-TimeSpan.FromMinutes(1))
        };

        // Act
        var result = _tokenManager.GetNextAttemptTime(token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "Expected empty string when token is below max attempts but past min interval");
    }

    [Test]
    public void GetNextAttemptTime_TokenAtMaxAttemptsButWithinNextAttemptInterval_ReturnsNextAvailableTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var token = new RedisConfirmationToken
        {
            UserId = Guid.NewGuid(),
            Value = "token",
            TokenType = TokenType.PasswordChange,
            AttemptCount = MailConfig.Values.MaxAttemptsConfirmationResend,
            Modified = now.Add(MailConfig.Values.NextAttemptAvailableAfter)
        };

        // Act
        var result = DateTime.Parse(_tokenManager.GetNextAttemptTime(token)).ToUniversalTime();

        // Assert
        var expectedTime = token.Modified.Add(MailConfig.Values.NextAttemptAvailableAfter);
        var difference = Math.Abs((expectedTime - result).TotalSeconds);

        Assert.That(difference <= 1, Is.True,
            "Expected next available time when token is at max attempts but within next attempt interval");
    }

    [Test]
    public void GetNextAttemptTime_TokenAtMaxAttemptsAndPastNextAttemptInterval_ReturnsEmptyString()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            UserId = Guid.NewGuid(),
            Value = "token",
            TokenType = TokenType.UsernameChange,
            AttemptCount = MailConfig.Values.MaxAttemptsConfirmationResend,
            Modified = DateTime.UtcNow.Add(-MailConfig.Values.NextAttemptAvailableAfter)
        };

        // Act
        var result = _tokenManager.GetNextAttemptTime(token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "Expected empty string when token is at max attempts and past next attempt interval");
    }

    #endregion

    #region DeleteTokenAsyncTest

    [Test]
    public async Task DeleteTokenAsync_TokenIsDeletedSuccessfully_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = userId,
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation
        };

        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.Value, token.TokenType))
            .ReturnsAsync(true);
        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.UserId.ToString(), token.TokenType))
            .ReturnsAsync(true);

        // Act
        var result = await _tokenManager.DeleteTokenAsync(token);

        // Assert
        Assert.That(result, Is.True, "Expected successful deletion of token.");
    }

    [Test]
    public async Task DeleteTokenAsync_TokenIsNull_ReturnsFalse()
    {
        // Act
        var result = await _tokenManager.DeleteTokenAsync(null);

        // Assert
        Assert.That(result, Is.False, "Expected false result when token is null.");
    }

    [Test]
    public async Task DeleteTokenAsync_FailedToDeleteToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = userId,
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation
        };

        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.Value, token.TokenType))
            .ReturnsAsync(false);
        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.UserId.ToString(), token.TokenType))
            .ReturnsAsync(true);

        // Act
        var result = await _tokenManager.DeleteTokenAsync(token);

        // Assert
        Assert.That(result, Is.False, "Expected false result when deletion of token fails.");
    }

    [Test]
    public async Task DeleteTokenAsync_FailedToDeleteUserId_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = userId,
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation
        };

        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.Value, token.TokenType))
            .ReturnsAsync(true);
        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.UserId.ToString(), token.TokenType))
            .ReturnsAsync(false);

        // Act
        var result = await _tokenManager.DeleteTokenAsync(token);

        // Assert
        Assert.That(result, Is.False, "Expected false result when deletion of user ID fails.");
    }

    [Test]
    public async Task DeleteTokenAsync_TokenDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = userId,
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation
        };

        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.Value, token.TokenType))
            .ReturnsAsync(false);
        _mockCacheRepo
            .Setup(repo => repo.DeleteAsync(token.UserId.ToString(), token.TokenType))
            .ReturnsAsync(false);

        // Act
        var result = await _tokenManager.DeleteTokenAsync(token);

        // Assert
        Assert.That(result, Is.False, "Expected false result when token does not exist.");
    }

    #endregion

    #region CreateTokenTest

    [Test]
    public void CreateToken_ValidParameters_ReturnsToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        // Act
        var result = _tokenManager.CreateToken(userId, tokenType);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected token to be created but got null.");
        Assert.That(result.UserId, Is.EqualTo(userId), "UserId of the returned token does not match.");
        Assert.That(result.TokenType, Is.EqualTo(tokenType), "TokenType of the returned token does not match.");
        Assert.That(DataHelper.IsTokenValid(result.Value), Is.True,
            "Token Value of the returned token does not match.");
    }

    [Test]
    public void CreateToken_InvalidTokenType_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType invalidTokenType = TokenType.Unknown;

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        // Act
        var result = _tokenManager.CreateToken(userId, invalidTokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected token to be null for TokenType.Unknown.");
    }

    [Test]
    public void CreateToken_AddTokenFails_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo.Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(false);

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        // Act
        var result = _tokenManager.CreateToken(userId, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected token to be null when AddToken fails.");
    }

    [Test]
    public void CreateToken_AddUserIdTokenFails_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo.Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(false);

        // Act
        var result = _tokenManager.CreateToken(userId, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected token to be null when Add for UserId fails.");
    }

    [Test]
    public void CreateToken_AddTokenAndUserIdFails_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        _mockCacheRepo.Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(false);

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(false);

        // Act
        var result = _tokenManager.CreateToken(userId, tokenType);

        // Assert
        Assert.That(result, Is.Null, "Expected token to be null when both Add operations fail.");
    }

    #endregion

    #region UpdateTokenAsyncTest

    [Test]
    public async Task UpdateTokenAsync_ValidToken_ReturnsUpdatedToken()
    {
        // Arrange
        var initialModified = DateTime.UtcNow - MailConfig.Values.MinIntervalBetweenAttempts;
        
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = Guid.NewGuid(),
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation,
            AttemptCount = 1,
            Modified = initialModified
        };
        
        _mockCacheRepo.Setup(repo => repo.DeleteAsync(It.IsAny<string>(), It.IsAny<TokenType>()))
            .ReturnsAsync(true);

        _mockCacheRepo.Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);
        
        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        // Act
        var result = await _tokenManager.UpdateTokenAsync(token);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected token to be updated but got null.");
        Assert.That(result.UserId, Is.EqualTo(token.UserId), "UserId of the returned token does not match.");
        Assert.That(result.TokenType, Is.EqualTo(token.TokenType), "TokenType of the returned token does not match.");
        Assert.That(DataHelper.IsTokenValid(result.Value), Is.True,
            "Token Value of the returned token does not match.");
        Assert.That(result.AttemptCount, Is.EqualTo(2), "AttemptCount should be incremented.");
        Assert.That(result.Modified, Is.Not.EqualTo(initialModified), "Modified date should be updated.");
    }
    
    [Test]
    public async Task UpdateTokenAsync_TokenModifiedLongEnough_AttemptCountResetToOne()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            UserId = Guid.NewGuid(),
            Value = "oldToken",
            TokenType = TokenType.RegistrationConfirmation,
            AttemptCount = MailConfig.Values.MaxAttemptsConfirmationResend,
            Modified = DateTime.UtcNow - MailConfig.Values.NextAttemptAvailableAfter
        };

        _mockCacheRepo.Setup(repo => repo.DeleteAsync(It.IsAny<string>(), It.IsAny<TokenType>()))
            .ReturnsAsync(true);

        _mockCacheRepo.Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        _mockCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        // Act
        var result = await _tokenManager.UpdateTokenAsync(token);

        // Assert
        Assert.That(result, Is.Not.Null, "Expected token to be updated but got null.");
        Assert.That(result.AttemptCount, Is.EqualTo(1), "AttemptCount should be reset to 0.");
    }

    [Test]
    public async Task UpdateTokenAsync_TokenNull_ReturnsNull()
    {
        // Act
        var result = await _tokenManager.UpdateTokenAsync(null);

        // Assert
        Assert.That(result, Is.Null, "Expected null when token is null.");
    }

    [Test]
    public async Task UpdateTokenAsync_DeleteTokenFails_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = userId,
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation,
            AttemptCount = 1,
            Modified = DateTime.UtcNow - MailConfig.Values.MinIntervalBetweenAttempts
        };

        _mockCacheRepo.Setup(repo => repo.DeleteAsync(It.IsAny<string>(), It.IsAny<TokenType>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tokenManager.UpdateTokenAsync(token);

        // Assert
        Assert.That(result, Is.Null, "Expected null when token deletion fails.");
    }

    [Test]
    public async Task UpdateTokenAsync_AddTokenFails_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RedisConfirmationToken
        {
            UserId = userId,
            Value = UserHelper.GetToken(userId),
            TokenType = TokenType.RegistrationConfirmation,
            AttemptCount = 1,
            Modified = DateTime.UtcNow - MailConfig.Values.MinIntervalBetweenAttempts
        };

        _mockCacheRepo.Setup(repo => repo.DeleteAsync(It.IsAny<string>(), It.IsAny<TokenType>()))
            .ReturnsAsync(true);

        _mockCacheRepo.Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<RedisConfirmationToken>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(false);

        // Act
        var result = await _tokenManager.UpdateTokenAsync(token);

        // Assert
        Assert.That(result, Is.Null, "Expected null when adding the token fails.");
    }

    #endregion

    #region DetermineTokenTypeTest

    [Test]
    public void DetermineTokenType_UsernameIsSet_ReturnsUsernameChange()
    {
        // Arrange
        var updateRequest = new UserUpdateRequest
        {
            Id = Guid.NewGuid(),
            Username = "newUsername"
        };

        // Act
        var result = _tokenManager.DetermineTokenType(updateRequest);

        // Assert
        Assert.That(result, Is.EqualTo(TokenType.UsernameChange), "Expected UsernameChange token type.");
    }

    [Test]
    public void DetermineTokenType_NewPasswordIsSet_ReturnsPasswordChange()
    {
        // Arrange
        var updateRequest = new UserUpdateRequest
        {
            Id = Guid.NewGuid(),
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = _tokenManager.DetermineTokenType(updateRequest);

        // Assert
        Assert.That(result, Is.EqualTo(TokenType.PasswordChange), "Expected PasswordChange token type.");
    }

    [Test]
    public void DetermineTokenType_EmailIsSet_ReturnsEmailChangeOld()
    {
        // Arrange
        var updateRequest = new UserUpdateRequest
        {
            Id = Guid.NewGuid(),
            Email = "newemail@example.com"
        };

        // Act
        var result = _tokenManager.DetermineTokenType(updateRequest);

        // Assert
        Assert.That(result, Is.EqualTo(TokenType.EmailChangeOld), "Expected EmailChangeOld token type.");
    }

    [Test]
    public void DetermineTokenType_NoFieldsSet_ReturnsUnknown()
    {
        // Arrange
        var updateRequest = new UserUpdateRequest
        {
            Id = Guid.NewGuid()
        };

        // Act
        var result = _tokenManager.DetermineTokenType(updateRequest);

        // Assert
        Assert.That(result, Is.EqualTo(TokenType.Unknown), "Expected Unknown token type.");
    }

    [Test]
    public void DetermineTokenType_UpdateRequestIsNull_ReturnsUnknown()
    {
        // Act
        var result = _tokenManager.DetermineTokenType(null);

        // Assert
        Assert.That(result, Is.EqualTo(TokenType.Unknown), "Expected Unknown token type.");
    }

    #endregion

    #region ValidateTokenTypeForRequestTest

    [Test]
    public void ValidateTokenTypeForRequest_RegistrationProcess_ReturnsTrueForRegistrationConfirmation()
    {
        // Arrange
        const bool isRegistrationProcess = true;
        const TokenType tokenType = TokenType.RegistrationConfirmation;

        // Act
        var result = _tokenManager.ValidateTokenTypeForRequest(tokenType, isRegistrationProcess);

        // Assert
        Assert.That(result, Is.True,
            "Expected true for RegistrationConfirmation token type during registration process.");
    }

    [Test]
    public void ValidateTokenTypeForRequest_RegistrationProcess_ReturnsFalseForOtherTokenTypes()
    {
        // Arrange
        const bool isRegistrationProcess = true;
        var tokenTypes = new[]
        {
            TokenType.EmailChangeOld,
            TokenType.EmailChangeNew,
            TokenType.PasswordChange,
            TokenType.UsernameChange,
            TokenType.Unknown
        };

        // Act & Assert
        foreach (var tokenType in tokenTypes)
        {
            var result = _tokenManager.ValidateTokenTypeForRequest(tokenType, isRegistrationProcess);
            Assert.That(result, Is.False, $"Expected false for token type {tokenType} during registration process.");
        }
    }

    [Test]
    public void ValidateTokenTypeForRequest_NotRegistrationProcess_ReturnsTrueForValidTokenTypes()
    {
        // Arrange
        const bool isRegistrationProcess = false;
        var tokenTypes = new[]
        {
            TokenType.EmailChangeOld,
            TokenType.EmailChangeNew,
            TokenType.PasswordChange,
            TokenType.UsernameChange
        };

        // Act & Assert
        foreach (var tokenType in tokenTypes)
        {
            var result = _tokenManager.ValidateTokenTypeForRequest(tokenType, isRegistrationProcess);
            Assert.That(result, Is.True,
                $"Expected true for valid token type {tokenType} when not in registration process.");
        }
    }

    [Test]
    public void ValidateTokenTypeForRequest_NotRegistrationProcess_ReturnsFalseForRegistrationConfirmationAndUnknown()
    {
        // Arrange
        const bool isRegistrationProcess = false;
        var tokenTypes = new[]
        {
            TokenType.RegistrationConfirmation,
            TokenType.Unknown
        };

        // Act & Assert
        foreach (var tokenType in tokenTypes)
        {
            var result = _tokenManager.ValidateTokenTypeForRequest(tokenType, isRegistrationProcess);
            Assert.That(result, Is.False,
                $"Expected false for token type {tokenType} when not in registration process.");
        }
    }

    #endregion
}