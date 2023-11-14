using FluentValidation;

namespace Occtoo.Formatter.Commercetools.Models;

public record ProductVariantDto(string Id,
    string? Sku,
    string ProductId,
    string ProductName,
    string? ProductType,
    string ProductSlug,
    string[] ProductCategories,
    string? ProductDescription,
    string? ProductMetaDescription,
    string? ProductMetaTitle,
    string? ProductMetaKeywords,
    string Language,
    bool IsMasterVariant,
    bool? PublishVariant,
    bool? PublishProduct,
    List<ImageDto>? Images,
    Dictionary<string, string> AttributesToValues);

public record ImageDto(string Url, int Width, int Height, string? Label);


public class ProductVariantDtoValidator : AbstractValidator<ProductVariantDto>
{
    public ProductVariantDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductName).NotEmpty();
        RuleFor(x => x.ProductSlug).NotEmpty();
        RuleFor(x => x.ProductCategories).NotNull()
            .Must(categories => categories.Length > 0).WithMessage("At least one category is required.");
        RuleFor(x => x.Language).NotEmpty();

        When(x => x.Images != null, () =>
        {
            RuleForEach(x => x.Images)
                .ChildRules(image =>
                {
                    image.RuleFor(img => img.Url).NotEmpty().Must(BeAValidUrl).WithMessage("Image URL must be a valid URL.");
                    image.RuleFor(img => img.Width).GreaterThan(0);
                    image.RuleFor(img => img.Height).GreaterThan(0);
                });
        });

    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}