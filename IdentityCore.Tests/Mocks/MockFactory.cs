using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using IdentityCore.DAL.PorstgreSQL;
using IdentityCore.DAL.Repository.Interfaces;
using IdentityCore.DAL.Repository.Interfaces.Base;
using IdentityCore.DAL.Repository.Repositories;
using IdentityCore.DAL.Repository.Repositories.Base;
using IdentityCore.Managers;
using IdentityCore.Managers.Interfaces;

using Moq;
using StackExchange.Redis;

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
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                options.TokenValidationParameters = MockSettings.CreateJwtBearerOptionsMock().TokenValidationParameters);
        
        services.AddDbContext<IdentityCoreDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));

        services.AddSingleton(_ =>
        {
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockMultiplexer
                .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);
            return mockMultiplexer.Object;
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IConfirmationTokenRepository, ConfirmationTokenRepository>();
        services.AddScoped<ICacheRepositoryBase, CacheRepositoryBase>();

        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IRefreshTokenManager, RefreshTokenManager>();
        services.AddScoped<IConfirmationTokenManager, ConfirmationTokenManager>();
        services.AddScoped<IMailManager, MailManager>();

        return services;
    }
}