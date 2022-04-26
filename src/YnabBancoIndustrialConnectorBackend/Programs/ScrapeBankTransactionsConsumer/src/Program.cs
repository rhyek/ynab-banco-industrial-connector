﻿using Amazon.Lambda.RuntimeSupport;
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
var handler = async (Stream stream) => {
  var evt = serializer.Deserialize<SQSEvent>(stream);
  foreach (var record in evt.Records) {
    Console.WriteLine($"message: {record.Body}");
  }
};

await LambdaBootstrapBuilder.Create(handler).Build().RunAsync();
