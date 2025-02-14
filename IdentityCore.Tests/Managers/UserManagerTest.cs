using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Models.Request;

using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;

using Helpers;
using IdentityCore.Managers;

using IdentityCore.Tests.Mocks;
using Moq;
using NUnit.Framework;

namespace IdentityCore.Tests.Managers;

public class UserManagerTest
{
    private UserManager _userManager;
    private Mock<IUserDbRepository> _mockUserDbRepo;
    private Mock<IUserCacheRepository> _mockUserCacheRepo;
    private Mock<ICfmTokenCacheRepository> _mockCtCacheRepo;


    [SetUp]
    public void SetUp()
    {
        var mockConfiguration = Mocks.MockFactory.GetMockConfiguration();
        MockSettings.InitializeAllConfigurations(mockConfiguration);

        _mockUserDbRepo = new Mock<IUserDbRepository>();
        _mockUserCacheRepo = new Mock<IUserCacheRepository>();
        _mockCtCacheRepo = new Mock<ICfmTokenCacheRepository>();

        _userManager = new UserManager(_mockUserDbRepo.Object, _mockUserCacheRepo.Object, _mockCtCacheRepo.Object);
    }

    #region GetUserByEmailAsyncTest

    [Test]
    public async Task GetUserByEmailAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "existingUser",
            Email = "user@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        _mockUserDbRepo
            .Setup(repo => repo.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _userManager.GetUserByEmailAsync(user.Email);

        // Assert
        Assert.That(result, Is.EqualTo(user));
    }

    [Test]
    public async Task GetUserByEmailAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userEmail = "user@example.com";

        _mockUserDbRepo
            .Setup(repo => repo.GetUserByEmailAsync(userEmail))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userManager.GetUserByEmailAsync(userEmail);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region UpdateUserTest

    [Test]
    public async Task UpdateUser_ValidUsernameAndEmail_ReturnsTrueAndUpdatesValues()
    {
        // Arrange
        var user = new User
        {
            Username = "oldUsername",
            Email = "oldEmail@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new SuUserUpdateRequest
        {
            Username = "newUsername",
            Email = "newEmail@example.com"
        };

        _mockUserDbRepo.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).ReturnsAsync(true);

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(user.Username, Is.EqualTo("newUsername"));
        Assert.That(user.Email, Is.EqualTo("newEmail@example.com"));
    }

