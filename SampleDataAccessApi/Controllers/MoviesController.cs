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

//[Authorize]: in the dapr context no need to use it asAzure is managing the connections as trusted
[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    //private readonly ISharedServicesClient _logger;
    private readonly ILogger<MoviesController> _logger;
    private readonly string controllerName = string.Empty;

    public MoviesController(IMovieService movieService, 
                            ILogger<MoviesController> logger
                           //ISharedServicesClient logger,
                            )
    {
        _movieService = movieService;
        _logger = logger;
        //_logger = logger;
        controllerName = GetType().Name;
    }

    [HttpGet]
    public async  Task<ActionResult<IEnumerable<MovieDTORead>>> GetAll()
    {
        try
        {
            var movies = await _movieService.GetAll();
            return Ok(movies);
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
            var MovieDTORead = await _movieService.GetById(id);
            if (MovieDTORead == null)
            {
                _logger.LogInformation("MovieController.GetById Movie not found: {id}", id);
                //await _logger.LogAsync(
                //        controllerName,
                //        $"GetById . Entity Not found: {id}",
                //        LogLevel.Warning);
                return NotFound(new { id });
            }
            return Ok(MovieDTORead);
        }
        catch (Exception ex)
        {
             var message = $"MovieController.GetById Exception: {ex.Message}";

           _logger.LogError(
                message
            );

            return StatusCode(500, message);
        //    var message = $"MovieController.GetById Exception: {ex.Message}";

        //    await _logger.LogAsync(
        //        controllerName,
        //        message,
        //        LogLevel.Error
        //    );

        //    return StatusCode(500, message);
        }        
    }

    //[Authorize]
    [HttpPost]
    public async Task<IActionResult> Add(MovieDTOAdd movieDTOAdd)
    {
        try
        {
            var serviceResult =await _movieService.Add(movieDTOAdd);
            if (!serviceResult.Success)
            {
                _logger.LogWarning("MovieController.Create Failed: Reason={Reason}", ServiceResult<MovieDTORead>.ErrorsToString(serviceResult.Errors));
                //await _logger.LogAsync(
                //        controllerName,
                //        $"Add . Entity Not Added. Reason: {ServiceResult<MovieDTORead>.ErrorsToString(serviceResult.Errors)}",
                //        LogLevel.Warning);
                return BadRequest(serviceResult.Errors);
            }


            //CreatedAtAction is NOT from EF Core.  
            //It comes from ASP.NET Core MVC, specifically from the ControllerBase class.
            //⭐ What CreatedAtAction actually does
            //It builds an HTTP 201 Created response and includes:
            //the Location header (URL of the newly created resource)
            //the response body (your DTO)
            _logger.LogInformation("MovieController.Create Movie Created: {Id}", serviceResult.Data?.Id);
            //await _logger.LogAsync(
            //            controllerName,
            //            $"Add . Entity Added Successfully. id: {serviceResult.Data?.Id}",
            //            LogLevel.Information);
            return Ok(serviceResult);
            //return CreatedAtAction(nameof(GetById), new { id = serviceResult.Data!.Id }, serviceResult.Data);
            // null‑forgiving operator: serviceResult.Data!.Id
            // serviceResult.Data!.Id: serviceResult.Data can be null, so Data.Id would fail.
            // It tells the compiler:I know this value is not null here — trust me.        
        }
        catch(Exception ex)
        {
            var message = $"MovieController.Add Exception: {ex.Message}";

           _logger.LogError(
                message
            );

            return StatusCode(500, message);
        //    var message = $"MovieController.GetById Exception: {ex.Message}";

        //    await _logger.LogAsync(
        //        controllerName,
        //        message,
        //        LogLevel.Error
        //    );

        //    return StatusCode(500, message);
        }
        
    }

    //[Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, MovieDTOUpdate movieDTOUpdate)
    {
        try
        {
            var serviceResult =await _movieService.Update(id, movieDTOUpdate);
            if (!serviceResult.Success)
            {
                _logger.LogWarning("MovieController.Update Failed: Reason={Reason}", ServiceResult<MovieDTORead>.ErrorsToString(serviceResult.Errors));
                //await _logger.LogAsync(
                //        controllerName,
                //        $"Update . Update Failed. Reason: {ServiceResult<MovieDTORead>.ErrorsToString(serviceResult.Errors)}",
                //        LogLevel.Warning);
                return BadRequest(serviceResult.Errors);
            }
            _logger.LogInformation("MovieController.Update Movie Updated: {Id}", serviceResult.Data?.Id);
            //await _logger.LogAsync(
            //            controllerName,
            //            $"Update . Update Successful. Id: {serviceResult.Data?.Id}",
            //            LogLevel.Warning);
            return Ok(serviceResult.Data);
        }
        catch (Exception ex)
        {
            var message = $"MovieController.Update Exception: {ex.Message}";

           _logger.LogError(
                message
            );

            return StatusCode(500, message);
        //    var message = $"MovieController.GetById Exception: {ex.Message}";

        //    await _logger.LogAsync(
        //        controllerName,
        //        message,
        //        LogLevel.Error
        //    );

        //    return StatusCode(500, message);
        }
        
    }

    //[Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var serviceResult = await _movieService.Delete(id);
            if (!serviceResult.Success)
            {
                _logger.LogWarning("MovieController.Delete Failed: Reason={Reason}", ServiceResult<bool>.ErrorsToString(serviceResult.Errors));
                //await _logger.LogAsync(
                //        controllerName,
                //        $"Add . Delete Failed. Reason: {ServiceResult<bool>.ErrorsToString(serviceResult.Errors)}",
                //        LogLevel.Warning);
                return BadRequest(serviceResult.Errors);
            }

            _logger.LogInformation("MovieController.Delete Movie Deleted: {Id}", id);
            //await _logger.LogAsync(
            //            controllerName,
            //            $"Add . Delete Successful. Id: {id}",
            //            LogLevel.Warning);
            return Ok(true); 
        }
        catch (Exception ex)
        {
            var message = $"MovieController.Delete Exception: {ex.Message}";

           _logger.LogError(
                message
            );

            return StatusCode(500, message);
        //    var message = $"MovieController.GetById Exception: {ex.Message}";

        //    await _logger.LogAsync(
        //        controllerName,
        //        message,
        //        LogLevel.Error
        //    );

        //    return StatusCode(500, message);
        }
        
    }
}
