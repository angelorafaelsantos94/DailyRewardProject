namespace GameBackend.DailyRewards
{
    public interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }
    }
}