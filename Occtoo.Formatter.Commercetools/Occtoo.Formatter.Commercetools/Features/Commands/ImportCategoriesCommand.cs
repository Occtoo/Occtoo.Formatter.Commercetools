using commercetools.Sdk.ImportApi.Models.Categories;
using commercetools.Sdk.ImportApi.Models.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;

namespace Occtoo.Formatter.Commercetools.Features.Commands;

public record ImportCategoriesCommand(IReadOnlyList<CategoryDto> Categories) : IRequest<CommandResult>;

public record ImportCategoriesCommandHandler : IRequestHandler<ImportCategoriesCommand, CommandResult>
{
    private readonly ICommercetoolsImportService _commercetoolsImportService;
    private readonly CommercetoolsSettings _commercetoolsSettings;
    private readonly ILogger<ImportCategoriesCommandHandler> _logger;

    public ImportCategoriesCommandHandler(ICommercetoolsImportService commercetoolsImportService,
        IOptions<CommercetoolsSettings> commercetoolsSettings,
        ILogger<ImportCategoriesCommandHandler> logger)
    {
        _commercetoolsImportService = commercetoolsImportService;
        _commercetoolsSettings = commercetoolsSettings.Value;
        _logger = logger;
    }

    public async Task<CommandResult> Handle(ImportCategoriesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var categoryImports = CreateCategoryImports(request.Categories);
            var importCategoriesBatches = categoryImports
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index / _commercetoolsSettings.ImportContainerEntriesLimit)
                .Select((group, index) => (Index: index, Batch: group.Select(x => x.Item).ToList()))
                .ToList();

            foreach (var (index, categoryImportBatch) in importCategoriesBatches)
            {
                var importContainer = await _commercetoolsImportService.CreateImportContainerIfNotExists(
                    GetImportContainerName(index),
                    cancellationToken);

                if (importContainer == null)
                {
                    _logger.LogError("Failure occurred when trying to retrieve import container");
                    return new CommandResult(false);
                }

                await _commercetoolsImportService.ImportCategories(importContainer, categoryImportBatch,
                    cancellationToken);
            }

            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception occurred while importing categories: {error}", ex.Message);
            return new CommandResult(false);
        }
    }

    private static IEnumerable<ICategoryImport> CreateCategoryImports(IEnumerable<CategoryDto> categories)
    {
        return categories
            .GroupBy(category => category.Id)
            .Select(group =>
            {
                var categoryImport = CreateBasicCategoryImport(group.First());
                return FillOutLocalizedData(group, categoryImport);
            });
    }

    private static CategoryImport CreateBasicCategoryImport(CategoryDto category) =>
        new ()
        {
            Key = category.Id,
            Name = new LocalizedString(),
            Slug = new LocalizedString(),
            Description = new LocalizedString(),
            MetaTitle = new LocalizedString(),
            MetaDescription = new LocalizedString(),
            MetaKeywords = new LocalizedString(),
            Parent = string.IsNullOrWhiteSpace(category.Parent) ? null : new CategoryKeyReference { Key = category.Parent, TypeId = IReferenceType.Category},
            OrderHint = category.OrderHint,
            ExternalId = category.ExternalId
        };

    private static CategoryImport FillOutLocalizedData(IEnumerable<CategoryDto> categories, CategoryImport categoryImport)
    {
        foreach (var category in categories)
        {
            categoryImport.Name[category.Language] = category.Name;
            categoryImport.Slug[category.Language] = category.Slug;

            if (!string.IsNullOrWhiteSpace(category.Description))
            {
                categoryImport.Description[category.Language] = category.Description;
            }

            if (!string.IsNullOrWhiteSpace(category.MetaTitle))
            {
                categoryImport.MetaTitle[category.Language] = category.MetaTitle;
            }

            if (!string.IsNullOrWhiteSpace(category.MetaDescription))
            {
                categoryImport.MetaDescription[category.Language] = category.MetaDescription;
            }

            if (!string.IsNullOrWhiteSpace(category.MetaKeywords))
            {
                categoryImport.MetaKeywords[category.Language] = category.MetaKeywords;
            }
        }

        return categoryImport;
    }

    private static string GetImportContainerName(int batchIndex) => $"occtoo-categories-{batchIndex}";
}