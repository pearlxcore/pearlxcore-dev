using FluentValidation;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.ViewModels.Admin.Posts;

namespace pearlxcore.dev.Validators.Admin.Posts;

public class PostFormViewModelValidator : AbstractValidator<PostFormViewModel>
{
    public PostFormViewModelValidator(IPostService postService)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
         .Matches("^[a-z0-9-]+$")
         .When(x => !string.IsNullOrWhiteSpace(x.Slug))
         .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");


        RuleFor(x => x.Content)
            .NotEmpty();
    }
}
