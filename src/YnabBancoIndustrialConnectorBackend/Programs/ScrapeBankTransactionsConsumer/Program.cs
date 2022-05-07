using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
var serviceProvider = builder.Build();
var serializer = new DefaultLambdaJsonSerializer();

// ReSharper disable once ConvertToLocalFunction
var handler = async (Stream stream, ILambdaContext context) => {
  BancoIndustrialScraper.Diagnostics.RunDiagnostics();
  var mediator = serviceProvider.Services.GetService<IMediator>();
  if (mediator == null) {
    throw new Exception("mediator is null");
  }
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
  }
};

await LambdaBootstrapBuilder.Create(handler).Build().RunAsync();
