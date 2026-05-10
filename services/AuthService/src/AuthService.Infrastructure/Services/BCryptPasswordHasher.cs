using AuthService.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Services;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string password)
    {
        return _inner.HashPassword(null!, password);
    }

    public bool Verify(string password, string hash)
    {
        var res = _inner.VerifyHashedPassword(null!, hash, password);
        return res == PasswordVerificationResult.Success || res == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
