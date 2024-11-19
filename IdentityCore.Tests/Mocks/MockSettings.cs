using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using IdentityCore.Configuration;

namespace IdentityCore.Tests.Mocks;

public static class MockSettings
{
    public static JwtBearerOptions CreateJwtBearerOptionsMock()
    {
        var randomKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());
        var jwtBearerOptions = new JwtBearerOptions
        {
            TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                IssuerSigningKey = randomKey, 
                ValidateIssuerSigningKey = true,
            }
        };

        return jwtBearerOptions;
    }
    
    public static void InitializeAllConfigurations(IConfigurationRoot mockConfiguration)
    {
        ConfigBase.SetConfiguration(mockConfiguration);
   
        HostConfig.Values.Initialize(mockConfiguration);
        MailConfig.Values.Initialize(mockConfiguration);
        RefTokenConfig.Values.Initialize(mockConfiguration);
        TokenConfig.Values.Initialize(mockConfiguration);
        GoogleConfig.Values.Initialize(mockConfiguration, true);
        JwtConfig.Values.Initialize(mockConfiguration, true);
        RabbitMqConfig.Values.Initialize(mockConfiguration, true);
    }
}