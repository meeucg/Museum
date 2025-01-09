using FluentValidation;
using webProject.Models;

namespace webProject;

public class UserValidationRules:AbstractValidator<UserLoginModel?>
{
    public UserValidationRules()
    {
        RuleFor(user => user.Login)
            .NotEmpty()
            .Matches(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")
            .WithMessage("Username must be a valid email.");
        RuleFor(user => user.Password)
            .NotEmpty()
            .Matches(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,20}$")
            .WithMessage("Password must be between 8 and 20 characters, at least one digit, special symbol, and upper case letter.");
    }
}