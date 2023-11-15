using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Occtoo.Formatter.Commercetools.Features.Commands;
using Occtoo.Formatter.Commercetools.Features.Queries;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;

namespace Occtoo.Formatter.Commercetools.Functions;

public class PeriodicDataTransfer
{
    private readonly IMediator _mediator;
    private readonly ILogger<PeriodicDataTransfer> _logger;
    private readonly IAzureTableService _azureTableService;

    private const string OnceEveryHour = "0 0 */1 * * *";

    public PeriodicDataTransfer(IMediator mediator,
        IAzureTableService azureTableService,
        ILogger<PeriodicDataTransfer> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _azureTableService = azureTableService;
    }

    [Function(nameof(PeriodicDataTransferFunction))]
    public async Task PeriodicDataTransferFunction([TimerTrigger(OnceEveryHour)] TimerInfo myTimer, CancellationToken cancellationToken)
    {
        var commercetoolsConfigurationEntity = await _azureTableService.GetCommercetoolsConfigurationAsync();
        var configuration = commercetoolsConfigurationEntity == null
            ? new CommercetoolsConfigurationDto(default)
            : new CommercetoolsConfigurationDto(commercetoolsConfigurationEntity.LastRunTime);

        var categories = await _mediator.Send(new GetCategoriesQuery(configuration.LastRunTime), cancellationToken);
        if (categories.Any())
        {
            var importCategoriesResult = await _mediator.Send(new ImportCategoriesCommand(categories), cancellationToken);
            if (!importCategoriesResult.IsSuccess)
            {
                _logger.LogError("Categories were not imported successfully");
                return;
            }
        }

        var productVariants = await _mediator.Send(new GetProductVariantsQuery(configuration.LastRunTime), cancellationToken);

        if (productVariants.Any())
        {
            var importProductsResult = await _mediator.Send(new ImportProductsCommand(productVariants), cancellationToken);
            if (!importProductsResult.IsSuccess)
            {
                _logger.LogError("Products were not imported successfully");
                return;
            }

            var importProductVariantsResult = await _mediator.Send(new ImportProductVariantsCommand(productVariants), cancellationToken);
            if (!importProductVariantsResult.IsSuccess)
            {
                _logger.LogError("ProductVariants were not imported successfully");
                return;
            }

        }

        await _azureTableService.UpdateCommercetoolsConfigurationAsync(configuration with { LastRunTime = DateTime.UtcNow });
    }
}