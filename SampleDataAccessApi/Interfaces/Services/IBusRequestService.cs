using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.DTO.ServiceBus;
using NuGet.SampleSharedModels.Results;

namespace SampleDataAccessApi.Interfaces.MovieInterfaces;
public interface IBusRequestService
{
    Task<ServiceResult<BusRequestDTO<MovieDTORead>>> GetByMessageId(Guid messageId);
    Task<ServiceResult<BusRequestDTO<MovieDTORead>>> Update(BusRequestDTO<MovieDTORead> busRequestDTO);

    /*Task<ServiceResult<IEnumerable<MovieDTORead>>> GetAll();
    Task<ServiceResult<MovieDTORead?>> GetById(string id);
    Task<ServiceResult<MovieDTORead>> Add(MovieDTORead movieDTOAdd, Guid messageId);
    Task<ServiceResult<MovieDTORead>> Update(MovieDTORead movieDTOUpdate, Guid messageId);
    Task<ServiceResult<bool>> Delete(MovieDTORead movieDTODelete, Guid messageId);*/
}