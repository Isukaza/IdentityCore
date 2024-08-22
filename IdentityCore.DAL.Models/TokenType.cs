namespace IdentityCore.DAL.Models;

public enum TokenType
{
    RegistrationConfirmation,
    EmailChangeOld,
    EmailChangeNew,
    PasswordChange,
    UsernameChange,
    Unknown
}