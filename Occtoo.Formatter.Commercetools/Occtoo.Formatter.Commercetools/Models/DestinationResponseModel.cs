using System.Collections.Immutable;

namespace Occtoo.Formatter.Commercetools.Models;

public record DestinationResponseModel<T>
{
    public string Language { get; init; }

    public ImmutableList<T> Results { get; init; } = ImmutableList<T>.Empty;
}

/// <summary>
/// Parameter inside of DestinationRootDto should correspond of key that Destination will be ordered by in sortAsc query parameter
/// </summary>
/// <param name="Id">In my case baseQueryString has got sortAsc=id</param>
public abstract record DestinationRootDto(string Id);

/// <summary>
/// This should adhere to product endpoint on your destination
/// </summary>
public record ProductDto(string Id,
    string Thumbnail,
    string Urls,
    string Weight,
    string Site,
    string Name,
    string Category,
    string Concept,
    string WashInstructions,
    string DescriptionLocalized,
    int StockLevel,
    string Wash2,
    string[] ProductColor,
    IEnumerable<LinesDto> Lines) : DestinationRootDto(Id);

public record LinesDto(string StoreNumber, string CustomerNumber, string PostingDate);

public record CategoryDto(string Id) : DestinationRootDto(Id); 