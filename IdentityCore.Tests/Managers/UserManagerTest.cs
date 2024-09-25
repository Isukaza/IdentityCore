using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers;
using IdentityCore.Models.Request;
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
        ConfigBase.SetConfiguration(mockConfiguration);

        _mockUserDbRepo = new Mock<IUserDbRepository>();
        _mockUserCacheRepo = new Mock<IUserCacheRepository>();
        _mockCtCacheRepo = new Mock<ICfmTokenCacheRepository>();

        _userManager = new UserManager(_mockUserDbRepo.Object, _mockUserCacheRepo.Object, _mockCtCacheRepo.Object);
    }

    #region UpdateUserTests

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
}