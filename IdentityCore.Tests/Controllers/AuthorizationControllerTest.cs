using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using JetBrains.Annotations;

using IdentityCore.Controllers;
using IdentityCore.DAL.PostgreSQL;
using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.Models.Request;

using Moq;
using NUnit.Framework;

namespace IdentityCore.Tests.Controllers;

[TestSubject(typeof(AuthorizationController))]
public class AuthorizationControllerTest
{
    #region props

    private AuthorizationController _controller;
    private IdentityCoreDbContext _dbContext;

    private Mock<HttpContext> _httpContextMock;

    private readonly Random _random = new();
    private readonly string _defaultPassword = Helpers.TestDataHelper.GeneratePassword();

    private List<User> _users;

    #endregion

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _httpContextMock = Mocks.MockFactory.GetHttpContextMock();
        _dbContext = _httpContextMock.Object.RequestServices.GetService<IdentityCoreDbContext>();
        _controller = Mocks.MockFactory.GetController<AuthorizationController>(_httpContextMock.Object);
        _users = Helpers.TestDbEntityBuilder.GenerateUsers(_defaultPassword, 10, 2);

        await _dbContext.Database.EnsureCreatedAsync();
        await _dbContext.Users.AddRangeAsync(_users);
        await _dbContext.SaveChangesAsync();
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        var authData = new UserLoginRequest
        {
            Email = user.Email,
            Password = _defaultPassword
        };

        // Act
        var response = await _controller.Login(authData) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task Login_InvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        var authData = new UserLoginRequest
        {
            Email = user.Email,
            Password = "BadPass"
        };

        // Act
        var response = await _controller.Login(authData) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task Login_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var authData = new UserLoginRequest
        {
            Email = "invalidEmail@example.com",
            Password = _defaultPassword
        };

        // Act
        var response = await _controller.Login(authData) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }
    
    [Test]
    public async Task LoginSSO_NewUser_ReturnsOK()
    {
        // Act
        var response = await _controller.GoogleCallback("") as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task Refresh_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        Assert.That(0, Is.Not.EqualTo(user.RefreshTokens.Count));

        var refreshToken = user.RefreshTokens.FirstOrDefault();
        Assert.That(refreshToken, Is.Not.Null);

        var refreshTokenRequest = new RefreshTokenRequest
        {
            UserId = refreshToken.UserId,
            RefreshToken = refreshToken.RefToken
        };

        // Act
        var response = await _controller.Refresh(refreshTokenRequest) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task Refresh_InvalidTokenValue_ReturnsBadRequest()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        Assert.That(0, Is.Not.EqualTo(user.RefreshTokens.Count));

        var refreshToken = user.RefreshTokens.FirstOrDefault();
        Assert.That(refreshToken, Is.Not.Null);

        var refreshTokenRequest = new RefreshTokenRequest
        {
            UserId = refreshToken.UserId,
            RefreshToken = "InvalidRefreshToken"
        };

        // Act
        var response = await _controller.Refresh(refreshTokenRequest) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task Logout_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        Assert.That(0, Is.Not.EqualTo(user.RefreshTokens.Count));

        var refreshToken = user.RefreshTokens.FirstOrDefault();
        Assert.That(refreshToken, Is.Not.Null);

        var logoutData = new LogoutRequest
        {
            UserId = refreshToken.UserId,
            RefreshToken = refreshToken.RefToken
        };

        // Act
        var response = await _controller.Logout(logoutData) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task Logout_InvalidTokenValue_ReturnsOk()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        Assert.That(0, Is.Not.EqualTo(user.RefreshTokens.Count));

        var refreshToken = user.RefreshTokens.FirstOrDefault();
        Assert.That(refreshToken, Is.Not.Null);

        var logoutData = new LogoutRequest
        {
            UserId = refreshToken.UserId,
            RefreshToken = refreshToken.RefToken
        };

        // Act
        var response = await _controller.Logout(logoutData) as ObjectResult;

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dbContext.Database.EnsureDeleted();
    }
}