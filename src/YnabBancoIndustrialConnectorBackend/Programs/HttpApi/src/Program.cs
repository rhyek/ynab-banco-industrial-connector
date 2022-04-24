using YnabBancoIndustrialConnector.Infrastructure.BIScraper;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;
using YnabBancoIndustrialConnector.Programs.HttpApi;
using MediatR;
using MessageQueueService;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Application.Commands;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;
using YnabBancoIndustrialConnector.Infrastructure.CurrencyConverter;
using YnabBancoIndustrialConnector.Interfaces;
using YnabBancoIndustrialConnector.Programs.HttpApi.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAllOptions(builder.Configuration,
  builder.Environment);

// Add services to the container.

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

const string corsPolicy = "MyCorsPolicy";
builder.Services.AddCors(options =>
  options.AddPolicy(corsPolicy,
    builder => builder.AllowAnyOrigin().AllowAnyHeader()));
builder.Services.AddBancoIndustrialScraper();
builder.Services.AddYnabController();
builder.Services.AddMessageQueueService();
builder.Services.AddApplication();
builder.Services.AddCurrencyConverter();

var app = builder.Build();

app.UseCors(corsPolicy);

app.MapGet("/", () => Results.Redirect("/status"));
app.MapGet("/status",
  (IOptions<BancoIndustrialScraperOptions> biScraperOptions) => {
    app.Logger.LogInformation("Status requested");
    return Results.Json(new {
      health = "ok!",
      version = "1.0",
      biScraperOptions = new {
        auth = new {
          username = biScraperOptions.Value.Auth?.Username
        }
      }
    });
  });

app.MapPost("/mobile-app-notifications/register-new",
  async (IMediator mediator, MobileNotificationDto payload) => {
    app.Logger.LogInformation(
      "Mobile notification of transaction received: {Message}",
      payload.Text);
    return Results.Ok(await mediator.Send(
      new NewMobileNotificationTransactionCommand {
        MobileNotificationText = payload.Text
      }));
  });

app.MapPost("/scrape-bank-transactions/reserved",
  async (IMediator mediator, IMessageQueueService messageQueue) => {
    if (app.Environment.IsDevelopment()) {
      await mediator.Send(
        new UpdateBankReservedTransactionsCommand());
      return Results.Text("ok");
    }
    else {
      // queue sqs message
      await messageQueue.SendScrapeReservedTransactionsMessage();
      return Results.Text("message queued");
    }
  });

app.MapPost("/scrape-bank-transactions/confirmed",
  async (IMediator mediator, IMessageQueueService messageQueue) => {
    if (app.Environment.IsDevelopment()) {
      await mediator.Send(
        new UpdateBankConfirmedTransactionsCommand());
      return Results.Text("ok");
    }
    else {
      // queue sqs message
      await messageQueue.SendScrapeConfirmedTransactionsMessage();
      return Results.Text("message queued");
    }
  });

if (app.Environment.IsDevelopment()) {
  // handled by .NET Kestrel web server
  var port = Environment.GetEnvironmentVariable("PORT") ?? "3700";
  app.Run(url: $"http://0.0.0.0:{port}");
}
else {
  // handled by Amazon.Lambda.AspNetCoreServer
  app.Run();
}
