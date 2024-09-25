using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using IdentityCore.DAL.PostgreSQL;
using IdentityCore.DAL.PostgreSQL.Repositories.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers;
using IdentityCore.Managers.Interfaces;

using Moq;
using StackExchange.Redis;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Responses;

namespace IdentityCore.Tests.Mocks;

public static class MockFactory
{
    public static T GetController<T>(HttpContext context) where T : ControllerBase
    {
        var controller = (T)ActivatorUtilities.CreateInstance(context.RequestServices, typeof(T));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };

        return controller;
    }

    public static Mock<HttpContext> GetHttpContextMock()
    {
        var httpContextMock = new Mock<HttpContext> { CallBase = true };
        var serviceCollection = GetDependencies();

        var requestMock = new Mock<HttpRequest>();
        var headers = new HeaderDictionary();
        requestMock
            .Setup(request => request.Headers)
            .Returns(headers);

        httpContextMock
            .Setup(context => context.Request)
            .Returns(requestMock.Object);

        httpContextMock
            .Setup(context => context.RequestServices)
            .Returns(serviceCollection.BuildServiceProvider());

        return httpContextMock;
    }

    private static ServiceCollection GetDependencies()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<IdentityCoreDbContext>(options => options.UseInMemoryDatabase("TestDatabase"));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                options.TokenValidationParameters =
                    MockSettings.CreateJwtBearerOptionsMock().TokenValidationParameters);

        services.AddSingleton(_ =>
        {
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockMultiplexer
                .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            return mockMultiplexer.Object;
        });

        services.AddSingleton(_ =>
        {
            var mockGoogleManager = new Mock<IGoogleManager>();
            mockGoogleManager
                .Setup(g => g.ExchangeCodeForTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new TokenResponse
                {
                    IdToken = "id_token"
                });

            mockGoogleManager
                .Setup(g => g.VerifyGoogleTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new GoogleJsonWebSignature.Payload
                {
                    Email = "test@test.com",
                    Name = "test"
                });

            return mockGoogleManager.Object;
        });

        services.AddScoped<ICacheRepositoryBase, CacheRepositoryBase>();
        services.AddScoped<IUserDbRepository, UserDbRepository>();
        services.AddScoped<IUserCacheRepository, UserCacheRepository>();
        services.AddScoped<IRefreshTokenDbRepository, RefreshTokenDbRepository>();
        services.AddScoped<ICfmTokenCacheRepository, CfmTokenCacheRepository>();

        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IAuthenticationManager, AuthenticationManager>();
        services.AddScoped<IRefreshTokenManager, RefreshTokenManager>();
        services.AddScoped<ICfmTokenManager, CfmTokenManager>();
        services.AddScoped<IMailManager, MailManager>();

        return services;
    }

    public static IConfigurationRoot GetMockConfiguration()
    {
        var mockConfiguration = new Mock<IConfigurationRoot>();

        const string hostGroupName = "Host";
        mockConfiguration
            .Setup(c => c[$"{hostGroupName}:URL"])
            .Returns("https://localhost:7109");
        mockConfiguration
            .Setup(c => c[$"{hostGroupName}:RegistrationConfirmationPath"])
            .Returns("/User/cfm-reg-token");
        mockConfiguration
            .Setup(c => c[$"{hostGroupName}:ConfirmationTokenPath"])
            .Returns("/User/cfm-token");

        const string jwtGroupName = "JWT";
        mockConfiguration
            .Setup(c => c[$"{jwtGroupName}:Issuer"])
            .Returns("AuthOptions.ISSUER");
        mockConfiguration
            .Setup(c => c[$"{jwtGroupName}:Audience"])
            .Returns("AuthOptions.AUDIENCE");
        mockConfiguration
            .Setup(c => c[$"{jwtGroupName}:Expires"])
            .Returns("512");
        mockConfiguration
            .Setup(c => c[$"{jwtGroupName}:Key"])
            .Returns("VVY$%V#v3vt4q4tB$%QY$%ny45nwb2qv4y34uy45u");

        const string refreshTokenGroupName = "RefreshToken";
        mockConfiguration
            .Setup(c => c[$"{refreshTokenGroupName}:Expires"])
            .Returns("30");
        mockConfiguration
            .Setup(c => c[$"{refreshTokenGroupName}:MaxSessions"])
            .Returns("5");

        const string tokenConfigGroupName = "TokenConfig";
        mockConfiguration
            .Setup(c => c[$"{tokenConfigGroupName}:TTL:RegistrationConfirmation"])
            .Returns("00:15:00");
        mockConfiguration
            .Setup(c => c[$"{tokenConfigGroupName}:TTL:EmailChangeOld"])
            .Returns("00:15:00");
        mockConfiguration
            .Setup(c => c[$"{tokenConfigGroupName}:TTL:EmailChangeNew"])
            .Returns("00:15:00");
        mockConfiguration
            .Setup(c => c[$"{tokenConfigGroupName}:TTL:PasswordChange"])
            .Returns("00:15:00");
        mockConfiguration
            .Setup(c => c[$"{tokenConfigGroupName}:TTL:UsernameChange"])
            .Returns("00:15:00");

        const string mailGroupName = "Mail";
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:Mail"])
            .Returns("admin@skillforge.click");
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:Region"])
            .Returns("eu-central-1");
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:AwsAccessKeyId"])
            .Returns("keyId");
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:AwsSecretAccessKey"])
            .Returns("key");
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:MaxAttemptsConfirmationResend"])
            .Returns("3");
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:NextAttemptAvailableAfter"])
            .Returns("00:00:20");
        mockConfiguration
            .Setup(c => c[$"{mailGroupName}:MinIntervalBetweenAttempts"])
            .Returns("00:00:10");

        const string googleAuthGroupName = "GoogleAuth";
        mockConfiguration
            .Setup(c => c[$"{googleAuthGroupName}:ClientId"])
            .Returns("<google client id>");
        mockConfiguration
            .Setup(c => c[$"{googleAuthGroupName}:ClientSecret"])
            .Returns("<google client secret>");
        mockConfiguration
            .Setup(c => c[$"{googleAuthGroupName}:RedirectUri"])
            .Returns("<your callback endpoint to process the code>");
        mockConfiguration
            .Setup(c => c[$"{googleAuthGroupName}:Scope"])
            .Returns("<your need scope>");

        return mockConfiguration.Object;
    }
    
    public static void MoqUserClaims(Mock<HttpContext> httpContextMock, User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, nameof(UserRole.SuperAdmin))
        };

        var claimsIdentity = new ClaimsIdentity(claims, "JwtBearer");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        httpContextMock
            .Setup(context => context.User)
            .Returns(claimsPrincipal);
    }
}