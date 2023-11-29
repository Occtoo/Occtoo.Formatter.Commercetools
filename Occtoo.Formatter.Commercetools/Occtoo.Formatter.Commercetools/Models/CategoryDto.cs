using FluentValidation;
using System.Globalization;

namespace Occtoo.Formatter.Commercetools.Models;

public record CategoryDto(string Id,
    string Name,
    string Slug,
    string? Description,
    string? Parent,
    string? ExternalId,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string Language,
    string OrderHint = "0");

public class CategoryDtoValidator : AbstractValidator<CategoryDto>
{
    public CategoryDtoValidator()
    {
        RuleFor(cat => cat.Id).NotEmpty();
        RuleFor(cat => cat.Name).NotEmpty();
        RuleFor(cat => cat.Slug).NotEmpty();
        RuleFor(cat => cat.Language).NotEmpty();
        RuleFor(cat => decimal.Parse(cat.OrderHint, CultureInfo.InvariantCulture)).InclusiveBetween(0m, 1m);
    }
}