using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Occtoo.Formatter.Commercetools.Models;

namespace Occtoo.Formatter.Commercetools.Services;

public interface IAzureTableService
{
    Task<CommercetoolsConfigurationEntity?> GetCommercetoolsConfigurationAsync();
    Task<CommercetoolsConfigurationEntity> UpdateCommercetoolsConfigurationAsync(CommercetoolsConfigurationDto configurationDto);
}

public class AzureTableService : IAzureTableService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableService> _logger;
    private const string PartitionKey = "CommercetoolsConfiguration";
    private const string RowKey = "CommercetoolsConfigurationRow";

    public AzureTableService(TableClient tableClient,
        ILogger<AzureTableService> logger)
    {
        _tableClient = tableClient;
        _logger = logger;
    }

    public async Task<CommercetoolsConfigurationEntity?> GetCommercetoolsConfigurationAsync()
    {
        try
        {
            var table = await _tableClient.CreateIfNotExistsAsync();
            return await _tableClient.GetEntityAsync<CommercetoolsConfigurationEntity>(PartitionKey, RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Azure table service responded with error when retrieving data: {error}", ex.Message);
            return null;
        }
    }

    public async Task<CommercetoolsConfigurationEntity> UpdateCommercetoolsConfigurationAsync(CommercetoolsConfigurationDto configurationDto)
    {
        try
        {
            var updatedEntity = CommercetoolsConfigurationEntity.FromDto(configurationDto);
            await _tableClient.UpsertEntityAsync(updatedEntity);

            return updatedEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError("Azure table service responded with exception when updating: {error}", ex.Message);
            throw;
        }
    }
}