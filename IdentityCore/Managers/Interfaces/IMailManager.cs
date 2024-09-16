using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.Managers.Interfaces;

public interface IMailManager
{
    Task<string> SendEmailAsync(
        string toEmailAddress,
        TokenType tokenType,
        string confirmationLink,
        User user = null,
        RedisUserUpdate userUpdate = null);

    Task<string> CreateTemplate(string templateName, string subject, string htmlContent);

    Task<string> DeleteTemplate(string templateName);
}