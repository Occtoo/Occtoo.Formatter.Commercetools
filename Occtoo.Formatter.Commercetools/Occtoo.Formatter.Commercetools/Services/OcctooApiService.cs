using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Occtoo.Formatter.Commercetools.Services;

public interface IOcctooApiService
{
    Task<DestinationResponse<T>> GetDataBatch<T>(string url, DateTime lastRunTime, string language, string lastIdPrevBatch);
}

public class OcctooApiService : IOcctooApiService
{
    private readonly ApiClientCredentials _applicationCredentials;
    private readonly DestinationSettings _destinationSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcctooApiService> _logger;
    private DateTime? _accessTokenExpiration;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public OcctooApiService(IOptions<ApiClientCredentials> applicationCredentials,
        IOptions<DestinationSettings> destinationSettings,
        ILogger<OcctooApiService> logger,
        HttpClient httpClient)
    {
        _applicationCredentials = applicationCredentials.Value;
        _destinationSettings = destinationSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SetToken()
    {
        if (_accessTokenExpiration == null || DateTime.UtcNow >= _accessTokenExpiration)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(
                    new Dictionary<string, string>
                    {
                        {"clientId", _applicationCredentials.ClientId},
                        {"clientSecret", _applicationCredentials.ClientSecret }
                    }),
                Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_applicationCredentials.TokenAuthUrl, content);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadAsAsync<OcctooTokenModel>();
            _accessTokenExpiration = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 500);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        }
    }

    public async Task<DestinationResponse<T>> GetDataBatch<T>(string url, DateTime lastRunTime, string language, string lastIdPrevBatch = "")
    {
        try
        {
            await SetToken();

            var data = new DestinationResponse<T>(language, new List<T>());

            var response =
                await _httpClient.GetAsync(
                    $"{url}{GetQueryString(lastRunTime, language, lastIdPrevBatch)}");

            response.EnsureSuccessStatusCode();

            var stringResult = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DestinationResponse<T>>(stringResult, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred during connection to occtoo api service {error}", ex.Message);
            return new DestinationResponse<T>(language, new List<T>());
        }
    }

    private string GetQueryString(DateTime lastRunTime, string language, string lastIdPrevBatch) =>
        $"?top={_destinationSettings.BatchSize}&sortAsc=id&language={language}&periodSince={lastRunTime:o}" +
        $"{(string.IsNullOrWhiteSpace(lastIdPrevBatch) ? string.Empty : $"&after={lastIdPrevBatch}")}";
}