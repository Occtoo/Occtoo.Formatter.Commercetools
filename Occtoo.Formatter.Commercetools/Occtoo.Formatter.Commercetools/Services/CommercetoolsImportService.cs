﻿using commercetools.Base.Client.Error;
using commercetools.Sdk.ImportApi.Models.Categories;
using commercetools.Sdk.ImportApi.Models.Common;
using commercetools.Sdk.ImportApi.Models.Importcontainers;
using commercetools.Sdk.ImportApi.Models.Importrequests;
using commercetools.Sdk.ImportApi.Models.Products;
using commercetools.Sdk.ImportApi.Models.Productvariants;
using Microsoft.Extensions.Logging;
using ImportApi = commercetools.Sdk.ImportApi.Client.ProjectApiRoot;

namespace Occtoo.Formatter.Commercetools.Services;

public interface ICommercetoolsImportService
{
    Task<IImportContainer?> CreateImportContainerIfNotExists(string containerName, CancellationToken cancellationToken);
    Task ImportCategories(IImportContainer container, IEnumerable<ICategoryImport> categoryImports, CancellationToken cancellationToken);
    Task ImportProducts(IImportContainer container, IEnumerable<IProductImport> productVariants, CancellationToken cancellationToken);
    Task ImportProductVariants(IImportContainer container, IEnumerable<IProductVariantImport> productVariants, CancellationToken cancellationToken);
}

public class CommercetoolsImportService : ICommercetoolsImportService
{
    private readonly ImportApi _importApi;
    private readonly ILogger<CommercetoolsImportService> _logger;

    public CommercetoolsImportService(ImportApi importApi,
        ILogger<CommercetoolsImportService> logger)
    {
        _importApi = importApi;
        _logger = logger;
    }

    public async Task<IImportContainer?> CreateImportContainerIfNotExists(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            return await _importApi
                .ImportContainers()
                .WithImportContainerKeyValue(containerName)
                .Get()
                .ExecuteAsync(cancellationToken);
        }
        catch (NotFoundException ex)
        {
            return await _importApi
                .ImportContainers()
                .Post(new ImportContainerDraft { Key = containerName })
                .ExecuteAsync(cancellationToken);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError("An exception occurred while trying to create import container {containerName} error: {error}, body: {body}, status code: {statusCode}",
                containerName,
                ex.Message,
                ex.Body,
                ex.StatusCode);
            return null;
        }
    }

    public async Task ImportCategories(IImportContainer container, IEnumerable<ICategoryImport> categoryImports, CancellationToken cancellationToken)
    {
        try
        {
            var importTasks = categoryImports
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index / 20)
                .Select(group => new CategoryImportRequest
                {
                    Resources = group.Select(x => x.Item).ToList(),
                    Type = IImportResourceType.Category
                })
                .Select(importRequest => _importApi
                    .Categories()
                    .ImportContainers()
                    .WithImportContainerKeyValue(container.Key)
                    .Post(importRequest)
                    .ExecuteAsync(cancellationToken))
                .ToList();

            await Task.WhenAll(importTasks);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError("An exception occurred while trying to import categories error: {error}, body: {body}, status code: {statusCode}",
                ex.Message,
                ex.Body,
                ex.StatusCode);
        }
    }

    public async Task ImportProducts(IImportContainer container, IEnumerable<IProductImport> productImports, CancellationToken cancellationToken)
    {
        try
        {
            var importTasks = productImports
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index / 20)
                .Select(group => new ProductImportRequest
                {
                    Resources = group.Select(x => x.Item).ToList(),
                    Type = IImportResourceType.Product
                })
                .Select(importRequest => _importApi
                    .Products()
                    .ImportContainers()
                    .WithImportContainerKeyValue(container.Key)
                    .Post(importRequest)
                    .ExecuteAsync(cancellationToken))
                .ToList();

            await Task.WhenAll(importTasks);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(
                "An exception occurred while trying to import products error: {error}, body: {body}, status code: {statusCode}",
                ex.Message,
                ex.Body,
                ex.StatusCode);
        }
    }

    public async Task ImportProductVariants(IImportContainer container, IEnumerable<IProductVariantImport> productVariantImports, CancellationToken cancellationToken)
    {
        try
        {
            var importTasks = productVariantImports
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index / 20)
                .Select(group => new ProductVariantImportRequest
                {
                    Resources = group.Select(x => x.Item).ToList(),
                    Type = IImportResourceType.ProductVariant
                })
                .Select(importRequest => _importApi
                    .ProductVariants()
                    .ImportContainers()
                    .WithImportContainerKeyValue(container.Key)
                    .Post(importRequest)
                    .ExecuteAsync(cancellationToken))
                .ToList();

            await Task.WhenAll(importTasks);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(
                "An exception occurred while trying to import product variants error: {error}, body: {body}, status code: {statusCode}",
                ex.Message,
                ex.Body,
                ex.StatusCode);
        }
    }
}