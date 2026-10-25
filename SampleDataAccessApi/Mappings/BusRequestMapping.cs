using System.Text.Json;
using NuGet.SampleSharedModels.DTO;
using NuGet.SampleSharedModels.DTO.ServiceBus;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Mappings.MovieMapping;
static class BusRequestMapping
{

    public static BusRequestDTO<MovieDTORead> MapFromBusRequestEntityToBusRequestDTO(BusRequest busRequest)
    {
        var busRequestDTO = new BusRequestDTO<MovieDTORead>()
        {
            Id = busRequest.Id,
            MessageAction = busRequest.MessageAction,
            MessageId = busRequest.MessageId,
            MessageStatus = busRequest.MessageStatus,
            MessageData = !string.IsNullOrEmpty(busRequest.MessageData) ? JsonSerializer.Deserialize<MovieDTORead>(busRequest.MessageData): null,
            CreatedOn = busRequest.CreatedOn,
            UpdatedOn = busRequest.UpdatedOn
        };
        return busRequestDTO;
    }

    public static BusRequest MapFromBusRequestDTOToBusRequestEntity(BusRequestDTO<MovieDTORead> busRequestDTO)
    {
        var busRequest = new BusRequest()
        {
            Id = busRequestDTO.Id,
            MessageAction = busRequestDTO.MessageAction,
            MessageId = busRequestDTO.MessageId,
            MessageStatus = busRequestDTO.MessageStatus,
            MessageData = busRequestDTO.MessageData != null ? JsonSerializer.Serialize<MovieDTORead>(busRequestDTO.MessageData): null,
            CreatedOn = busRequestDTO.CreatedOn,
            UpdatedOn = busRequestDTO.UpdatedOn
        };
        return busRequest;
    }

    
}