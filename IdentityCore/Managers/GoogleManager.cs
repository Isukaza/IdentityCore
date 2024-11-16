using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

using IdentityCore.Configuration;
using IdentityCore.Managers.Interfaces;

namespace IdentityCore.Managers;

public class GoogleManager : IGoogleManager
{
    public string GenerateGoogleLoginUrl()
    {
        var flowInitializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = GoogleConfig.Values.ClientId,
                ClientSecret = GoogleConfig.Values.ClientSecret
            },
            Scopes = GoogleConfig.Values.Scope.Split(' ')
        };
        
        var flow = new GoogleAuthorizationCodeFlow(flowInitializer);
        
        var authorizationUrl = flow
            .CreateAuthorizationCodeRequest(GoogleConfig.Values.RedirectUri)
            .Build();
        
        return authorizationUrl.AbsoluteUri;
    }

    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { GoogleConfig.Values.ClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return payload;
    }

    public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = GoogleConfig.Values.ClientId,
            ClientSecret = GoogleConfig.Values.ClientSecret
        };
        
        var tokenResponse = await new AuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets
            })
            .ExchangeCodeForTokenAsync(userId: "", code, GoogleConfig.Values.RedirectUri, CancellationToken.None);

        return tokenResponse;
    }
}