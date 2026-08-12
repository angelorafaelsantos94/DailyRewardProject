using System.Net;
using System.Text.Json;
using GameBackend.DailyRewards;
using GameBackend.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace GameBackend.Functions
{
    public class ClaimDailyRewardV1Function
    {
        private readonly DailyRewardService _dailyRewardService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ClaimDailyRewardV1Function(DailyRewardService dailyRewardService)
        {
            _dailyRewardService = dailyRewardService;
        }

        [Function("ClaimDailyRewardV1")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData request,
            CancellationToken cancellationToken)
        {
            PlayFabExecuteFunctionRequest? playFabRequest;

            try
            {
                playFabRequest =
                    await JsonSerializer.DeserializeAsync<PlayFabExecuteFunctionRequest>(
                        request.Body,
                        JsonOptions,
                        cancellationToken);
            }
            catch (JsonException)
            {
                var badRequestResult = new ClaimDailyRewardResult(
                    400,
                    ClaimDailyRewardResponse.Error(
                        "BadRequest",
                        "Request body is malformed or required fields are missing."));

                return await WriteJsonResponseAsync(
                    request,
                    badRequestResult,
                    cancellationToken);
            }

            var result = await _dailyRewardService.ClaimAsync(
                playFabRequest,
                cancellationToken);

            return await WriteJsonResponseAsync(
                request,
                result,
                cancellationToken);
        }

        private static async Task<HttpResponseData> WriteJsonResponseAsync(
            HttpRequestData request,
            ClaimDailyRewardResult result,
            CancellationToken cancellationToken)
        {
            var response = request.CreateResponse(
                (HttpStatusCode)result.HttpStatusCode);

            await response.WriteAsJsonAsync(
                result.Body,
                cancellationToken);

            return response;
        }
    }
}