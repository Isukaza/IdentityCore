using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using JetBrains.Annotations;

using IdentityCore.Controllers;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.PorstgreSQL;
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
        _users = Helpers.TestDbEntityBuilder.GenerateUsers(_defaultPassword, 10);

        await _dbContext.Database.EnsureCreatedAsync();
        await _dbContext.Users.AddRangeAsync(_users);
        await _dbContext.SaveChangesAsync();
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var user = _users[_random.Next(_users.Count)];
        var authData = new UserLoginRequest()
        {
            Email = user.Email,
            Password = _defaultPassword
        };
        
        // Act
        var response = await _controller.Login(authData) as ObjectResult;
        
        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(StatusCodes.Status200OK, Is.EqualTo(response.StatusCode));
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
        Assert.That(StatusCodes.Status400BadRequest, Is.EqualTo(response.StatusCode));
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
        Assert.That(StatusCodes.Status400BadRequest, Is.EqualTo(response.StatusCode));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dbContext.Database.EnsureDeleted();
    }
}