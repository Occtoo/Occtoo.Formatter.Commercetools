namespace Occtoo.Formatter.Commercetools.Models;

public record DestinationResponse<T>(string Language, List<T> Results);