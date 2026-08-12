namespace GameBackend.Models
{
    public class ClaimDailyRewardResponse
    {
        public bool Success { get; set; }
        public string? QuestId { get; set; }
        public string? PlayerEntityId { get; set; }
        public string? ClaimDateUtc { get; set; }
        public string? RewardCurrencyId { get; set; }
        public int RewardAmount { get; set; }
        public bool NewlyClaimed { get; set; }
        public bool AlreadyClaimed { get; set; }
        public int? Balance { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public static ClaimDailyRewardResponse Error(
            string errorCode,
            string errorMessage,
            string? questId = null,
            string? playerEntityId = null)
        {
            return new ClaimDailyRewardResponse
            {
                Success = false,
                QuestId = questId,
                PlayerEntityId = playerEntityId,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }

    public class ClaimDailyRewardResult
    {
        public int HttpStatusCode { get; set; }

        public ClaimDailyRewardResponse Body { get; set; } = new();

        public ClaimDailyRewardResult()
        {
        }

        public ClaimDailyRewardResult(
            int httpStatusCode,
            ClaimDailyRewardResponse body)
        {
            HttpStatusCode = httpStatusCode;
            Body = body;
        }
    }
}