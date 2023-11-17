using commercetools.Base.Models;
using commercetools.Sdk.ImportApi.Models.Common;
using commercetools.Sdk.ImportApi.Models.Productvariants;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;

namespace Occtoo.Formatter.Commercetools.Features.Commands;

public record ImportProductVariantsCommand(IReadOnlyList<ProductVariantDto> ProductVariants) : IRequest<CommandResult>;

public record ImportProductVariantsCommandHandler : IRequestHandler<ImportProductVariantsCommand, CommandResult>
{
    private readonly ICommercetoolsImportService _commercetoolsImportService;
    private readonly CommercetoolsSettings _commercetoolsSettings;
    private readonly ILogger<ImportProductVariantsCommandHandler> _logger;
    public ImportProductVariantsCommandHandler(ICommercetoolsImportService commercetoolsImportService,
        IOptions<CommercetoolsSettings> commercetoolsSettings,
        ILogger<ImportProductVariantsCommandHandler> logger)
    {
        _commercetoolsImportService = commercetoolsImportService;
        _commercetoolsSettings = commercetoolsSettings.Value;
        _logger = logger;
    }

    public async Task<CommandResult> Handle(ImportProductVariantsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productVariantImports = CreateProductVariantImports(request.ProductVariants);
            var importProductVariantBatches = productVariantImports
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index / _commercetoolsSettings.ImportContainerEntriesLimit)
                .Select((group, index) => (Index: index, Batch: group.Select(x => x.Item).ToList()))
                .ToList();

            foreach (var (index, importProductVariants) in importProductVariantBatches)
            {
                var importContainer = await _commercetoolsImportService.CreateImportContainerIfNotExists(
                    GetImportContainerName(index),
                    cancellationToken);

                if (importContainer == null)
                {
                    _logger.LogError("Failure occurred when trying to retrieve import container");
                    return new CommandResult(false);
                }

                await _commercetoolsImportService.ImportProductVariants(importContainer, importProductVariants,
                    cancellationToken);
            }

            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred while importing product variants: {error}", ex.Message);
            return new CommandResult(false);
        }
    }

    private IEnumerable<IProductVariantImport> CreateProductVariantImports(IEnumerable<ProductVariantDto> productVariants)
    {
        return productVariants
            .GroupBy(v => v.Id)
            .Select(group => CreateImportFromVariant(group.First(), group))
            .ToList();
    }

    private IProductVariantImport CreateImportFromVariant(ProductVariantDto representativeVariant, IEnumerable<ProductVariantDto> group)
    {
        var attributes = ParseAttributes(group, representativeVariant);
        var images = CreateImages(representativeVariant.Images);

        return new ProductVariantImport
        {
            Key = representativeVariant.Id,
            Sku = representativeVariant.Sku,
            Product = new ProductKeyReference { Key = representativeVariant.ProductId, TypeId = IReferenceType.Product },
            IsMasterVariant = representativeVariant.IsMasterVariant,
            Publish = representativeVariant.PublishVariant ?? _commercetoolsSettings.PublishProductVariants,
            Attributes = attributes,
            Images = images
        };
    }

    private static List<IImage> CreateImages(IEnumerable<ImageDto>? imageDtos)
    {
        return imageDtos?.Select(image => new Image
        {
            Dimensions = new AssetDimensions { H = image.Height, W = image.Width },
            Url = image.Url,
            Label = image.Label
        }).Cast<IImage>().ToList() ?? new List<IImage>();
    }

    private List<IAttribute?> ParseAttributes(IEnumerable<ProductVariantDto> productVariants, ProductVariantDto representativeVariant)
    {
        return _commercetoolsSettings.AttributeDictionary
            .Select(kv => ParseAttribute(productVariants, representativeVariant, kv.Key, kv.Value))
            .Where(attribute => attribute != null)
            .ToList();
    }

    private static IAttribute? ParseAttribute(IEnumerable<ProductVariantDto> productVariants, ProductVariantDto representativeVariant, string attributeName, AttributeType attributeType)
    {
        if (attributeType == AttributeType.LocalizedText)
        {
            return ParseLocalizedAttribute(productVariants, attributeName);
        }

        if (representativeVariant.AttributesToValues.TryGetValue(attributeName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return attributeType switch
            {
                AttributeType.Boolean => new BooleanAttribute { Name = attributeName, Value = bool.Parse(value) },
                AttributeType.DateTime => new DateTimeAttribute { Name = attributeName, Value = DateTime.Parse(value) },
                AttributeType.Time => new TimeAttribute { Name = attributeName, Value = DateTime.Parse(value).TimeOfDay },
                AttributeType.Date => new DateAttribute { Name = attributeName, Value = new Date(DateTime.Parse(value).Date) },
                AttributeType.EnumList => new EnumAttribute { Name = attributeName, Value = value },
                AttributeType.LocalizedList => new LocalizableEnumAttribute { Name = attributeName, Value = value },
                AttributeType.Number => new NumberAttribute { Name = attributeName, Value = decimal.Parse(value) },
                AttributeType.Text => new TextAttribute { Name = attributeName, Value = value },
                _ => throw new InvalidOperationException($"Unknown attribute type: {attributeType}")
            };
        }

        return null;
    }


    private static IAttribute ParseLocalizedAttribute(IEnumerable<ProductVariantDto> productVariants, string attributeName)
    {
        var localizedString = new LocalizedString();
    
        foreach (var variant in productVariants)
        {
            if (variant.AttributesToValues.TryGetValue(attributeName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                localizedString[variant.Language] = value;
            }
        }

        return new LocalizableTextAttribute { Name = attributeName, Value = localizedString };
    }

    private static string GetImportContainerName(int batchIndex) => $"occtoo-product-variants-{batchIndex}";
}