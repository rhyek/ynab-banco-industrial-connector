namespace BancoIndustrialMonitor.Application.BIScraper.Events;

public record RequestReadTransactionsEvent(bool Reserved = false,
  bool Confirmed = false);
