namespace IdentityCore.Configuration.Constants;

public static class ValidationConstants
{
    public const int PassMinLength = 12;
    public const int PassMaxLength = 255;
    public const string PassErrorMessage = "The password must be between {0} and {1} characters long.";
    public const string PassRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$";
    public const string PassFormatErrorMessage = "The password does not match the required format.";
    public const string PassMismatchErrorMessage = "NewPassword and ConfirmNewPassword must match.";
}