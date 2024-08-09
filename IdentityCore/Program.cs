using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using IdentityCore.DAL.PorstgreSQL;
using IdentityCore.DAL.Repository;
using IdentityCore.DAL.Repository.Base;
using IdentityCore.Configuration;
using IdentityCore.Managers;

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
    Console.WriteLine(redisConnectionUrl);
    if (string.IsNullOrEmpty(redisConnectionUrl))
    {
        throw new InvalidOperationException("RedisConnectionUrl is not configured.");
    }

    return ConnectionMultiplexer.Connect(redisConnectionUrl);
});

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<RefreshTokenRepository>();
builder.Services.AddScoped<CacheRepositoryBase>();
builder.Services.AddScoped<ConfirmationTokenRepository>();

builder.Services.AddScoped<UserManager>();
builder.Services.AddScoped<RefreshTokenManager>();
builder.Services.AddScoped<ConfirmationTokenManager>();
builder.Services.AddScoped<MailManager>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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