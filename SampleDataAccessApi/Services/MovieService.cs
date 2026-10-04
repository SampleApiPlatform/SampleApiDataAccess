using Microsoft.EntityFrameworkCore;
using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
using SampleDataAccessApi.Mappings.MovieMapping;
using SampleDataAccessApi.Models;



namespace SampleApi.Services.MovieServices;
public class MovieService : IMovieService
{
    //ASYNC RULE
    //If your method uses await, it must be async.
    //If your method returns a Task directly, it must NOT be async
    private readonly IMovieRepository _movieRepository;
    private readonly IValidator<Movie> _movieValidator;

    public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
    {
        _movieRepository = movieRepository;
        _movieValidator = movieValidator;
    }

    public async Task<ServiceResult<IEnumerable<MovieDTORead>>> GetAll()
    {
        var movieEntities = await _movieRepository.GetAll();
        var moviesDTO = new List<MovieDTORead>();
        foreach(Movie movieEntity in movieEntities)
        {
            moviesDTO.Add(MovieMapping.MapFromMovieEntityToMovieDTORead(movieEntity));
        }
        return ServiceResult<IEnumerable<MovieDTORead>>.Ok(moviesDTO);
    }
    public async Task<ServiceResult<MovieDTORead?>> GetById(string id)
    {
        var movieEntity = await _movieRepository.GetById(id);

        if (movieEntity == null)
            return ServiceResult<MovieDTORead?>.Fail([$"Record with id: {id} not found"]);

        var movieDTO = MovieMapping.MapFromMovieEntityToMovieDTORead(movieEntity);
        return ServiceResult<MovieDTORead?>.Ok(movieDTO);
    }
    public async Task<ServiceResult<MovieDTORead>> Add(MovieDTOAdd movieDTOAdd)
    {
        var serviceResult = new ServiceResult<MovieDTORead>();
        // map
        var movieEntity = MovieMapping.MapFromMovieDTOAddToMovieEntity(movieDTOAdd);
        // validate
        var validationResult = _movieValidator.Validate(movieEntity);
        if (!validationResult.IsValid)
            return ServiceResult<MovieDTORead>.Fail(validationResult.Errors); 
        //EF Core tracks the entity that we have just added
        await _movieRepository.Add(movieEntity);  
        var dtoRead = MovieMapping.MapFromMovieEntityToMovieDTORead(movieEntity);      
        return ServiceResult<MovieDTORead>.Ok(dtoRead);
    }  
    public async Task<ServiceResult<MovieDTORead>> Update(string id, MovieDTOUpdate movieDTOUpdate)
    {
        //MovieDTORead?? = Because the service might not find the movie, and you need a way to express that possibility in the return type.
        //The service should always return MovieDTORead, never MovieDTOUpdate
        //DTOUpdate = input
        //DTORead = output
        //It represents what the API returns after the update.
        //Returning DTOUpdate makes no sense because:
        //DTOUpdate does not contain the Id
        //DTOUpdate does not represent the final state of the entity
        //DTOUpdate is not what clients expect after an update
        //DTOUpdate is not used anywhere else in your API

        var movieEntity = await _movieRepository.GetById(id);

        if (movieEntity == null)
            return  ServiceResult<MovieDTORead>.Fail(["Entity Not Found"]);

        movieEntity.Title = movieDTOUpdate.Title;
        movieEntity.Year = movieDTOUpdate.Year;

        var validationResult = _movieValidator.Validate(movieEntity);
        if (!validationResult.IsValid)
            return ServiceResult<MovieDTORead>.Fail(validationResult.Errors); 

        await _movieRepository.Update(movieEntity);
        var dtoRead = MovieMapping.MapFromMovieEntityToMovieDTORead(movieEntity);      
        return ServiceResult<MovieDTORead>.Ok(dtoRead);
    }
    public async Task<ServiceResult<bool>> Delete(string id)
    {
        //if the entity cannot be found the EF will return false.
        //var deleted = await _movieRepository.Delete(id);
        //return deleted;

        var movie = await _movieRepository.GetById(id);
        if (movie == null)
            return ServiceResult<bool>.Fail(["Entity Not Found"]);
        // Optional: cross-entity validation
        //var validation = _movieValidator.Validate(movie);
        //if (!validation.IsValid)
        //    return ServiceResult<bool>.Fail(validation.Errors);
        await _movieRepository.Delete(movie);
        return ServiceResult<bool>.Ok(true);
    }

}