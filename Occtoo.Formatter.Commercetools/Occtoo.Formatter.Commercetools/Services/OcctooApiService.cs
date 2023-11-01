using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Occtoo.Formatter.Commercetools.Services;

public enum DataType
{
    Product,
    Category
}

public interface IOcctooApiService
{
    Task<IReadOnlyList<T>> FetchAllItems<T>(DataType dataType, DateTime? lastRunTime, string language = "en") where T : DestinationRootDto;
}

public class OcctooApiService : IOcctooApiService
{
    private readonly ApiClientCredentials _applicationCredentials;
    private readonly DestinationSettings _destinationSettings;
    private readonly HttpClient _httpClient;
    private DateTime? _accessTokenExpiration;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new () { PropertyNameCaseInsensitive = true };

    public OcctooApiService(IOptions<ApiClientCredentials> applicationCredentials,
        IOptions<DestinationSettings> destinationSettings, 
        HttpClient httpClient)
    {
        _applicationCredentials = applicationCredentials.Value;
        _destinationSettings = destinationSettings.Value;
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

    public async Task<IReadOnlyList<T>> FetchAllItems<T>(DataType dataType, DateTime? lastRunTime, string language = "en") where T : DestinationRootDto
    {
        await SetToken();

        var allItems = new List<T>();
        var baseQueryString = $"?top={_destinationSettings.BatchSize}&sortAsc=id&language={language}&periodSince={lastRunTime ?? default:o}";
        var lastIdPrevBatch = string.Empty;

        while (true)
        {
            var queryStringBuilder = new StringBuilder(baseQueryString);

            if (!string.IsNullOrWhiteSpace(lastIdPrevBatch))
            {
                queryStringBuilder.Append($"&after={lastIdPrevBatch}");
            }

            var response = await _httpClient.GetFromJsonAsync<DestinationResponseModel<T>>($"{GetBaseUrl(dataType)}{queryStringBuilder}", _jsonSerializerOptions);

            if (response?.Results != null && response.Results.Any())
            {
                allItems.AddRange(response.Results);
                lastIdPrevBatch = response.Results[^1].Id;
            }
            else
            {
                break;
            }
        }

        return allItems.AsReadOnly();
    }

    private string GetBaseUrl(DataType dataType)
        => dataType switch
        {
            DataType.Product => _destinationSettings.ProductUrl,
            DataType.Category => _destinationSettings.CategoriesUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
        };
}