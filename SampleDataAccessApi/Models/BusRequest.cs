using NuGet.SampleSharedModels.DTO.ServiceBus;
using NuGet.SampleSharedModels.Enums.EnumsServiceBus;

public class BusRequest: IHasUpdateTimestamp 
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public MessageAction MessageAction { get; set; }
    public MessageStatus MessageStatus { get; set; }
    public string? MessageData { get; set; }  
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}