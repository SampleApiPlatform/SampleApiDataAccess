using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using SampleDataAccessApi.Models;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
using SampleDataAccessApi.Services.MovieServices;
using SampleDataAccessApi.DTOs.Movies;
using SampleDataAccessApi.Validators;
using System.Net.Cache;


public class MovieServiceTests
{
    //GetById
    [Fact]
    public async Task GetById_ReturnsMovie_WhenIdDoesMatchSetup()
    {
        // Arrange
        string id = "10";
        var movie = new Movie { Id = "10", Title = "A", Year = 2000 };

        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.GetById(id))
                .ReturnsAsync(movie);

        var validatorMock = new Mock<IValidator<Movie>>();

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.GetById(id);  // repository returns a movie

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result?.Id);

        repoMock.Verify(r => r.GetById(id), Times.Once);
    }

   
    [Fact]
    public async Task GetById_ReturnsNull_WhenIdDoesNotMatchSetup()
    {
        // Arrange
        string id = "10";
        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.GetById(id))
                .ReturnsAsync((Movie?)null);

        var validatorMock = new Mock<IValidator<Movie>>();

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.GetById(id);  // ID not configured

        // Assert
        Assert.Null(result);

        repoMock.Verify(r => r.GetById(id), Times.Once);
    }

    //GetAll
    [Fact]
    public async Task GetAll_ReturnsEmpty()
    {
        // Arrange
        var movies = new List<Movie>();

        var movieRepositiory_Mock = new Mock<IMovieRepository>();
        movieRepositiory_Mock.Setup(r => r.GetAll())
                .ReturnsAsync(movies);

        var validator_Mock = new Mock<IValidator<Movie>>();

        var movieService = new MovieService(movieRepositiory_Mock.Object, validator_Mock.Object);

        // Act
        var result = await movieService.GetAll();

        // Assert
        Assert.Empty(result);

        movieRepositiory_Mock.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsMappedDTOs_WithCorrectCount()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "A", Year = 2000 },
            new Movie { Id = "2", Title = "B", Year = 2001 }
        };

        var movieRepositiory_Mock = new Mock<IMovieRepository>();
        movieRepositiory_Mock.Setup(r => r.GetAll())
                .ReturnsAsync(movies);

        var validator_Mock = new Mock<IValidator<Movie>>();
        // No validation is used in GetAll(), so no setup needed

        var movieService = new MovieService(movieRepositiory_Mock.Object, validator_Mock.Object);

        // Act
        var result = await movieService.GetAll();

        // Assert
        Assert.Equal(movies.Count, result.Count());
    }

    // Add
    [Fact]
    public async Task Add_MappingDTOs_Correct()
    {
        // Arrange
        var movieDTOAdd = new MovieDTOAdd { Title = "A", Year = 2000 };

        Movie? capturedEntity = null;

        var repoMock = new Mock<IMovieRepository>();

        //This is the key part:
        // When the service calls Add(movie),
        // take the movie object and store it in the variable capturedEntity.
        // In other words, we will check that the object that we are sending to the repository
        // corresponds to the DTO that it has originated
        repoMock.Setup(r => r.Add(It.IsAny<Movie>()))
                .Callback<Movie>(m => capturedEntity = m)
                .Returns(Task.CompletedTask);

        var validatorMock = new Mock<IValidator<Movie>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<Movie>()))
                    .Returns(new ValidationResult
                    {
                        IsValid = true,
                        Errors = new List<string>()
                    });

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Add(movieDTOAdd);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(capturedEntity);

        // Entity mapping correctness
        Assert.Equal(movieDTOAdd.Title, capturedEntity!.Title);
        Assert.Equal(movieDTOAdd.Year, capturedEntity.Year);

        // DTORead mapping correctness
        Assert.Equal(movieDTOAdd.Title, result.Data!.Title);
        Assert.Equal(movieDTOAdd.Year, result.Data.Year);

        repoMock.Verify(r => r.Add(It.IsAny<Movie>()), Times.Once);
        validatorMock.Verify(v => v.Validate(It.IsAny<Movie>()), Times.Once);
    }

    [Fact]
    public async Task Add_AddsRecord_WhenValidationPasses()
    {
        // Arrange
        var movieDTOAdd = new MovieDTOAdd { Title = "A", Year = 2000 };

        var movieEntity = new Movie { Id = "1", Title = "A", Year = 2000 };

        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.Add(It.IsAny<Movie>()))
                .Returns(Task.CompletedTask);   // repository returns Task, not DTO

        var validatorMock = new Mock<IValidator<Movie>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<Movie>()))
                    .Returns(new ValidationResult
                    {
                        IsValid = true,
                        Errors = new List<string>()
                    });

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Add(movieDTOAdd);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Errors);

        // Verify repository was called
        repoMock.Verify(r => r.Add(It.IsAny<Movie>()), Times.Once);

        // Verify validator was called
        validatorMock.Verify(v => v.Validate(It.IsAny<Movie>()), Times.Once);
    }

    [Fact]
    public async Task Add_ReturnsError_WhenYearIsBelow1800()
    {
        // Arrange
        var movieDTOAdd = new MovieDTOAdd { Title = "A", Year = 1700 };

        var validatorMock = new Mock<IValidator<Movie>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<Movie>()))
                    .Returns(new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "The years must be greater than 1799" }
                    });

        var repoMock = new Mock<IMovieRepository>();

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Add(movieDTOAdd);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The years must be greater than 1799", result.Errors);

        repoMock.Verify(r => r.Add(It.IsAny<Movie>()), Times.Never);
    }

    [Fact]
    public async Task Add_ReturnsError_WhenTitleIsMissing()
    {
        // Arrange
        var movieDTOAdd = new MovieDTOAdd { Title = "", Year = 2000 };

        var validatorMock = new Mock<IValidator<Movie>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<Movie>()))
                    .Returns(new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "The Title of the Movie cannot be empty" }
                    });

        var repoMock = new Mock<IMovieRepository>();

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Add(movieDTOAdd);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The Title of the Movie cannot be empty", result.Errors);

        repoMock.Verify(r => r.Add(It.IsAny<Movie>()), Times.Never);
    }

    [Fact]
    public async Task Add_ReturnsError_WhenYearIsZero()
    {
        // Arrange
        var movieDTOAdd = new MovieDTOAdd { Title = "A", Year = 0 };

        var validatorMock = new Mock<IValidator<Movie>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<Movie>()))
                    .Returns(new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "The years must be greater than 1799" }
                    });

        var repoMock = new Mock<IMovieRepository>();

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Add(movieDTOAdd);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The years must be greater than 1799", result.Errors);

        repoMock.Verify(r => r.Add(It.IsAny<Movie>()), Times.Never);
    }

    //Update
   [Fact]
    public async Task UpdatesRecord_WhenValidationPasses()
    {
        // Arrange
        string id = "1";
        var movieDTOUpdate = new MovieDTOUpdate { Title = "B", Year = 2001 };

        var movieEntity = new Movie { Id = "1", Title = "A", Year = 2000 };

        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.GetById(id)).ReturnsAsync(movieEntity);
        repoMock.Setup(r => r.Update(It.IsAny<Movie>()))
                .Returns(Task.CompletedTask);

        var validatorMock = new Mock<IValidator<Movie>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<Movie>()))
                    .Returns(new ValidationResult
                    {
                        IsValid = true,
                        Errors = new List<string>()
                    });

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Update(id, movieDTOUpdate);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Errors);

        // Verify repository was called
        repoMock.Verify(r => r.Update(It.IsAny<Movie>()), Times.Once);

        // Verify validator was called
        validatorMock.Verify(v => v.Validate(It.IsAny<Movie>()), Times.Once);
    }

    [Fact]
    public async Task FailUpdate_WhenIdNotFound()
    {
        // Arrange
        string id = "2";
        var movieDTOUpdate = new MovieDTOUpdate { Title = "B", Year = 2001 };

        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.GetById(id))
                .ReturnsAsync((Movie?)null);   // simulate not found

        // Update should never be called
        repoMock.Setup(r => r.Update(It.IsAny<Movie>()))
                .Returns(Task.CompletedTask);

        var validatorMock = new Mock<IValidator<Movie>>();
        // Validator should NOT be called, so no setup needed

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Update(id, movieDTOUpdate);

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("Entity Not Found", result.Errors);

        // Verify repository Update was NOT called
        repoMock.Verify(r => r.Update(It.IsAny<Movie>()), Times.Never);

        // Verify validator was NOT called
        validatorMock.Verify(v => v.Validate(It.IsAny<Movie>()), Times.Never);
    }

    [Fact]
    public async Task DeletesRecord_WhenFound()
    {
        // Arrange
        string id = "1";

        var movieEntity = new Movie { Id = "1", Title = "A", Year = 2000 };

        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.GetById(id))
                .ReturnsAsync(movieEntity);

        repoMock.Setup(r => r.Delete(It.IsAny<Movie>()))
                .Returns(Task.CompletedTask);

        var validatorMock = new Mock<IValidator<Movie>>();
        // Validator is NOT used in Delete, so no setup needed

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Delete(id);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Errors);

        // Verify repository Delete was called
        repoMock.Verify(r => r.Delete(It.IsAny<Movie>()), Times.Once);

        // Verify validator was NOT called
        validatorMock.Verify(v => v.Validate(It.IsAny<Movie>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFails_WhenNotFound()
    {
        // Arrange
        string id = "2";

        var repoMock = new Mock<IMovieRepository>();
        repoMock.Setup(r => r.GetById(id))
                .ReturnsAsync((Movie?)null);   // simulate not found

        repoMock.Setup(r => r.Delete(It.IsAny<Movie>()))
                .Returns(Task.CompletedTask);

        var validatorMock = new Mock<IValidator<Movie>>();
        // Validator is NOT used in Delete, so no setup needed

        var service = new MovieService(repoMock.Object, validatorMock.Object);

        // Act
        var result = await service.Delete(id);

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("Entity Not Found", result.Errors);

        // Verify repository Delete was NOT called
        repoMock.Verify(r => r.Delete(It.IsAny<Movie>()), Times.Never);

        // Verify validator was NOT called
        validatorMock.Verify(v => v.Validate(It.IsAny<Movie>()), Times.Never);
    }


}
