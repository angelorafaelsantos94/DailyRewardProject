using GameBackend.Models;

namespace GameBackend.DailyRewards
{
    public class DailyRewardService
    {
        private readonly IDailyRewardStateStore _stateStore;
        private readonly ICurrencyWallet _currencyWallet;
        private readonly ISystemClock _clock;
        private readonly DailyRewardOptions _options;

        public DailyRewardService(
            IDailyRewardStateStore stateStore,
            ICurrencyWallet currencyWallet,
            ISystemClock clock,
            DailyRewardOptions options)
        {
            _stateStore = stateStore;
            _currencyWallet = currencyWallet;
            _clock = clock;
            _options = options;
        }

        public async Task<ClaimDailyRewardResult> ClaimAsync(
            PlayFabExecuteFunctionRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return CreateErrorResult(
                    400,
                    "BadRequest",
                    "Request body is malformed or required fields are missing.");
            }

            var questId = request.FunctionArgument?.QuestId;

            if (string.IsNullOrWhiteSpace(request.CallerEntityProfile?.Entity?.Id) ||
                string.IsNullOrWhiteSpace(request.CallerEntityProfile?.Entity?.Type))
            {
                return CreateErrorResult(
                    400,
                    "MissingCallerEntity",
                    "CallerEntityProfile.Entity.Id and Type are required.",
                    questId);
            }

            var playerEntityId = request.CallerEntityProfile.Entity.Id.Trim();

            if (!string.Equals(questId, _options.SupportedQuestId, StringComparison.Ordinal))
            {
                return CreateErrorResult(
                    400,
                    "UnknownQuest",
                    "Only questId daily_login is supported.",
                    questId,
                    playerEntityId);
            }

            var claimDateUtc = _clock.UtcNow.UtcDateTime.Date.ToString("yyyy-MM-dd");

            var idempotencyKey =
                $"{_options.SupportedQuestId}:{playerEntityId}:{claimDateUtc}";

            try
            {
                var newlyRecorded = await _stateStore.TryRecordClaimAsync(
                    idempotencyKey,
                    cancellationToken);

                if (!newlyRecorded)
                {
                    var existingBalance = await _currencyWallet.GetBalanceAsync(
                        playerEntityId,
                        _options.RewardCurrencyId,
                        cancellationToken);

                    return CreateSuccessResult(
                        playerEntityId,
                        claimDateUtc,
                        rewardAmount: 0,
                        newlyClaimed: false,
                        alreadyClaimed: true,
                        balance: existingBalance);
                }

                var newBalance = await _currencyWallet.GrantAsync(
                    playerEntityId,
                    _options.RewardCurrencyId,
                    _options.RewardAmount,
                    cancellationToken);

                return CreateSuccessResult(
                    playerEntityId,
                    claimDateUtc,
                    rewardAmount: _options.RewardAmount,
                    newlyClaimed: true,
                    alreadyClaimed: false,
                    balance: newBalance);
            }
            catch
            {
                return CreateErrorResult(
                    502,
                    "UpstreamPlayFabError",
                    "A mocked inventory or state dependency failed.",
                    questId,
                    playerEntityId);
            }
        }

        private ClaimDailyRewardResult CreateSuccessResult(
            string playerEntityId,
            string claimDateUtc,
            int rewardAmount,
            bool newlyClaimed,
            bool alreadyClaimed,
            int balance)
        {
            return new ClaimDailyRewardResult(
                200,
                new ClaimDailyRewardResponse
                {
                    Success = true,
                    QuestId = _options.SupportedQuestId,
                    PlayerEntityId = playerEntityId,
                    ClaimDateUtc = claimDateUtc,
                    RewardCurrencyId = _options.RewardCurrencyId,
                    RewardAmount = rewardAmount,
                    NewlyClaimed = newlyClaimed,
                    AlreadyClaimed = alreadyClaimed,
                    Balance = balance,
                    ErrorCode = null,
                    ErrorMessage = null
                });
        }

        private static ClaimDailyRewardResult CreateErrorResult(
            int httpStatusCode,
            string errorCode,
            string errorMessage,
            string? questId = null,
            string? playerEntityId = null)
        {
            return new ClaimDailyRewardResult(
                httpStatusCode,
                ClaimDailyRewardResponse.Error(
                    errorCode,
                    errorMessage,
                    questId,
                    playerEntityId));
        }
    }
}