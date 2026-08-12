using GameBackend.DailyRewards;
using GameBackend.Models;
using Xunit;

namespace GameBackend.Tests
{
    public class DailyRewardServiceTests
    {
        [Fact]
        public async Task FirstValidClaim_GrantsReward()
        {
            // Arrange
            var service = CreateService();
            var request = CreateValidRequest();

            // Act
            var result = await service.ClaimAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(200, result.HttpStatusCode);
            Assert.True(result.Body.Success);
            Assert.Equal("daily_login", result.Body.QuestId);
            Assert.Equal("title_player_123", result.Body.PlayerEntityId);
            Assert.Equal("2026-07-09", result.Body.ClaimDateUtc);
            Assert.Equal("currency.soft", result.Body.RewardCurrencyId);
            Assert.Equal(50, result.Body.RewardAmount);
            Assert.True(result.Body.NewlyClaimed);
            Assert.False(result.Body.AlreadyClaimed);
            Assert.Equal(50, result.Body.Balance);
            Assert.Null(result.Body.ErrorCode);
            Assert.Null(result.Body.ErrorMessage);
        }

        [Fact]
        public async Task DuplicateClaim_DoesNotGrantTwice()
        {
            // Arrange
            var service = CreateService();
            var request = CreateValidRequest();

            // Act
            var firstResult = await service.ClaimAsync(request, CancellationToken.None);
            var secondResult = await service.ClaimAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(200, firstResult.HttpStatusCode);
            Assert.Equal(200, secondResult.HttpStatusCode);

            Assert.True(firstResult.Body.Success);
            Assert.True(firstResult.Body.NewlyClaimed);
            Assert.False(firstResult.Body.AlreadyClaimed);
            Assert.Equal(50, firstResult.Body.RewardAmount);
            Assert.Equal(50, firstResult.Body.Balance);

            Assert.True(secondResult.Body.Success);
            Assert.False(secondResult.Body.NewlyClaimed);
            Assert.True(secondResult.Body.AlreadyClaimed);
            Assert.Equal(0, secondResult.Body.RewardAmount);
            Assert.Equal(50, secondResult.Body.Balance);
        }

        [Fact]
        public async Task UnknownQuest_Returns400()
        {
            // Arrange
            var wallet = new InMemoryCurrencyWallet();

            var service = CreateService(
                stateStore: new InMemoryDailyRewardStateStore(),
                currencyWallet: wallet);

            var request = CreateValidRequest();
            request.FunctionArgument!.QuestId = "weekly_login";

            // Act
            var result = await service.ClaimAsync(request, CancellationToken.None);

            var balance = await wallet.GetBalanceAsync(
                "title_player_123",
                "currency.soft",
                CancellationToken.None);

            // Assert
            Assert.Equal(400, result.HttpStatusCode);
            Assert.False(result.Body.Success);
            Assert.Equal("UnknownQuest", result.Body.ErrorCode);
            Assert.Equal("Only questId daily_login is supported.", result.Body.ErrorMessage);
            Assert.Equal(0, balance);
        }

        [Fact]
        public async Task MissingCallerEntity_Returns400()
        {
            // Arrange
            var service = CreateService();

            var request = new PlayFabExecuteFunctionRequest
            {
                FunctionArgument = new FunctionArgument
                {
                    QuestId = "daily_login",
                    ClientRequestId = "request-001",
                    ClientPlayerId = "do-not-trust-this-field"
                },
                CallerEntityProfile = null,
                TitleAuthenticationContext = new TitleAuthenticationContext
                {
                    Id = "TEST_TITLE",
                    EntityToken = "mock-title-entity-token"
                }
            };

            // Act
            var result = await service.ClaimAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(400, result.HttpStatusCode);
            Assert.False(result.Body.Success);
            Assert.Equal("MissingCallerEntity", result.Body.ErrorCode);
            Assert.Equal("CallerEntityProfile.Entity.Id and Type are required.", result.Body.ErrorMessage);
        }

        [Fact]
        public async Task Retry_DoesNotGrantTwice()
        {
            // Arrange
            var service = CreateService();

            var firstRequest = CreateValidRequest("request-001");
            var retryRequest = CreateValidRequest("request-002");

            // Act
            var firstResult = await service.ClaimAsync(firstRequest, CancellationToken.None);
            var retryResult = await service.ClaimAsync(retryRequest, CancellationToken.None);

            // Assert
            Assert.Equal(200, firstResult.HttpStatusCode);
            Assert.Equal(200, retryResult.HttpStatusCode);

            Assert.True(firstResult.Body.NewlyClaimed);
            Assert.False(firstResult.Body.AlreadyClaimed);
            Assert.Equal(50, firstResult.Body.RewardAmount);
            Assert.Equal(50, firstResult.Body.Balance);

            Assert.False(retryResult.Body.NewlyClaimed);
            Assert.True(retryResult.Body.AlreadyClaimed);
            Assert.Equal(0, retryResult.Body.RewardAmount);
            Assert.Equal(50, retryResult.Body.Balance);
        }

        [Fact]
        public async Task UpstreamFailure_Returns502()
        {
            // Arrange
            var service = CreateService(
                stateStore: new ThrowingDailyRewardStateStore());

            var request = CreateValidRequest();

            // Act
            var result = await service.ClaimAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(502, result.HttpStatusCode);
            Assert.False(result.Body.Success);
            Assert.Equal("UpstreamPlayFabError", result.Body.ErrorCode);
            Assert.Equal("A mocked inventory or state dependency failed.", result.Body.ErrorMessage);
        }

        private static DailyRewardService CreateService(
            IDailyRewardStateStore? stateStore = null,
            ICurrencyWallet? currencyWallet = null,
            ISystemClock? clock = null,
            DailyRewardOptions? options = null)
        {
            return new DailyRewardService(
                stateStore ?? new InMemoryDailyRewardStateStore(),
                currencyWallet ?? new InMemoryCurrencyWallet(),
                clock ?? new FixedClock(new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero)),
                options ?? new DailyRewardOptions
                {
                    SupportedQuestId = "daily_login",
                    RewardCurrencyId = "currency.soft",
                    RewardAmount = 50
                });
        }

        private static PlayFabExecuteFunctionRequest CreateValidRequest(
            string clientRequestId = "request-001")
        {
            return new PlayFabExecuteFunctionRequest
            {
                FunctionArgument = new FunctionArgument
                {
                    QuestId = "daily_login",
                    ClientRequestId = clientRequestId,
                    ClientPlayerId = "do-not-trust-this-field"
                },
                CallerEntityProfile = new CallerEntityProfile
                {
                    Entity = new Entity
                    {
                        Id = "title_player_123",
                        Type = "title_player_account"
                    }
                },
                TitleAuthenticationContext = new TitleAuthenticationContext
                {
                    Id = "TEST_TITLE",
                    EntityToken = "mock-title-entity-token"
                }
            };
        }

        private class FixedClock : ISystemClock
        {
            public FixedClock(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; }
        }

        private class ThrowingDailyRewardStateStore : IDailyRewardStateStore
        {
            public Task<bool> TryRecordClaimAsync(
                string idempotencyKey,
                CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("Simulated state store failure.");
            }
        }
    }
}