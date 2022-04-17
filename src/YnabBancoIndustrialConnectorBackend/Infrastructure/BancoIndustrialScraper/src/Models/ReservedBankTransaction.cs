namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;

public record ReservedBankTransaction (
  string Reference,
  DateOnly Date,
  decimal Amount
);
