using System.Collections.Immutable;

namespace Occtoo.Formatter.Commercetools.Models;

public record ApiClientCredentials
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string TokenAuthUrl { get; init; } = string.Empty;

}

public record DestinationSettings
{
    public string ProductUrl { get; init; } = string.Empty;
    public string CategoriesUrl { get; init; } = string.Empty;
    public int BatchSize { get; init; }
}

public record CommercetoolsSettings
{
    public string[] Languages { get; init; } = Array.Empty<string>();
    public string ImportContainerKey { get; init; } = string.Empty;
    public string? MasterVariantProperty { get; init; }
}