using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.Enums.EnumsServiceBus;
using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
using SampleDataAccessApi.Mappings.MovieMapping;
using SampleDataAccessApi.Models;
using SampleDataAccessApi.Validators;



namespace SampleApi.Services.MovieServices;
public class MovieBusService : IMovieBusService
{
    //ASYNC RULE
    //If your method uses await, it must be async.
    //If your method returns a Task directly, it must NOT be async
    private readonly IMovieRepository _movieRepository;
    public readonly IBusRequestRepository _busRequestRepository;
    private readonly IValidator<Movie> _movieValidator;
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<MovieBusService> _logger;

    public MovieBusService(
        IMovieRepository movieRepository, 
        IBusRequestRepository busRequestRepository,
        IValidator<Movie> movieValidator,
        ILogger<MovieBusService> logger,
        IUnitOfWork unitOfWork)
    {
        _movieRepository = movieRepository;
        _busRequestRepository = busRequestRepository;
        _movieValidator = movieValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
    public async Task<ServiceResult<MovieDTORead>> Add(MovieDTORead movieDTOAdd, Guid messageId)
    {
        // we do that is case of duplicated MessagesIds to avoid to recover the entity in the DB
        var movieDTOReadForDuplicatedMessageId = movieDTOAdd;
        // we need to this otherwise the movie is inserted in the table without an Id
        var movieDTOAdd1 = new MovieDTOAdd()
        {
            Title = movieDTOAdd.Title,
            Year = movieDTOAdd.Year
        };
        //var serviceResult = new ServiceResult<MovieDTORead>();
        // map               
        var movieEntity = MovieMapping.MapFromMovieDTOAddToMovieEntity(movieDTOAdd1);
        // validate
        var validationResult = _movieValidator.Validate(movieEntity);
        if (!validationResult.IsValid)
            return ServiceResult<MovieDTORead>.Fail(validationResult.Errors);
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
           //EF Core tracks the entity that we have just added and it assigns an ID (with SQL Server)
            await _movieRepository.Add(movieEntity);  
            var dtoRead = MovieMapping.MapFromMovieEntityToMovieDTORead(movieEntity); 
            // provide the 
            movieDTOReadForDuplicatedMessageId.Id = dtoRead.Id;
            // create a record in a busRequest table  
            var busRequest = new BusRequest()
            {
                MessageAction = MessageAction.Add,
                // inside this MessageData we have the id that we can use to lookup at the Movies table
                MessageData = JsonSerializer.Serialize(dtoRead),
                MessageId = messageId,
                MovieId = Guid.Parse(dtoRead.Id),
                MessageStatus = MessageStatus.Success
            }; 
            var busRequestValidator = new BusRequestValidator();
            var validationBusResult = busRequestValidator.Validate(busRequest);
            if (validationBusResult.IsValid)
            {
                await _busRequestRepository.Add(busRequest); 
                // 3. All good → Commit
                await transaction.CommitAsync();
                return ServiceResult<MovieDTORead>.Ok(dtoRead);                    
            }
            else
            {
                var errorText = string.Join("; ", validationBusResult.Errors);
                throw new Exception($"The BusRequest is not valid:{errorText}");
            }
        }
        catch (DbUpdateException ex)
        {
           // CASE: Duplicate Key → Exists already a record in the BusRequests table with the same MessageId and MessageStatus = Succeeded
           // The MessageStatus = Fail is only set when the message has reached the DLQ. Here cannot happen
           // The MessageStatus = Pending is never registered
           // So if the ,essageId exist it is only because it is succeeded and in this case we have to retuen success 
            if (IsDuplicateKeyError(ex))
            {
                _logger.LogWarning("MessageId {MessageId} already exists (duplicate). Treating as success.", messageId);
                // remove the movie that we have added.
                await transaction.RollbackAsync();
                return ServiceResult<MovieDTORead>.Ok(movieDTOReadForDuplicatedMessageId); // Erfolg melden!
            }

            // 2. FALL: Echter Fehler → Rollback und Fehler weitergeben
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to add Movie for MessageId {MessageId}", messageId);
            throw; // Wirf die Exception weiter, damit der Receiver sie fängt
        }
        // scope wird hier automatisch disposed → Rollback wenn nicht complete                
            
        
    }  
    public async Task<ServiceResult<MovieDTORead>> Update(MovieDTORead movieDTOUpdate, Guid messageId)
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

        var movieEntity = await _movieRepository.GetById(movieDTOUpdate.Id);

        if (movieEntity == null)
            return  ServiceResult<MovieDTORead>.Fail(["Entity Not Found"]);

        movieEntity.Title = movieDTOUpdate.Title;
        movieEntity.Year = movieDTOUpdate.Year;

