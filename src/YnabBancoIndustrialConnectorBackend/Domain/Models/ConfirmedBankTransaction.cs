namespace YnabBancoIndustrialConnector.Domain.Models;

public record ConfirmedBankTransaction (
  DateOnly Date,
  string Description,
  string Reference,
  decimal Amount
);
