namespace Kopeck.Contracts;

public record ResetPasswordRequest(string Email, string Code, string NewPassword);
