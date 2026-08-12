namespace GameBackend.DailyRewards
{
    public class DailyRewardOptions
    {
        public string SupportedQuestId { get; set; } = "daily_login";

        public string RewardCurrencyId { get; set; } = "currency.soft";

        public int RewardAmount { get; set; } = 50;
    }
}
