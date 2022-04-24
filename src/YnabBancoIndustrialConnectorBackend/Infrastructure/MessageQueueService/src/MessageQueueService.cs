using Amazon.SQS;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Interfaces;

namespace MessageQueueService;

public class MessageQueueService : IMessageQueueService
{
  private readonly IAmazonSQS _sqs;
  private IOptions<ApplicationOptions> _options;

  public MessageQueueService(IAmazonSQS sqs,
    IOptions<ApplicationOptions> options)
  {
    _sqs = sqs;
    _options = options;
  }

  public async Task SendScrapeReservedTransactionsMessage(
    CancellationToken? cancellationToken)
  {
    await _sqs.SendMessageAsync(_options.Value.ScrapeBankTransactionsSqsUrl!,
      "RESERVED");
  }

  public async Task SendScrapeConfirmedTransactionsMessage(
    CancellationToken? cancellationToken)
  {
    await _sqs.SendMessageAsync(_options.Value.ScrapeBankTransactionsSqsUrl!,
      "CONFIRMED");
  }
}
