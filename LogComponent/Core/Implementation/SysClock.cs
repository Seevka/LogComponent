using LogComponent.Core.Interfaces;

namespace LogComponent.Core.Implementation;

public sealed class SysClock : ISysClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.Now;
}
