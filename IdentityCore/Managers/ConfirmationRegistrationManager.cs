using System.Text;
using IdentityCore.DAL.Repository;

namespace IdentityCore.Managers;

public class ConfirmationRegistrationManager
{
    #region C-tor and fields

    private const int Sha512ByteSize = 64;

    private readonly UserRepository _userRepo;
    private readonly ConfirmationRegistrationRepository _crRepo;

    public ConfirmationRegistrationManager(UserRepository userRepo,
        ConfirmationRegistrationRepository crRepo)
    {
        _userRepo = userRepo;
        _crRepo = crRepo;
    }

    #endregion

    public bool IsTokenValid(string token)
    {
        var bufferSize = (int)Math.Ceiling(token.Length * 3.0 / 4.0);
        var buffer = new Span<byte>(new byte[bufferSize]);
        if (Convert.TryFromBase64String(token, buffer, out var bytes)) 
            return bytes == Sha512ByteSize;

        return false;
    }

    public async Task<string> ActivatedUser(string token)
    {
        var tokenDb = await _crRepo.GetRegistrationTokenByTokenAsync(token);
        if (tokenDb is null)
            return "Invalid token";

        tokenDb.User.IsActive = true;
        
        if (!await _userRepo.UpdateAsync(tokenDb.User))
            return "Invalid token";

        await _crRepo.DeleteAsync(tokenDb);
        
        return string.Empty;
    }

    public async Task DeleteExpiredTokens()
    {
        var expiredTokens = _crRepo.GetExpiredTokens();
        await _crRepo.DeleteRange(expiredTokens);
    }
}