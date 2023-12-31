﻿using Azure;
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
        _tableClient.CreateIfNotExists();
        _logger = logger;
    }

    public async Task<CommercetoolsConfigurationEntity?> GetCommercetoolsConfigurationAsync()
    {
        try
        {
            return await _tableClient.GetEntityAsync<CommercetoolsConfigurationEntity>(PartitionKey, RowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Commercetools configuration entity was not found");
            return null;
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