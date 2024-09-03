using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
}