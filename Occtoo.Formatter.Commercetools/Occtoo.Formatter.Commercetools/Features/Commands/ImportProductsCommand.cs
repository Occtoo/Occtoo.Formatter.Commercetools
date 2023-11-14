using commercetools.Sdk.ImportApi.Models.Common;
using commercetools.Sdk.ImportApi.Models.Products;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Extensions;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;

namespace Occtoo.Formatter.Commercetools.Features.Commands;

public record ImportProductsCommand(IReadOnlyList<ProductVariantDto> ProductVariants) : IRequest<CommandResult>;

public class ImportProductsCommandHandler : IRequestHandler<ImportProductsCommand, CommandResult>
{
    private readonly ICommercetoolsImportService _commercetoolsImportService;
    private readonly CommercetoolsSettings _commercetoolsSettings;
    private readonly ILogger<ImportProductsCommandHandler> _logger;

    public ImportProductsCommandHandler(ICommercetoolsImportService commercetoolsImportService,
        IOptions<CommercetoolsSettings> commercetoolsSettings,
        ILogger<ImportProductsCommandHandler> logger)
    {
        _commercetoolsImportService = commercetoolsImportService;
        _commercetoolsSettings = commercetoolsSettings.Value;
        _logger = logger;
    }

    public async Task<CommandResult> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var importProducts = CreateProductImports(request.ProductVariants);

            var importProductsBatches = importProducts.CreateBatches(_commercetoolsSettings.ImportContainerEntriesLimit);

            foreach (var (index, productImportBatch) in importProductsBatches)
            {
                var importContainer = await _commercetoolsImportService.CreateImportContainerIfNotExists(
                    GetImportContainerName(index),
                    cancellationToken);

                if (importContainer == null)
                {
                    _logger.LogError("Failure occurred when trying to retrieve import container");
                    return new CommandResult(false);
                }

                await _commercetoolsImportService.ImportProducts(importContainer, productImportBatch,
                    cancellationToken);
            }

            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred while importing products: {error}", ex.Message);
            return new CommandResult(false);
        }
    }

    private IEnumerable<IProductImport> CreateProductImports(IEnumerable<ProductVariantDto> productVariants)
    {
        var productImports = new List<ProductImport>();
        var groupedProducts = productVariants.GroupBy(p => p.ProductId);

        foreach (var group in groupedProducts)
        {
            var referenceProduct = group.First();
            var productImport = CreateBasicProductImport(referenceProduct);

            FillOutLocalizedParameters(productImport, group);
            productImports.Add(productImport);
        }

        return productImports;
    }

    private ProductImport CreateBasicProductImport(ProductVariantDto productVariant)
    {
        return new ProductImport
        {
            Key = productVariant.ProductId,
            ProductType = new ProductTypeKeyReference { Key = _commercetoolsSettings.ProductTypeName, TypeId = IReferenceType.ProductType },
            Name = new LocalizedString(),
            Slug = new LocalizedString(),
            Description = new LocalizedString(),
            MetaTitle = new LocalizedString(),
            MetaDescription = new LocalizedString(),
            MetaKeywords = new LocalizedString(),
            PriceMode = IProductPriceModeEnum.Embedded,
            Publish = productVariant.PublishProduct ?? _commercetoolsSettings.PublishProducts,
            Categories = productVariant.ProductCategories
                .Select(category => new CategoryKeyReference { Key = category, TypeId = IReferenceType.Category })
                .Cast<ICategoryKeyReference>()
                .ToList()
        };
    }

    public static void FillOutLocalizedParameters(ProductImport productImport, IEnumerable<ProductVariantDto> productVariants)
    {
        foreach (var product in productVariants)
        {
            var language = product.Language;

            productImport.Name[language] = product.ProductName;
            productImport.Slug[language] = product.ProductSlug;
            productImport.Description[language] = product.ProductDescription;

            if (!string.IsNullOrWhiteSpace(product.ProductMetaTitle))
            {
                productImport.MetaTitle[language] = product.ProductMetaTitle;
            }

            if (!string.IsNullOrWhiteSpace(product.ProductMetaDescription))
            {
                productImport.MetaDescription[language] = product.ProductMetaDescription;
            }

            if (!string.IsNullOrWhiteSpace(product.ProductMetaKeywords))
            {
                productImport.MetaKeywords[language] = product.ProductMetaKeywords;
            }
        }
    }

    private static string GetImportContainerName(int batchIndex) => $"occtoo-products-{batchIndex}";
}