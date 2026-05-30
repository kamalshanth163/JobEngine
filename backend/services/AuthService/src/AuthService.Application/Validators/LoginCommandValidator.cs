using FluentValidation;
using AuthService.Application.Commands;

namespace AuthService.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.TenantSlug).NotEmpty().Matches("^[a-z0-9-]+$");
    }
}