    [Test]
    public async Task UpdateUser_ValidRoleProvided_ReturnsTrueAndUpdatesRole()
    {
        // Arrange
        var user = new User
        {
            Role = UserRole.User,
            Username = null,
            Email = null,
            Password = null,
            Salt = null
        };
        var updateData = new SuUserUpdateRequest
        {
            Role = UserRole.Admin
        };

        _mockUserDbRepo.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).ReturnsAsync(true);

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
    }

    [Test]
    public async Task UpdateUser_NewPasswordProvided_ReturnsTrueAndUpdatesPassword()
    {
        // Arrange
        var pass = "oldPassword";
        var salt = UserHelper.GenerateSalt();
        var hashPass = UserHelper.GetPasswordHash(pass, salt);

        var user = new User
        {
            Password = hashPass,
            Username = null,
            Email = null,
            Salt = salt,
            Role = UserRole.User
        };
        var updateData = new SuUserUpdateRequest
        {
            NewPassword = "newPassword"
        };

        _mockUserDbRepo.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).ReturnsAsync(true);

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(user.Salt, Is.Not.EqualTo(salt));
        Assert.That(user.Password, Is.Not.EqualTo(hashPass));
    }

    [Test]
    public async Task UpdateUser_UserIsNull_ReturnsFalse()
    {
        // Arrange
        var updateData = new SuUserUpdateRequest();

        // Act
        var result = await _userManager.UpdateUser(null, updateData);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateUser_UpdateDataIsNull_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        // Act
        var result = await _userManager.UpdateUser(user, null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateUser_UpdateDataIsEmpty_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new SuUserUpdateRequest();

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateUser_UsernameAlreadyExists_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = "oldUsername",
            Email = "oldEmail@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new SuUserUpdateRequest
        {
            Username = "newUsername"
        };

        _mockUserDbRepo
            .Setup(repo => repo.GetUserByUsernameAsync("newUsername"))
            .ReturnsAsync(new User
            {
                Username = null,
                Email = null,
                Password = null,
                Salt = null,
                Role = UserRole.User
            });

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(user.Username, Is.EqualTo("oldUsername"));
    }

    [Test]
    public async Task UpdateUser_EmailAlreadyExists_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = "oldUsername",
            Email = "oldEmail@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new SuUserUpdateRequest
        {
            Email = "newEmail@example.com"
        };

        _mockUserDbRepo
            .Setup(repo => repo.GetUserByEmailAsync("newEmail@example.com"))
            .ReturnsAsync(new User
            {
                Username = null,
                Email = null,
                Password = null,
                Salt = null,
                Role = UserRole.User
            });

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(user.Email, Is.EqualTo("oldEmail@example.com"));
    }

    [Test]
    public async Task UpdateUser_DbUpdateFails_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = "testUser",
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };
        var updateData = new SuUserUpdateRequest
        {
            Username = "newUser"
        };

        _mockUserDbRepo.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).ReturnsAsync(false);

        // Act
        var result = await _userManager.UpdateUser(user, updateData);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(user.Username, Is.EqualTo("newUser")); // Имя пользователя должно быть обновлено
    }

    #endregion

    #region UserExistsByIdAsyncTest

    [Test]
    public async Task UserExistsByIdAsync_UserExists_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "existingUser",
            Email = "user@example.com",
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        _mockUserDbRepo
            .Setup(repo => repo.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userManager.UserExistsByIdAsync(userId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UserExistsByIdAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserDbRepo
            .Setup(repo => repo.GetUserByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userManager.UserExistsByIdAsync(userId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region ExecuteUserUpdateFromTokenAsyncTest

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_RegistrationConfirmation_ReturnStringEmpty()
    {
        // Arrange
        var user = new User
        {
            IsActive = false,
            Username = null,
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };
        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.RegistrationConfirmation,
            Value = null
        };

        _mockUserDbRepo
            .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, null, token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
        Assert.That(user.IsActive, Is.True);
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_EmailChangeOld_Success()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.EmailChangeOld,
            UserId = Guid.NewGuid(),
            Value = null
        };

        _mockCtCacheRepo
            .Setup(repo => repo.Add(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TokenType>(),
                It.IsAny<TimeSpan>()))
            .Returns(true);

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(null, null, token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_EmailChangeNew_ReturnStringEmpty()
    {
        // Arrange
        var user = new User
        {
            Email = "old@example.com",
            Username = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new RedisUserUpdate
        {
            NewValue = "new@example.com",
            Id = default,
            ChangeType = TokenType.EmailChangeNew
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.EmailChangeNew,
            Value = null
        };

        _mockUserDbRepo
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
        Assert.That(user.Email, Is.EqualTo("new@example.com"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_PasswordChange_ReturnStringEmpty()
    {
        // Arrange
        var user = new User
        {
            Password = "oldPass",
            Salt = "oldSalt",
            Username = null,
            Email = null,
            Role = UserRole.User
        };

        var updateData = new RedisUserUpdate
        {
            NewValue = "newPass",
            Salt = "newSalt",
            Id = default,
            ChangeType = TokenType.PasswordChange
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.PasswordChange,
            Value = null
        };

        _mockUserDbRepo
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
        Assert.That(user.Password, Is.EqualTo("newPass"));
        Assert.That(user.Salt, Is.EqualTo("newSalt"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_UsernameChange_ReturnStringEmpty()
    {
        // Arrange
        var user = new User
        {
            Username = "oldUsername",
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new RedisUserUpdate
        {
            NewValue = "newUsername",
            Id = default,
            ChangeType = TokenType.UsernameChange
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.UsernameChange,
            Value = null
        };

        _mockUserDbRepo
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
        Assert.That(user.Username, Is.EqualTo("newUsername"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_TokenIsNull_ReturnsInvalidTokenMessage()
    {
        // Arrange
        var user = new User
        {
            Role = UserRole.User,
            Username = null,
            Email = null,
            Password = null,
            Salt = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, null, null);

        // Assert
        Assert.That(result, Is.EqualTo("Invalid token"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_InvalidTokenType_ReturnsInvalidTokenMessage()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.Unknown,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(null, null, token);

        // Assert
        Assert.That(result, Is.EqualTo("Invalid token"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_RegistrationConfirmation_UserIsNull_ReturnsActivationError()
    {
        // Arrange
        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.RegistrationConfirmation,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(null, null, token);

        // Assert
        Assert.That(result, Is.EqualTo("Activation error"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_EmailChangeNew_UserIsNull_ReturnsErrorChangingEmail()
    {
        // Arrange
        var updateData = new RedisUserUpdate
        {
            NewValue = "new@example.com",
            Id = default,
            ChangeType = TokenType.EmailChangeNew
        };
        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.EmailChangeNew,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(null, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo("An error occurred while changing email"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_EmailChangeNew_EmailIsNullOrEmpty_ReturnsErrorChangingEmail()
    {
        // Arrange
        var user = new User
        {
            Email = "old@example.com",
            Username = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new RedisUserUpdate
        {
            NewValue = null,
            Id = default,
            ChangeType = TokenType.EmailChangeNew
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.EmailChangeNew,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo("An error occurred while changing email"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_PasswordChange_UserIsNull_ReturnsErrorChangingPassword()
    {
        // Arrange
        var updateData = new RedisUserUpdate
        {
            NewValue = "newPass",
            Salt = "newSalt",
            Id = default,
            ChangeType = TokenType.PasswordChange
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.PasswordChange,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(null, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo("An error occurred while changing password"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_PasswordChange_PasswordOrSaltIsNull_ReturnsErrorChangingPassword()
    {
        // Arrange
        var user = new User
        {
            Password = "oldPass",
            Salt = "oldSalt",
            Username = null,
            Email = null,
            Role = UserRole.User
        };

        var updateData = new RedisUserUpdate
        {
            NewValue = null,
            Salt = null,
            Id = default,
            ChangeType = TokenType.PasswordChange
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.PasswordChange,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo("An error occurred while changing password"));
    }

    [Test]
    public async Task ExecuteUserUpdateFromTokenAsync_UsernameChange_UserIsNull_ReturnsErrorChangingUsername()
    {
        // Arrange
        var updateData = new RedisUserUpdate
        {
            NewValue = "newUsername",
            Id = default,
            ChangeType = TokenType.UsernameChange
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.UsernameChange,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(null, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo("An error occurred while changing username"));
    }

    [Test]
    public async Task
        ExecuteUserUpdateFromTokenAsync_UsernameChange_UsernameIsNullOrEmpty_ReturnsErrorChangingUsername()
    {
        // Arrange
        var user = new User
        {
            Username = "oldUsername",
            Email = null,
            Password = null,
            Salt = null,
            Role = UserRole.User
        };

        var updateData = new RedisUserUpdate
        {
            NewValue = null,
            Id = default,
            ChangeType = TokenType.UsernameChange
        };

        var token = new RedisConfirmationToken
        {
            TokenType = TokenType.UsernameChange,
            Value = null
        };

        // Act
        var result = await _userManager.ExecuteUserUpdateFromTokenAsync(user, updateData, token);

        // Assert
        Assert.That(result, Is.EqualTo("An error occurred while changing username"));
    }

    #endregion

    #region ValidateUserIdentityTest

    [Test]
    public void ValidateUserIdentity_ValidIdentityWithoutComparison_ReturnsEmptyString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, nameof(UserRole.User))
        };

        // Act
        var result = _userManager.ValidateUserIdentity(claims, userId);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ValidateUserIdentity_ValidIdentityWithComparison_ReturnsEmptyString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, nameof(UserRole.Admin))
        };

        // Act
        var result = _userManager.ValidateUserIdentity(claims, userId, UserRole.User, (r1, r2) => r1 > r2);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ValidateUserIdentity_MissingRole_ReturnsAuthorizationFailedMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, nameof(UserRole.User))
        };

        // Act
        var result = _userManager.ValidateUserIdentity(claims, userId);

        // Assert
        Assert.That(result, Is.EqualTo("Authorization failed due to an invalid or missing role in the provided token"));
    }

    [Test]
    public void ValidateUserIdentity_MissingUserId_ReturnsAuthorizationFailedMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        // Act
        var result = _userManager.ValidateUserIdentity(claims, userId);

        // Assert
        Assert.That(result, Is.EqualTo("Authorization failed due to an invalid or missing role in the provided token"));
    }

    [Test]
    public void ValidateUserIdentity_UserAccessingOtherDataWithoutComparison_ReturnsPermissionDeniedMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, nameof(UserRole.User))
        };

        // Act
        var result = _userManager.ValidateUserIdentity(claims, otherUserId);

        // Assert
        Assert.That(result, Is.EqualTo("You do not have permission to access other users' data"));
    }

    [Test]
    public void ValidateUserIdentity_ComparisonNullCompareRoleNull_ThrowsArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, nameof(UserRole.Admin))
        };

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _userManager.ValidateUserIdentity(claims, userId, null, (r1, r2) => r1 == r2));

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.ParamName, Is.EqualTo("compareRole"));
        Assert.That(ex.Message, Does.Contain("compareRole cannot be null when comparison is provided."));
    }

    [Test]
    public void ValidateUserIdentity_InvalidComparison_ReturnsPermissionDeniedMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, nameof(UserRole.User))
        };

        // Act
        var result = _userManager.ValidateUserIdentity(claims, userId, UserRole.Admin, (r1, r2) => r1 == r2);

        // Assert
        Assert.That(result, Is.EqualTo("You do not have permission to access other users' data"));
    }

    #endregion

    #region GeneratePasswordUpdateEntityAsyncTest

    [Test]
    public void GeneratePasswordUpdateEntityAsync_ValidPassword_ReturnsValidRedisUserUpdate()
    {
        // Arrange
        var newPassword = "StrongPassword123!";

        // Act
        var result = _userManager.GeneratePasswordUpdateEntityAsync(newPassword);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.NewValue, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Salt, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GeneratePasswordUpdateEntityAsync_EmptyPassword_ReturnsNull()
    {
        // Arrange
        var newPassword = string.Empty;

        // Act
        var result = _userManager.GeneratePasswordUpdateEntityAsync(newPassword);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion
}