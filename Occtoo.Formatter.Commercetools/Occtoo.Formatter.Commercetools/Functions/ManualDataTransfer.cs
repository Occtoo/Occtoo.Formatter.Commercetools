using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Occtoo.Formatter.Commercetools.Features.Commands;
using Occtoo.Formatter.Commercetools.Features.Queries;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;
using System.Net;
using System.Text.Json;

namespace Occtoo.Formatter.Commercetools.Functions;

public record ManualDataTransferRequest(DateTime LastRunTime);

public class ManualDataTransfer
{
    private readonly IMediator _mediator;
    private readonly ILogger<ManualDataTransfer> _logger;
    private readonly IAzureTableService _azureTableService;

    public ManualDataTransfer(IMediator mediator,
        IAzureTableService azureTableService,
        ILogger<ManualDataTransfer> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _azureTableService = azureTableService;
    }

    [Function(nameof(ManualDataTransfer))]
    public async Task<HttpResponseData> ManualDataTransferFunction([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var lastRunDate = await GetLastRunTime(req, cancellationToken);

            var categories = await _mediator.Send(new GetCategoriesQuery(lastRunDate), cancellationToken);
            if (categories.Any())
            {
                var importCategoriesResult =
                    await _mediator.Send(new ImportCategoriesCommand(categories), cancellationToken);
                if (!importCategoriesResult.IsSuccess)
                {
                    _logger.LogError("Categories were not imported successfully");
                    return await CreateResponse(req, HttpStatusCode.BadRequest, "Categories were not imported successfully");
                }
            }

            var productVariants =
                await _mediator.Send(new GetProductVariantsQuery(lastRunDate), cancellationToken);

            if (productVariants.Any())
            {
                var importProductsResult =
                    await _mediator.Send(new ImportProductsCommand(productVariants), cancellationToken);
                if (!importProductsResult.IsSuccess)
                {
                    _logger.LogError("Products were not imported successfully");
                    return await CreateResponse(req, HttpStatusCode.BadRequest, "Products were not imported successfully");
                }

                var importProductVariantsResult =
                    await _mediator.Send(new ImportProductVariantsCommand(productVariants), cancellationToken);
                if (!importProductVariantsResult.IsSuccess)
                {
                    _logger.LogError("ProductVariants were not imported successfully");
                    return await CreateResponse(req, HttpStatusCode.BadRequest, "ProductVariants were not imported successfully");
                }

            }

            await _azureTableService.UpdateCommercetoolsConfigurationAsync(new CommercetoolsConfigurationDto(DateTime.UtcNow));

            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred while trying to perform manual data transfer: {error}", ex.Message);
            return await CreateResponse(req, HttpStatusCode.InternalServerError, $"An exception occurred while trying to perform manual data transfer: {ex.Message}");
        }
    }

    private static async Task<HttpResponseData> CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteStringAsync(message);
        return response;
    }

    private async Task<DateTime> GetLastRunTime(HttpRequestData req, CancellationToken cancellationToken)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
        if (!string.IsNullOrEmpty(requestBody))
        {
            var request = JsonSerializer.Deserialize<ManualDataTransferRequest>(requestBody);
            return request!.LastRunTime;
        }

        _logger.LogWarning("No last run date provided inside of request body, retrieving time from configuration");
        var commercetoolsConfigurationEntity = await _azureTableService.GetCommercetoolsConfigurationAsync();
        return commercetoolsConfigurationEntity?.LastRunTime ?? default;
    }
}