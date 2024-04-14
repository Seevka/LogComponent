namespace LogComponent.Core.Interfaces;

public interface ISysClock
{
    public DateTime UtcNow { get; }

    public DateTime Now { get; }
}
