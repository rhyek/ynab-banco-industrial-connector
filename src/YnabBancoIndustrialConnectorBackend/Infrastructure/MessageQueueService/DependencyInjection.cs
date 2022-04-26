using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Interfaces;

namespace MessageQueueService;

public static class DependencyInjection
{
  public static IServiceCollection AddMessageQueueService(
    this IServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<IAmazonSQS, AmazonSQSClient>();
    serviceCollection.AddSingleton<IMessageQueueService, MessageQueueService>();
    return serviceCollection;
  }
}
