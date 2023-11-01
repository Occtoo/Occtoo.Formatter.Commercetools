using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Occtoo.Formatter.Commercetools.Functions;

public record ManualDataTransferRequest(DateTime? LastRunTime);

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
            : new ManualDataTransferRequest(default);

        var products = await _occtooApiService.FetchAllItems<ProductDto>(DataType.Product, request.LastRunTime);
        var categories = await _occtooApiService.FetchAllItems<ProductDto>(DataType.Category, request.LastRunTime);

        _lastRunTime = DateTime.UtcNow;
        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }

    [Function("PeriodicDataTransfer")]
    public async Task PeriodicDataTransfer([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        var products = await _occtooApiService.FetchAllItems<ProductDto>(DataType.Product, _lastRunTime);
        var categories = await _occtooApiService.FetchAllItems<ProductDto>(DataType.Category, _lastRunTime);

        _lastRunTime = DateTime.UtcNow;
    }
}