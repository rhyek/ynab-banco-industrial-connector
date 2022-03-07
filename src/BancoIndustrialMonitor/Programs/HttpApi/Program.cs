using BancoIndustrialMonitor.Application.BIScraper;
using BancoIndustrialMonitor.Application.BIScraper.Commands;
using BancoIndustrialMonitor.Application.YnabController;
using BancoIndustrialMonitor.Application.YnabController.Models;
using BancoIndustrialMonitor.Programs.HttpApi;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

builder.Services.ConfigureOptionsFromEnvs();

const string corsPolicy = "MyCorsPolicy";
builder.Services.AddCors(options =>
  options.AddPolicy(corsPolicy,
    builder => builder.AllowAnyOrigin().AllowAnyHeader()));
builder.Services.AddBancoIndustrialScraper();
builder.Services.AddYnabController();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
  Console.WriteLine("Loading .env");
  DotNetEnv.Env.TraversePath().Load();
}

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

// app.UseAuthorization();

app.UseCors(corsPolicy);

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/health-check"));
app.MapGet("/health-check", () => Results.Text("ok"));

app.MapPost("/request-read-transactions/reserved",
  async (IMediator mediator) => Results.Ok(
    await mediator.Send(
      new RequestReadTransactionsCommand(ReadTransactionsType.Reserved))));

app.MapPost("/request-read-transactions/confirmed",
  async (IMediator mediator) => Results.Ok(
    await mediator.Send(
      new RequestReadTransactionsCommand(ReadTransactionsType.Confirmed))));

app.Run(url: "http://0.0.0.0:3700");
