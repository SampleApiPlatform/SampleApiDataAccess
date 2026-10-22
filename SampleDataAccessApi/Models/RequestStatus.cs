public class RequestStatus
{
    public Guid Id { get; set; }          // oder Guid, je nach Stil
    public int Code { get; set; }   // 'Pending', 'Fail', 'Success'
    public string Description { get; set; } = "";
}