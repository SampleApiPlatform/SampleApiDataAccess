using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Validators;
public class MovieValidator: IValidator<Movie>
{
    public ValidationResult Validate(Movie movie)
    {
        var validationResult = new ValidationResult()
        {
            IsValid = true,
            Errors = new List<string>()
        };
        if (movie == null)
        {
            validationResult.IsValid = false;
            validationResult.Errors.Add("The movie cannot be null");
            return validationResult; 
            // if the movie is null it does not make sense to check the rest , 
            // because then for instance movie.Title will throw an exception (the title of something that is null)
        }
        if(string.IsNullOrWhiteSpace(movie.Title))
        {
            validationResult.IsValid = false;
            validationResult.Errors.Add("The Title of the Movie cannot be empty");

        }
        if (movie.Year < 1800)
        {
            validationResult.IsValid = false;
            validationResult.Errors.Add("The years must be greater than 1799");
        }

        return validationResult;
    }
}