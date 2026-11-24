using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Common;
using Kopeck.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Auth;

/// <summary>
/// Sign-up, sign-in and password reset.
/// Security notes:
/// <list type="bullet">
/// <item>Register / resend / forgot always succeed the same way and email delivery is queued, so neither the reply nor
/// its timing reveals which emails are registered.</item>
/// <item>Writes to security state (failed-login counters, code attempts) deliberately ignore request cancellation:
/// a client that aborts the request must not be able to skip the lockout bookkeeping.</item>
/// </list>
/// </summary>
public sealed class AuthService(IKopeckDbContext db, IPasswordHasher passwords, ITokenService tokens, IEmailSender mail, TimeProvider clock)
    : IAuthService
{
    private static readonly Error InvalidCode = Error.Validation("Invalid or expired code.");

    public async Task<Result> RegisterAsync(RegisterRequest request)
    {
        var email = DomainRules.NormalizeEmail(request.Email)!;   // the request validator guarantees a valid address

        var now = clock.GetUtcNow();
        var existing = await FindAsync(email);
        var user = existing ?? User.Create(email, now);
        var passwordHash = passwords.Hash(user, request.Password);   // hashed on every path so timing is the same

        if (user.EmailConfirmed)
        {
            await SendAsync(email, AuthEmails.AlreadyRegistered());
            return Result.Success;
        }

        if (existing is null) db.Users.Add(user);
        user.SetDisplayName(request.DisplayName);
        user.SetPasswordHash(passwordHash);   // a pending (unverified) account just takes the latest registration
        var code = user.IssueVerifyCode(now);
        await db.SaveChangesAsync(CancellationToken.None);
        await SendAsync(email, AuthEmails.VerificationCode(code));
        return Result.Success;
    }

    public async Task<Result> ResendCodeAsync(EmailRequest request)
    {
        var user = await FindAsync(request.Email);
        if (user is { EmailConfirmed: false })
        {
            var code = user.IssueVerifyCode(clock.GetUtcNow());
            await db.SaveChangesAsync(CancellationToken.None);
            await SendAsync(user.Email, AuthEmails.VerificationCode(code));
        }
        return Result.Success;
    }

    public async Task<Result<AuthResponse>> VerifyEmailAsync(EmailCodeRequest request)
    {
        var user = await FindAsync(request.Email);
        if (user is null) return InvalidCode;

        var confirmed = user.TryConfirmEmail(request.Code, clock.GetUtcNow());
        await db.SaveChangesAsync(CancellationToken.None);   // persists the attempt counter or the confirmation
        return confirmed ? IssueToken(user) : InvalidCode;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var now = clock.GetUtcNow();
        var user = await FindAsync(request.Email);
        var locked = user?.IsLockedOut(now) ?? false;
        var passwordOk = passwords.Verify(user, request.Password ?? "");   // always hashes, even for unknown emails

        if (user is null || locked || !passwordOk)
        {
            if (user is not null && !locked)
            {
                user.RegisterFailedLogin(now);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            return Error.Unauthorized("Wrong email or password, or the account is temporarily locked.");
        }
        if (!user.EmailConfirmed) return Error.Forbidden("Email not verified. Enter the code we emailed you.");

        user.RegisterSuccessfulLogin();
        await db.SaveChangesAsync(CancellationToken.None);
        return IssueToken(user);
    }

    public async Task<Result> ForgotPasswordAsync(EmailRequest request)
    {
        var user = await FindAsync(request.Email);
        if (user is { EmailConfirmed: true })
        {
            var code = user.IssueResetCode(clock.GetUtcNow());
            await db.SaveChangesAsync(CancellationToken.None);
            await SendAsync(user.Email, AuthEmails.ResetCode(code));
        }
        return Result.Success;
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await FindAsync(request.Email);
        if (user is null) return InvalidCode;

        var ok = user.TryConsumeResetCode(request.Code, clock.GetUtcNow());
        if (ok) user.SetPasswordHash(passwords.Hash(user, request.NewPassword));
        await db.SaveChangesAsync(CancellationToken.None);
        return ok ? Result.Success : InvalidCode;
    }

    private Task<User?> FindAsync(string? email) =>
        DomainRules.NormalizeEmail(email) is { } e
            ? db.Users.FirstOrDefaultAsync(u => u.Email == e)
            : Task.FromResult<User?>(null);

    private AuthResponse IssueToken(User user)
    {
        var token = tokens.Create(user);
        return new AuthResponse(token.Value, token.ExpiresAt, user.Id, user.Email, user.DisplayName);
    }

    private Task SendAsync(string to, (string Subject, string Body) email) => mail.SendAsync(to, email.Subject, email.Body);
}
