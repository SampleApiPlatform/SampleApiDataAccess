public class Request
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    
    // true  = Success
    // false = Failed
    // null  = Pending
    public bool? IsSucceeded { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}