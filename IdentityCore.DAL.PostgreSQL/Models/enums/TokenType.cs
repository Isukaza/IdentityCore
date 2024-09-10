namespace IdentityCore.DAL.PostgreSQL.Models.enums;

public enum TokenType
{
    RegistrationConfirmation,
    EmailChangeOld,
    EmailChangeNew,
    PasswordChange,
    UsernameChange,
    Unknown
}