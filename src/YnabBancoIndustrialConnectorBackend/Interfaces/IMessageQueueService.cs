namespace YnabBancoIndustrialConnector.Interfaces;

public interface IMessageQueueService
{
  Task SendScrapeReservedTransactionsMessage(
    string messageDeduplicationId,
    CancellationToken? cancellationToken = null);

  Task SendScrapeConfirmedTransactionsMessage(
    string messageDeduplicationId,
    CancellationToken? cancellationToken = null);

  Task SendDuplicateConfirmedReferences(
    string[] references);
}
