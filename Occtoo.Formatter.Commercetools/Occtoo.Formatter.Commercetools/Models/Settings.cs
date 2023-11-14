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
    public int ImportContainerEntriesLimit { get; init; } = 200000;
    public string ProductTypeName { get; init; } = string.Empty;
    public bool PublishProducts { get; init; }
    public bool PublishProductVariants { get; init; }
    public Dictionary<string, AttributeType> AttributeDictionary { get; init; } = new();
    public List<string> Languages { get; init; } = new();
}

public enum AttributeType
{
    Boolean,
    Text,
    LocalizedText,
    Number,
    DateTime,
    Date,
    Time,
    //Reference -> Not Supported
    //Money -> Not Supported
    EnumList,
    LocalizedList
}