using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
  .ConfigureServices((hostContext, services) => {
    // services.AddHostedService<Worker>();
  });
var serviceProvider = builder.Build();
var serializer = new DefaultLambdaJsonSerializer();

// ReSharper disable once ConvertToLocalFunction
var handler = async (Stream stream, ILambdaContext context) => {
  var evt = serializer.Deserialize<SQSEvent>(stream);
  foreach (var record in evt.Records) {
    context.Logger.LogInformation($"message: {record.Body}");
  }
};

await LambdaBootstrapBuilder.Create(handler).Build().RunAsync();
