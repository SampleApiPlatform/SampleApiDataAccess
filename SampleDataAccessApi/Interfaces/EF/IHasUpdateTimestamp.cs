// Interface for entities that reuquire automatic Updates
public interface IHasUpdateTimestamp
{
    DateTime UpdatedOn { get; set; }
}
