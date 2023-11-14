using System.Collections.Immutable;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;

namespace Occtoo.Formatter.Commercetools.Features.Queries;

public record GetCategoriesQuery(DateTime LastRunTime) : IRequest<IReadOnlyList<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IOcctooApiService _occtooApiService;
    private readonly IValidationService _validationService;
    private readonly CommercetoolsSettings _commercetoolsSettings;
    private readonly DestinationSettings _destinationSettings;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(IOcctooApiService occtooApiService,
        IValidationService validationService,
        IOptions<CommercetoolsSettings> commercetoolsSettings,
        IOptions<DestinationSettings> destinationSettings,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _occtooApiService = occtooApiService;
        _validationService = validationService;
        _commercetoolsSettings = commercetoolsSettings.Value;
        _destinationSettings = destinationSettings.Value;
        _logger = logger;
    }


    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allCategories = await FetchAllCategories(request.LastRunTime);

            if (!_validationService.ValidateAll(allCategories))
            {
                _logger.LogError("There was problem with retrieved categories");
                return ImmutableList<CategoryDto>.Empty;
            }

            return allCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred while retrieving data for categories: {error}", ex.Message);
            return ImmutableList<CategoryDto>.Empty;
        }
    }

    private async Task<List<CategoryDto>> FetchAllCategories(DateTime lastRunTime)
    {
        var allCategories = new List<CategoryDto>();
        foreach (var language in _commercetoolsSettings.Languages)
        {
            var categoriesForLanguage = await FetchCategoriesForLanguage(language, lastRunTime);
            allCategories.AddRange(categoriesForLanguage);
        }

        return allCategories;
    }

    private async Task<List<CategoryDto>> FetchCategoriesForLanguage(string language, DateTime lastRunTime)
    {
        var allCategoriesForLanguage = new List<CategoryDto>();
        var nextId = string.Empty;

        while (true)
        {
            var categoriesBatch = await _occtooApiService.GetDataBatch<CategoryDto>(
                _destinationSettings.CategoriesUrl,
                lastRunTime,
                language,
                nextId);

            if (!categoriesBatch.Results.Any())
                break;

            allCategoriesForLanguage.AddRange(categoriesBatch.Results);
            nextId = categoriesBatch.Results.Last().Id;
        }

        return allCategoriesForLanguage.Select(category => category with { Language = language }).ToList();
    }
}
