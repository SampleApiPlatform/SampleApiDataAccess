using FluentValidation;
using SampleDataAccessApi.Models; // Importiere dein Model

public class MovieValidator : AbstractValidator<Movie> // ✅ Von 'AbstractValidator' erben
{
    public MovieValidator()
    {
        RuleFor(x => x.Title)
        .NotEmpty()
        .WithMessage("Title is required");
        
        RuleFor(x => x.Year)
        .NotEmpty()
        .WithMessage("Title is required");
    }
}