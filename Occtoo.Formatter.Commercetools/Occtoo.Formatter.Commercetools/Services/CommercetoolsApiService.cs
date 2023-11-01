using ImportApi = commercetools.Sdk.ImportApi.Client.ProjectApiRoot;

namespace Occtoo.Formatter.Commercetools.Services;

public interface ICommercetoolsImportService
{
    void DoStuff();
}

public class CommercetoolsImportService : ICommercetoolsImportService
{
    private readonly ImportApi _importApi;

    public CommercetoolsImportService(ImportApi importApi)
    {
        _importApi = importApi;
    }

    public void DoStuff()
    {
        var test = _importApi.ClientName;
    }

    // Set languages (all languages that exist within a destination)
    // Prodcut type currently will be set to be "occtoo" only. Has to be present (created manually)

    // Import Variants
    // Import Categories
    // Import Media

    ///
    /// Import Products
    ///
    /// (occtoo) Id = Key (commercetools side)
    /// Product type = Occtoo
    ///
    /// Variants
    ///
    /// (occtoo) Id = Key (commercetools)
    /// If no "masterVariant" property is specified on a product then first should be set as master
    ///
    /// Attributes
    ///
    /// Attributes are defined on the product type (occtoo). Attribute Ids need to match fields in the destination.
    /// The configuration file needs to contain different attributes with occtoo data types
    /// Mapping:
    /// - Yes/No (boolean) - Boolean
    /// - Text - Text
    /// - Localized Text - Localized text
    /// - Number - Integer?
    /// - DateTime - Timestamp
    /// - Reference - not supported
    /// - Money - not supported
    /// - List (Enum) - List
    /// - Localized List (Enum) - Localized List
    ///
    /// Images
    /// Can be uploaded or reference URL can be used (will use reference URL)
}