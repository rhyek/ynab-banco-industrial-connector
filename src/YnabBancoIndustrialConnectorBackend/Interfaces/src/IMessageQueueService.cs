namespace YnabBancoIndustrialConnector.Interfaces;

public interface IMessageQueueService
{
  Task SendScrapeReservedTransactionsMessage(
    CancellationToken? cancellationToken = null);

  Task SendScrapeConfirmedTransactionsMessage(
    CancellationToken? cancellationToken = null);
}
