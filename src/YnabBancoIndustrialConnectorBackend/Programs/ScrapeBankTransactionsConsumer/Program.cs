using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using MessageQueueService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Application.Commands;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;
using YnabBancoIndustrialConnector.Domain;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;

var builder = Host.CreateDefaultBuilder(args)
  .ConfigureServices((hostContext, services) => {
    services.Configure<ApplicationOptions>(
      hostContext.Configuration.GetSection("APPLICATION"));
    services.Configure<YnabControllerOptions>(
      hostContext.Configuration.GetSection("YNAB"));
    services.Configure<BancoIndustrialScraperOptions>(
      hostContext.Configuration.GetSection("BANCO_INDUSTRIAL_SCRAPER"));
    services.AddApplication();
    services.AddBancoIndustrialScraper();
    services.AddYnabController();
    services.AddMessageQueueService();
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
    var txType = record.Body;
    var command = txType switch {
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
      using var s3Client = new AmazonS3Client();
      var s3BucketName =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACES_S3_BUCKET_NAME"); 
      context.Logger.LogInformation($"Uploading to s3 bucket: {s3BucketName}");
      var putObjectRequest = new PutObjectRequest {
        BucketName = s3BucketName,
        Key = $"playwright-scrape-{txType}-transactions-trace-file",
        FilePath = tracePath,
      };
      await s3Client.PutObjectAsync(putObjectRequest);
      context.Logger.LogInformation($"trace file uploaded to s3");
    }
  }
};

await LambdaBootstrapBuilder.Create(handler).Build().RunAsync();
