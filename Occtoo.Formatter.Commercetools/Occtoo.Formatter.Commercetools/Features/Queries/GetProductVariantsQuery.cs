using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Extensions;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;
using System.Collections.Immutable;
using System.Text.Json;

namespace Occtoo.Formatter.Commercetools.Features.Queries;

public record GetProductVariantsQuery(DateTime LastRunTime) : IRequest<IReadOnlyList<ProductVariantDto>>;

public class GetProductVariantsQueryHandler : IRequestHandler<GetProductVariantsQuery, IReadOnlyList<ProductVariantDto>>
{
    private readonly IOcctooApiService _occtooApiService;
    private readonly IValidationService _validationService;
    private readonly CommercetoolsSettings _commercetoolsSettings;
    private readonly DestinationSettings _destinationSettings;
    private readonly ILogger<GetProductVariantsQueryHandler> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public GetProductVariantsQueryHandler(IOcctooApiService occtooApiService,
        IValidationService validationService,
        IOptions<CommercetoolsSettings> commercetoolsSettings,
        IOptions<DestinationSettings> destinationSettings,
        ILogger<GetProductVariantsQueryHandler> logger)
    {
        _occtooApiService = occtooApiService;
        _validationService = validationService;
        _commercetoolsSettings = commercetoolsSettings.Value;
        _destinationSettings = destinationSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductVariantDto>> Handle(GetProductVariantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allProductVariants = await FetchAllProductVariants(request.LastRunTime);

            if (!_validationService.ValidateAll(allProductVariants))
            {
                _logger.LogError("There was problem with retrieved products");
                return ImmutableList<ProductVariantDto>.Empty;
            }

            return allProductVariants
                .GroupBy(v => v.ProductId)
                .SelectMany(EnsureMasterVariant)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred while retrieving data for product variants: {error}", ex.Message);
            return ImmutableList<ProductVariantDto>.Empty;
        }
    }

    private async Task<List<ProductVariantDto>> FetchAllProductVariants(DateTime lastRunTime)
    {
        var allProducts = new List<ProductVariantDto>();
        foreach (var language in _commercetoolsSettings.Languages)
        {
            var productsForLanguage = await FetchProductVariantsForLanguage(language, lastRunTime);
            allProducts.AddRange(productsForLanguage);
        }

        return allProducts;
    }

    private async Task<IEnumerable<ProductVariantDto>> FetchProductVariantsForLanguage(string language, DateTime lastRunTime)
    {
        var allProductVariants = new List<ProductVariantDto>();
        var nextId = string.Empty;

        while (true)
        {
            var productsBatch = await _occtooApiService.GetDataBatch<JsonElement>(
                _destinationSettings.ProductUrl,
                lastRunTime,
                language,
                nextId);

            if (!productsBatch.Results.Any())
                break;

            // Additional deserialization is needed because of custom, configurable attributes inside of ProductVariant
            var productVariants = productsBatch.Results
                .Select(resultElement => DeserializeProductVariant(resultElement, language))
                .ToList();

            allProductVariants.AddRange(productVariants);
            nextId = productVariants[^1].Id;
        }

        return allProductVariants;
    }

    private ProductVariantDto DeserializeProductVariant(JsonElement element, string language)
    {
        var productVariant = JsonSerializer.Deserialize<ProductVariantDto>(element.GetRawText(), _jsonSerializerOptions) 
                             ?? throw new InvalidOperationException("Deserialization of ProductVariantDto failed.");

        var attributesToValues = new Dictionary<string, string>();
        foreach (var attributeName in _commercetoolsSettings.AttributeDictionary.Keys)
        {
            if (element.TryGetProperty(attributeName.LowerCaseFirstLetter(), out var attributeValue))
            {
                attributesToValues.Add(attributeName, attributeValue.ToString());
            }
        }

        return productVariant with
        {
            Language = language,
            AttributesToValues = attributesToValues
        };
    }

    private static IEnumerable<ProductVariantDto> EnsureMasterVariant(IEnumerable<ProductVariantDto> variantsForGivenProductId)
    {
        var variantList = variantsForGivenProductId.ToList();
        if (variantList.Any(v => v.IsMasterVariant))
        {
            return variantList;
        }

        variantList[0] = variantList[0] with { IsMasterVariant = true };

        return variantList;
    }
}