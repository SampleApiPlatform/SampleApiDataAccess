using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Models; // Importiere dein Model

public class MovieValidator : IValidator<Movie>
{
    public ValidationResult Validate(Movie movie)
    {
        var validationResult = new ValidationResult();
        if (string.IsNullOrEmpty(movie.Title))
        {
            validationResult.Errors.Add("The Title of the Movie cannot be empty");
        }

        if (movie.Year > 0 && movie.Year < DateTime.Now.Year)
        {
            validationResult.Errors.Add("The Title of the Movie cannot be empty");
        }

        if (validationResult.Errors.Count == 0)
        {
            validationResult.IsValid = true;
        }
        else
        {
            validationResult.IsValid = false;
        }
        return validationResult;

    }
}