using System.Collections.Concurrent;

namespace GameBackend.DailyRewards
{
    public class InMemoryDailyRewardStateStore : IDailyRewardStateStore
    {
        private readonly ConcurrentDictionary<string, byte> _claims = new();

        public Task<bool> TryRecordClaimAsync(
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var wasAdded = _claims.TryAdd(idempotencyKey, 0);

            return Task.FromResult(wasAdded);
        }
    }
}