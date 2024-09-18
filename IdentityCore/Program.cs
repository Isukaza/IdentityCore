using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL;
using IdentityCore.DAL.PostgreSQL.Repositories.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.Managers;
using IdentityCore.Managers.Interfaces;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPath
#if DEBUG
                            | HttpLoggingFields.RequestBody
                            | HttpLoggingFields.ResponseBody
#endif
                            | HttpLoggingFields.Duration
                            | HttpLoggingFields.ResponseStatusCode;
    logging.CombineLogs = true;
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Jwt.Configs.Issuer,
            ValidateAudience = true,
            ValidAudience = Jwt.Configs.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = Jwt.Configs.Key,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddDbContext<IdentityCoreDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnectionUrl = builder.Configuration.GetConnectionString("RedisConnectionUrl");
    if (string.IsNullOrEmpty(redisConnectionUrl))
    {
        throw new InvalidOperationException("RedisConnectionUrl is not configured.");
    }

    return ConnectionMultiplexer.Connect(redisConnectionUrl);
});

builder.Services.AddScoped<ICacheRepositoryBase, CacheRepositoryBase>();
builder.Services.AddScoped<IUserDbRepository, UserDbRepository>();
builder.Services.AddScoped<IUserCacheRepository, UserCacheRepository>();
builder.Services.AddScoped<IRefreshTokenDbRepository, RefreshTokenDbRepository>();
builder.Services.AddScoped<ICfmTokenCacheRepository, CfmTokenCacheRepository>();

builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IAuthenticationManager, AuthenticationManager>();
builder.Services.AddScoped<IRefreshTokenManager, RefreshTokenManager>();
builder.Services.AddScoped<ICfmTokenManager, CfmTokenManager>();
builder.Services.AddScoped<IMailManager, MailManager>();
builder.Services.AddScoped<IGoogleManager, GoogleManager>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "current",
        new OpenApiInfo
        {
            Title = "Identity Core API",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        });

    options.IncludeXmlComments(
        Path.Combine(
            AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
            "Enter 'Bearer' [space and then your token in the text input below. \r\n\r\n" +
            "Example: 'Bearer HHH.PPP.CCC'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "OAuth 2.0",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseHttpLogging();

app.UseSwagger(options =>
{
    options.RouteTemplate = "/api/{documentName}/swagger.json";
    options.PreSerializeFilters.Add((document, _) => document.Servers.Clear());
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api/current/swagger.json", "Identity Core API");
    options.RoutePrefix = "api";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();