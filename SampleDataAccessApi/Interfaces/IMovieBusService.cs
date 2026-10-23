using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.Results;

namespace SampleDataAccessApi.Interfaces.MovieInterfaces;
public interface IMovieBusService
{
    Task<ServiceResult<IEnumerable<MovieDTORead>>> GetAll();
    Task<ServiceResult<MovieDTORead?>> GetById(string id);
    Task<ServiceResult<MovieDTORead>> Add(MovieDTORead movieDTOAdd, Guid messageId);
    Task<ServiceResult<MovieDTORead>> Update(MovieDTORead movieDTOUpdate, Guid messageId);
    Task<ServiceResult<bool>> Delete(MovieDTORead movieDTODelete, Guid messageId);
}
