// https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/

using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using MediatR;
using MessageQueueService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Application.Commands;
using YnabBancoIndustrialConnector.Infrastructure.CurrencyConverter;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;

var host = Host.CreateDefaultBuilder(args)
  .ConfigureServices((hostContext, services) => {
    services.Configure<ApplicationOptions>(
      hostContext.Configuration.GetSection("APPLICATION"));
    services.Configure<YnabControllerOptions>(
      hostContext.Configuration.GetSection("YNAB"));
    services.AddApplication();
    services.AddYnabController();
    services.AddCurrencyConverter();
    services.AddMessageQueueService();
  })
  .Build();

var mediator = host.Services.GetService<IMediator>()!;

var handler = async (DynamoDBEvent evt, ILambdaContext context) => {
  foreach (var record in evt.Records) {
    if (record.EventName == OperationType.INSERT) {
      var notificationText = record.Dynamodb.NewImage["text"].S;
      context.Logger.Log("notification text:");
      context.Logger.Log(notificationText);
      var tx = await mediator.Send(new NewMobileNotificationTransactionCommand {
        MobileNotificationText = notificationText
      });
      context.Logger.Log("parsed tx:");
      context.Logger.Log(JsonSerializer.Serialize(tx));
    }
  }
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
  .Build().RunAsync();
