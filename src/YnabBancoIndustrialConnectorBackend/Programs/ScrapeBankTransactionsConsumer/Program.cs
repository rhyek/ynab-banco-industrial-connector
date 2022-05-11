using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Application.Commands;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;

var builder = Host.CreateDefaultBuilder(args)
  .ConfigureServices((hostContext, services) => {
    services.Configure<YnabControllerOptions>(
      hostContext.Configuration.GetSection("YNAB"));
    services.Configure<BancoIndustrialScraperOptions>(
      hostContext.Configuration.GetSection("BANCO_INDUSTRIAL_SCRAPER"));
    services.Configure<ApplicationOptions>(
      hostContext.Configuration.GetSection("APPLICATION"));
    services.AddApplication();
    services.AddBancoIndustrialScraper();
    services.AddYnabController();
  });
var host = builder.Build();
var serializer = new DefaultLambdaJsonSerializer();


// ReSharper disable once ConvertToLocalFunction
var handler = async (Stream stream, ILambdaContext context) => {
  BancoIndustrialScraper.Diagnostics.RunDiagnostics();
  var bancoIndustrialScraperOptions = host.Services
    .GetService<IOptions<BancoIndustrialScraperOptions>>()!.Value;
  var mediator = host.Services.GetService<IMediator>()!;
  
  var evt = serializer.Deserialize<SQSEvent>(stream);
  foreach (var record in evt.Records) {
    context.Logger.LogInformation($"message received: {record.Body}");
    var command = record.Body switch {
      "RESERVED" => typeof(UpdateBankReservedTransactionsCommand),
      "CONFIRMED" => typeof(UpdateBankConfirmedTransactionsCommand),
      _ => null
    };
    if (command != null) {
      await mediator.Send(Activator.CreateInstance(command) ??
                          throw new InvalidOperationException());
    }
    var tracePath = bancoIndustrialScraperOptions.PlaywrightTraceFile;
    if (File.Exists(tracePath)) {
      context.Logger.LogInformation(
        $"trace file: {tracePath}, exists: {File.Exists(tracePath)}");
    }
  }
};

await LambdaBootstrapBuilder.Create(handler).Build().RunAsync();
