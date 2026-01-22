using FluentValidation;
using pearlxcore.dev.ViewModels.Account;

namespace pearlxcore.dev.Validators.Account;

public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
{
    public LoginViewModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
