using commercetools.Sdk.ImportApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((builder, sc) =>
    {
        sc.Configure<ApiClientCredentials>(builder.Configuration.GetSection(nameof(ApiClientCredentials)));
        sc.Configure<DestinationSettings>(builder.Configuration.GetSection(nameof(DestinationSettings)));

        sc.AddHttpClient<IOcctooApiService, OcctooApiService>();
        sc.UseCommercetoolsImportApi(builder.Configuration, "CommercetoolsImportClient");

        sc.AddSingleton<ICommercetoolsImportService, CommercetoolsImportService>();
    })
    .Build();

host.Run();
