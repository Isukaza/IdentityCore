namespace IdentityCore.DAL.PostgreSQL.Models.enums;

public enum TokenType
{
    RegistrationConfirmation,
    EmailChangeOld,
    EmailChangeNew,
    PasswordReset,
    PasswordChange,
    UsernameChange,
    RoleChange,
    Unknown
}