using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Interfaces;

namespace MessageQueueService;

public class MessageQueueService : IMessageQueueService
{
  private readonly IAmazonSQS _sqs;
  private readonly IOptions<ApplicationOptions> _options;

  public MessageQueueService(IAmazonSQS sqs,
    IOptions<ApplicationOptions> options)
  {
    _sqs = sqs;
    _options = options;
  }

  public async Task SendScrapeReservedTransactionsMessage(
    string messageDeduplicationId,
    CancellationToken? cancellationToken)
  {
    var request = new SendMessageRequest(
      _options.Value.ScrapeBankTransactionsSqsUrl!,
      "RESERVED"
    ) {
      MessageDeduplicationId = messageDeduplicationId,
      MessageGroupId = "default"
    };
    await _sqs.SendMessageAsync(request);
  }

  public async Task SendScrapeConfirmedTransactionsMessage(
    string messageDeduplicationId,
    CancellationToken? cancellationToken)
  {
    var request = new SendMessageRequest(
      _options.Value.ScrapeBankTransactionsSqsUrl!,
      "CONFIRMED"
    ) {
      MessageDeduplicationId = messageDeduplicationId,
      MessageGroupId = "default"
    };
    await _sqs.SendMessageAsync(request);
  }
}