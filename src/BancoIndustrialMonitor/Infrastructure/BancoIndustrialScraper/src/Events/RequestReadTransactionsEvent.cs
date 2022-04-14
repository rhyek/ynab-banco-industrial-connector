namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Events;

public record RequestReadTransactionsEvent(bool Reserved = false,
  bool Confirmed = false);
