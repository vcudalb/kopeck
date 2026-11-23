using System.Net.Mail;
using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").RequireRateLimiting("auth");

        // Always 202 so the endpoint can't be used to discover which emails are registered.
        g.MapPost("/register", async (RegisterRequest r, KopeckDb db, IEmailSender mail, TimeProvider clock) =>
        {
            var email = NormalizeEmail(r.Email);
            if (email is null) return Rules.Bad("Enter a valid email address.");
            if (PasswordPolicy.Validate(r.Password) is { } pwErr) return Rules.Bad(pwErr);
            if (!Rules.IsValidName(r.DisplayName)) return Rules.Bad("Enter a display name (max 80 characters).");

            var now = clock.GetUtcNow();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is { EmailConfirmed: true })
            {
                await mail.SendAsync(email, "Kopeck account", "Someone tried to register with this email, but an account already exists. If it was you, sign in or use 'Forgot password'.");
                return Results.Accepted();
            }
            if (user is null) { user = new User { Email = email, CreatedAt = now }; db.Users.Add(user); }
            user.DisplayName = r.DisplayName.Trim();
            user.PasswordHash = PasswordService.Hash(user, r.Password);   // a pending (unverified) account just takes the latest registration
            var code = user.IssueVerifyCode(now);
            await db.SaveChangesAsync();
            await mail.SendAsync(email, "Confirm your Kopeck account", $"Your verification code is {code}. It expires in 15 minutes.");
            return Results.Accepted();
        });

        g.MapPost("/resend-code", async (EmailRequest r, KopeckDb db, IEmailSender mail, TimeProvider clock) =>
        {
            var user = await FindUser(db, r.Email);
            if (user is { EmailConfirmed: false })
            {
                var code = user.IssueVerifyCode(clock.GetUtcNow());
                await db.SaveChangesAsync();
                await mail.SendAsync(user.Email, "Confirm your Kopeck account", $"Your verification code is {code}. It expires in 15 minutes.");
            }
            return Results.Accepted();
        });

        g.MapPost("/verify-email", async (EmailCodeRequest r, KopeckDb db, JwtService jwt, TimeProvider clock) =>
        {
            var user = await FindUser(db, r.Email);
            var ok = user?.TryConfirmEmail(r.Code, clock.GetUtcNow()) ?? false;
            if (user is not null) await db.SaveChangesAsync();   // persists attempt counter or confirmation
            return ok ? Results.Ok(Token(user!, jwt)) : Rules.Bad("Invalid or expired code.");
        });

        g.MapPost("/login", async (LoginRequest r, KopeckDb db, JwtService jwt, TimeProvider clock) =>
        {
            var now = clock.GetUtcNow();
            var user = await FindUser(db, r.Email);
            var locked = user?.IsLockedOut(now) ?? false;
            var passwordOk = PasswordService.Verify(user, r.Password ?? "");   // always hashes, even for unknown emails

            if (user is null || locked || !passwordOk)
            {
                if (user is not null && !locked) { user.RegisterFailedLogin(now); await db.SaveChangesAsync(); }
                return Results.Json(new ApiError("Wrong email or password, or the account is temporarily locked."), statusCode: StatusCodes.Status401Unauthorized);
            }
            if (!user.EmailConfirmed)
                return Results.Json(new ApiError("Email not verified. Enter the code we emailed you."), statusCode: StatusCodes.Status403Forbidden);

            user.RegisterSuccessfulLogin();
            await db.SaveChangesAsync();
            return Results.Ok(Token(user, jwt));
        });

        g.MapPost("/forgot-password", async (EmailRequest r, KopeckDb db, IEmailSender mail, TimeProvider clock) =>
        {
            var user = await FindUser(db, r.Email);
            if (user is { EmailConfirmed: true })
            {
                var code = user.IssueResetCode(clock.GetUtcNow());
                await db.SaveChangesAsync();
                await mail.SendAsync(user.Email, "Reset your Kopeck password", $"Your password reset code is {code}. It expires in 15 minutes. If you didn't ask for this, ignore this email.");
            }
            return Results.Accepted();
        });

        g.MapPost("/reset-password", async (ResetPasswordRequest r, KopeckDb db, TimeProvider clock) =>
        {
            if (PasswordPolicy.Validate(r.NewPassword) is { } pwErr) return Rules.Bad(pwErr);
            var user = await FindUser(db, r.Email);
            var ok = user?.TryConsumeResetCode(r.Code, clock.GetUtcNow()) ?? false;
            if (ok) user!.PasswordHash = PasswordService.Hash(user, r.NewPassword);
            if (user is not null) await db.SaveChangesAsync();
            return ok ? Results.NoContent() : Rules.Bad("Invalid or expired code.");
        });
    }

    private static Task<User?> FindUser(KopeckDb db, string? email) =>
        NormalizeEmail(email) is { } e ? db.Users.FirstOrDefaultAsync(u => u.Email == e) : Task.FromResult<User?>(null);

    private static AuthResponse Token(User u, JwtService jwt)
    {
        var (token, expires) = jwt.Create(u);
        return new AuthResponse(token, expires, u.Id, u.Email, u.DisplayName);
    }

    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return null;
        var trimmed = email.Trim();
        try { return new MailAddress(trimmed).Address == trimmed ? trimmed.ToLowerInvariant() : null; }
        catch (FormatException) { return null; }
    }
}
