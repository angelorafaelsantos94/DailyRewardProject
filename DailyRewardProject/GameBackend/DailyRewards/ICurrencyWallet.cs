namespace GameBackend.DailyRewards
{
    public interface ICurrencyWallet
    {
        Task<int> GrantAsync(
            string playerEntityId,
            string currencyId,
            int amount,
            CancellationToken cancellationToken);

        Task<int> GetBalanceAsync(
            string playerEntityId,
            string currencyId,
            CancellationToken cancellationToken);
    }
}