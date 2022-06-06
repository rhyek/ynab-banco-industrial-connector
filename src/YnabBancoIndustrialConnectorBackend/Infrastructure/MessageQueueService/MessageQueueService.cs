using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Application.Commands;
using YnabBancoIndustrialConnector.Interfaces;

namespace MessageQueueService;

public class MessageQueueService : IMessageQueueService
{
  private readonly IHostEnvironment _hostEnvironment;
  private readonly IMediator _mediator;
  private readonly IAmazonSQS _sqs;
  private readonly IOptions<ApplicationOptions> _options;

  public MessageQueueService(IHostEnvironment hostEnvironment, IMediator mediator, IAmazonSQS sqs,
    IOptions<ApplicationOptions> options)
  {
    _hostEnvironment = hostEnvironment;
    _mediator = mediator;
    _sqs = sqs;
    _options = options;
  }

  public async Task SendScrapeReservedTransactionsMessage(
    string messageDeduplicationId,
    CancellationToken? cancellationToken)
  {
    if (_hostEnvironment.IsDevelopment()) {
      await _mediator.Send(new UpdateBankReservedTransactionsCommand());
    }
    else {
      var request = new SendMessageRequest(
        _options.Value.ScrapeBankTransactionsSqsUrl!,
        "RESERVED"
      ) {
        // fifo queue properties
        MessageDeduplicationId = messageDeduplicationId,
        MessageGroupId = "default"
      };
      await _sqs.SendMessageAsync(request);
    }
  }

  public async Task SendScrapeConfirmedTransactionsMessage(
    string messageDeduplicationId,
    CancellationToken? cancellationToken)
  {
    if (_hostEnvironment.IsDevelopment()) {
      await _mediator.Send(new UpdateBankConfirmedTransactionsCommand());
    } else
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
}
