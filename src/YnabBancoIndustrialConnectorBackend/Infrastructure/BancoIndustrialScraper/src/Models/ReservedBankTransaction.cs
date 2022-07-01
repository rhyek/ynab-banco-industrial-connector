namespace YnabBancoIndustrialConnector.Domain.Models;

public record ReservedBankTransaction (
  string Reference,
  DateOnly Date,
  decimal Amount
);
