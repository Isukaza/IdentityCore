using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using IdentityCore.DAL.PostgreSQL;
using IdentityCore.DAL.PostgreSQL.Repositories.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
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
        services.AddScoped<IConfirmationTokenManager, ConfirmationTokenManager>();
        services.AddScoped<IMailManager, MailManager>();

        return services;
    }
}