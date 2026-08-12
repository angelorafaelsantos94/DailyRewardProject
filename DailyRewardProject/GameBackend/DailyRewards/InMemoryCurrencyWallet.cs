using System.Collections.Concurrent;

namespace GameBackend.DailyRewards
{
    public class InMemoryCurrencyWallet : ICurrencyWallet
    {
        private readonly ConcurrentDictionary<string, int> _balances = new();

        public Task<int> GrantAsync(
            string playerEntityId,
            string currencyId,
            int amount,
            CancellationToken cancellationToken)
        {
            var walletKey = BuildWalletKey(playerEntityId, currencyId);

            var newBalance = _balances.AddOrUpdate(
                walletKey,
                amount,
                (_, currentBalance) => currentBalance + amount);

            return Task.FromResult(newBalance);
        }

        public Task<int> GetBalanceAsync(
            string playerEntityId,
            string currencyId,
            CancellationToken cancellationToken)
        {
            var walletKey = BuildWalletKey(playerEntityId, currencyId);

            _balances.TryGetValue(walletKey, out var balance);

            return Task.FromResult(balance);
        }

        private static string BuildWalletKey(string playerEntityId, string currencyId)
        {
            return $"{playerEntityId}:{currencyId}";
        }
    }
}