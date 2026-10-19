using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
namespace SampleDataAccessApi.Controllers;

//using the repositiory pattern
//ASYNC RULE
//If your method uses await, it must be async.
//If your method returns a Task directly, it must NOT be async

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AccessAsUser")]
[ApiController]
[Route("api/bus/movies")]
public class MoviesBusController : ControllerBase
{
    private readonly IMovieBusService _movieService;
    private readonly ILogger<MoviesController> _logger;
    private readonly string controllerName = string.Empty;

    public MoviesBusController(
                            //IMovieService movieService, 
                            IMovieBusService movieService,
                            ILogger<MoviesController> logger
                            )
    {
        _movieService = movieService;
        _logger = logger;
        controllerName = GetType().Name;
    }

    [HttpGet]
    public async  Task<ActionResult<IEnumerable<MovieDTORead>>> GetAll()
    {
        try
        {
            var serviceResult = await _movieService.GetAll();
            return Ok(serviceResult);
        }
        catch (Exception ex)
        {
            var message = $"MovieController.GetAll Exception: {ex.Message}";

           _logger.LogError(
                message
            );

            return StatusCode(500, message);
        }
        
    }

    

    [HttpGet("{id}")]
    public async Task<ActionResult<MovieDTORead>> GetById(string id)
    {
        try
        {
            var serviceResult = await _movieService.GetById(id);
            if (serviceResult == null)
            {
                _logger.LogInformation("MovieController.GetById Movie not found: {id}", id);
                return NotFound(new { id });
            }
            return Ok(serviceResult);
        }
        catch (Exception ex)
        {
             var message = $"MovieController.GetById Exception: {ex.Message}";

           _logger.LogError(
                message
            );
            return StatusCode(500, message);
        }        
    }

}
