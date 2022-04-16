namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;

public record ConfirmedBankTransaction (
  DateOnly Date,
  string Description,
  string Reference,
  decimal Amount
);