        var validationResult = _movieValidator.Validate(movieEntity);
        if (!validationResult.IsValid)
            return ServiceResult<MovieDTORead>.Fail(validationResult.Errors); 
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
           //EF Core tracks the entity that we have just added
           await _movieRepository.Update(movieEntity); //<-
           var dtoRead = MovieMapping.MapFromMovieEntityToMovieDTORead(movieEntity);      
            // create a record in a busRequest table  
            var busRequest = new BusRequest()
            {
                MessageAction = MessageAction.Update,
                // inside this MessageData we have the id that we can use to lookup at the Movies table
                MessageData = JsonSerializer.Serialize(dtoRead),
                MessageId = messageId,
                MessageStatus = MessageStatus.Success
            }; 
            var busRequestValidator = new BusRequestValidator();
            var validationBusResult = busRequestValidator.Validate(busRequest);
            if (validationBusResult.IsValid)
            {
                await _busRequestRepository.Add(busRequest); 
                // 3. All good → Commit
                await transaction.CommitAsync();
                return ServiceResult<MovieDTORead>.Ok(dtoRead);                    
            }
            else
            {
                var errorText = string.Join("; ", validationBusResult.Errors);
                throw new Exception($"The BusRequest is not valid:{errorText}");
            }
        }
        catch (DbUpdateException ex)
        {
           // CASE: Duplicate Key → Exists already a record in the BusRequests table with the same MessageId and MessageStatus = Succeeded
           // The MessageStatus = Fail is only set when the message has reached the DLQ. Here cannot happen
           // The MessageStatus = Pending is never registered
           // So if the ,essageId exist it is only because it is succeeded and in this case we have to retuen success 
            if (IsDuplicateKeyError(ex))
            {
                _logger.LogWarning("MessageId {MessageId} already exists (duplicate). Treating as success.", messageId);
                // remove the movie that we have added.
                await transaction.RollbackAsync();
                return ServiceResult<MovieDTORead>.Ok(movieDTOUpdate); // Erfolg melden!
            }

            // 2. FALL: Echter Fehler → Rollback und Fehler weitergeben
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to add Movie for MessageId {MessageId}", messageId);
            throw; // Wirf die Exception weiter, damit der Receiver sie fängt
        }   
    }
    public async Task<ServiceResult<bool>> Delete(MovieDTORead movieDTODelete, Guid messageId)
    {
        //if the entity cannot be found the EF will return false.
        //var deleted = await _movieRepository.Delete(id);
        //return deleted;

        var movie = await _movieRepository.GetById(movieDTODelete.Id);
        if (movie == null)
            return ServiceResult<bool>.Fail(["Entity Not Found"]);
        // Optional: cross-entity validation
        //var validation = _movieValidator.Validate(movie);
        //if (!validation.IsValid)
        //    return ServiceResult<bool>.Fail(validation.Errors);
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
           //EF Core tracks the entity that we have just added
           await _movieRepository.Delete(movie);//<-
           var dtoRead = MovieMapping.MapFromMovieEntityToMovieDTORead(movie);      
            // create a record in a busRequest table  
            var busRequest = new BusRequest()
            {
                MessageAction = MessageAction.Delete,
                // inside this MessageData we have the id that we can use to lookup at the Movies table
                MessageData = JsonSerializer.Serialize(dtoRead),
                MessageId = messageId,
                MessageStatus = MessageStatus.Success
            }; 
            var busRequestValidator = new BusRequestValidator();
            var validationBusResult = busRequestValidator.Validate(busRequest);
            if (validationBusResult.IsValid)
            {
                await _busRequestRepository.Add(busRequest); 
                // 3. All good → Commit
                await transaction.CommitAsync();
                return ServiceResult<bool>.Ok(true);                  
            }
            else
            {
                var errorText = string.Join("; ", validationBusResult.Errors);
                throw new Exception($"The BusRequest is not valid:{errorText}");
            }
        }
        catch (DbUpdateException ex)
        {
           // CASE: Duplicate Key → Exists already a record in the BusRequests table with the same MessageId and MessageStatus = Succeeded
           // The MessageStatus = Fail is only set when the message has reached the DLQ. Here cannot happen
           // The MessageStatus = Pending is never registered
           // So if the ,essageId exist it is only because it is succeeded and in this case we have to retuen success 
            if (IsDuplicateKeyError(ex))
            {
                _logger.LogWarning("MessageId {MessageId} already exists (duplicate). Treating as success.", messageId);
                // remove the movie that we have added.
                await transaction.RollbackAsync();
                return ServiceResult<bool>.Ok(true); // Erfolg melden!
            }

            // 2. FALL: Echter Fehler → Rollback und Fehler weitergeben
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to add Movie for MessageId {MessageId}", messageId);
            throw; // Wirf die Exception weiter, damit der Receiver sie fängt
        }
    }

    private static bool IsDuplicateKeyError(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            // 2601 = Unique Index violated
            // 2627 = Primary Key violated
            return sqlEx.Number == 2601 || sqlEx.Number == 2627;
        }
        return false;
    }
}