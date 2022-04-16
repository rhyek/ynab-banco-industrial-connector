using YnabBancoIndustrialConnector.Infrastructure.BIScraper;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;
using YnabBancoIndustrialConnector.Programs.HttpApi;
using MediatR;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Application.Commands;
using YnabBancoIndustrialConnector.Programs.HttpApi.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

if (builder.Environment.IsDevelopment()) {
  Console.WriteLine("Loading .env");
  DotNetEnv.Env.TraversePath().Load();
  // this is normally done automatically, but we need to re-run it
  // due to our loading the .env file post startup
  builder.Configuration.AddEnvironmentVariables();
}
builder.Services.ConfigureOptions(builder.Configuration);

const string corsPolicy = "MyCorsPolicy";
builder.Services.AddCors(options =>
  options.AddPolicy(corsPolicy,
    builder => builder.AllowAnyOrigin().AllowAnyHeader()));
builder.Services.AddBancoIndustrialScraper();
builder.Services.AddYnabController();
builder.Services.AddApplication();

var app = builder.Build();

app.UseCors(corsPolicy);

app.MapGet("/", () => Results.Redirect("/health-check"));
app.MapGet("/health-check", () => Results.Text("ok"));

app.MapPost("/request-read-transactions/confirmed",
  async (IMediator mediator) => Results.Ok(
    await mediator.Send(
      new UpdateBankConfirmedTransactionsCommand())));

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

var port = Environment.GetEnvironmentVariable("PORT") ?? "3700";

app.Run(url: $"http://0.0.0.0:{port}");
