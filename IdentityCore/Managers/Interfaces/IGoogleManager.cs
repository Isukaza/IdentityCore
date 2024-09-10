using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Responses;

namespace IdentityCore.Managers.Interfaces;

public interface IGoogleManager
{
    string GenerateGoogleLoginUrl();
    Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken);
    Task<TokenResponse> ExchangeCodeForTokenAsync(string code);
}