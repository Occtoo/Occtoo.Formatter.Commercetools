using Azure.Data.Tables;
using commercetools.Sdk.Api;
using commercetools.Sdk.ImportApi;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Occtoo.Formatter.Commercetools.Models;
using Occtoo.Formatter.Commercetools.Services;
using System.Reflection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((builder, sc) =>
    {
        sc.Configure<ApiClientCredentials>(builder.Configuration.GetSection(nameof(ApiClientCredentials)));
        sc.Configure<DestinationSettings>(builder.Configuration.GetSection(nameof(DestinationSettings)));
        sc.Configure<CommercetoolsSettings>(builder.Configuration.GetSection(nameof(CommercetoolsSettings)));

        var assembly = typeof(Program).GetTypeInfo().Assembly;
        sc.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        sc.AddSingleton<IValidationService, ValidationService>();
        sc.AddValidatorsFromAssemblyContaining<Program>();
        
        var azureWebJobsStorage = builder.Configuration["AzureWebJobsStorage"];
        sc.AddSingleton(_ => new TableClient(azureWebJobsStorage, "CommercetoolsConfiguration"));
        sc.AddSingleton<IAzureTableService, AzureTableService>();

        sc.AddHttpClient<IOcctooApiService, OcctooApiService>();
        sc.UseCommercetoolsImportApi(builder.Configuration, "CommercetoolsImportClient");
        sc.UseCommercetoolsApi(builder.Configuration, "CommercetoolsApiClient");

        sc.AddSingleton<ICommercetoolsImportService, CommercetoolsImportService>();
    })
    .Build();

host.Run();
