using NuGet.SampleSharedModels.DTO;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Mappings.MovieMapping;
static class MovieMapping
{
    public  static MovieDTORead MapFromMovieEntityToMovieDTORead(Movie movie)
    {
        var movieDTORead = new MovieDTORead()
        {
            Id = movie.Id,
            Title = movie.Title,
            Year = movie.Year
        };
        
        return movieDTORead;
    }

    public static Movie MapFromMovieDTOReadToMovieEntity(MovieDTORead movieDTORead)
    {
        var movieEntity = new Movie()
        {
            Id = movieDTORead.Id,
            Title = movieDTORead.Title,
            Year = movieDTORead.Year
        };
        return movieEntity;
    }

    public  static MovieDTOAdd MapFromMovieEntityToMovieDTOAdd(Movie movieEntity)
    {
        var movieDTOAdd = new MovieDTOAdd()
        {
             //Id = movieEntity.Id,
             Title = movieEntity.Title,
             Year = movieEntity.Year
        };
        return movieDTOAdd;
    }

    public static Movie MapFromMovieDTOAddToMovieEntity(MovieDTOAdd movieDTOAdd)
    {
        var movieEntity = new Movie()
        {
            Title = movieDTOAdd.Title,
            Year = movieDTOAdd.Year
        };
        return movieEntity;
    }

    public  static MovieDTOUpdate MapFromMovieEntityToMovieDTOUpdate(Movie movieEntity)
    {
        var movieDTOUpdate = new MovieDTOUpdate()
        {
            Title = movieEntity.Title,
            Year = movieEntity.Year
        };
        return movieDTOUpdate;
    }
                        
    public static Movie MapFromMovieDTOUpdateToMovieEntity(MovieDTOUpdate movieDTOUpdate)
    {
        var movieEntity = new Movie()
        {
            Title = movieDTOUpdate.Title,
            Year = movieDTOUpdate.Year
        };
        return movieEntity;
    }
}