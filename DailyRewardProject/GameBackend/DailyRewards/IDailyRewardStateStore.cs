namespace GameBackend.DailyRewards
{
    public interface IDailyRewardStateStore
    {
        Task<bool> TryRecordClaimAsync(
            string idempotencyKey,
            CancellationToken cancellationToken);
    }
}