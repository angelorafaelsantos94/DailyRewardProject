namespace GameBackend.Models;

public class ClaimDailyRewardRequest
{
    public string QuestId { get; set; } = string.Empty;

    public string PlayerEntityId { get; set; } = string.Empty;

    public string? ClientRequestId { get; set; }
}