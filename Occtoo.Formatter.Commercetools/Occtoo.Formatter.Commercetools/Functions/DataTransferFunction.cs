using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Occtoo.Formatter.Commercetools.Functions;

public record ManualDataTransferRequest(DateTime? LastRunTime, string? Language);

public class DataTransferFunction
{
    private readonly IOcctooApiService _occtooApiService;
    private readonly CommercetoolsSettings _commercetoolsSettings;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static DateTime _lastRunTime;

    public DataTransferFunction(IOcctooApiService occtooApiService, IOptions<CommercetoolsSettings> commercetoolsSettings)
    {
        _occtooApiService = occtooApiService;
        _commercetoolsSettings = commercetoolsSettings.Value;
    }

    [Function("ManualDataTransfer")]
    public async Task<HttpResponseData> ManualDataTransfer([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transfer")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = string.IsNullOrWhiteSpace(requestBody)
            ? JsonSerializer.Deserialize<ManualDataTransferRequest>(requestBody, _jsonSerializerOptions)!
            : new ManualDataTransferRequest(null, null);

        var products = await _occtooApiService.FetchAllItems<ProductDto>(DataType.Product, request.LastRunTime ?? default, request.Language ?? "en");
        var categories = await _occtooApiService.FetchAllItems<CategoryDto>(DataType.Category, request.LastRunTime ?? default, request.Language ?? "en");

        _lastRunTime = DateTime.UtcNow;
        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }

    [Function("PeriodicDataTransfer")]
    public async Task PeriodicDataTransfer([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        var allProducts = new List<ProductDto>();
        var allCategories = new List<CategoryDto>();
        foreach (var language in _commercetoolsSettings.Languages)
        {
            allProducts.AddRange(await _occtooApiService.FetchAllItems<ProductDto>(DataType.Product, _lastRunTime, language));
            allCategories.AddRange(await _occtooApiService.FetchAllItems<CategoryDto>(DataType.Category, _lastRunTime, language));
        }

        _lastRunTime = DateTime.UtcNow;
    }
}