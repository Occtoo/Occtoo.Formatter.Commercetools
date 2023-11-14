using Azure;
using Azure.Data.Tables;

namespace Occtoo.Formatter.Commercetools.Models;

public class CommercetoolsConfigurationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "CommercetoolsConfiguration";
    public string RowKey { get; set; } = "CommercetoolsConfigurationRow";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public DateTime LastRunTime { get; set; }

    public static CommercetoolsConfigurationEntity FromDto(CommercetoolsConfigurationDto dto)
    {
        return new CommercetoolsConfigurationEntity()
        {
            LastRunTime = dto.LastRunTime,
        };
    }
}

public record CommercetoolsConfigurationDto(DateTime LastRunTime);