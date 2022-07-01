namespace YnabBancoIndustrialConnector.Interfaces;

public interface IConfirmedBankTransaction
{
  DateOnly Date { get; set; }
  string Description { get; set; }
  string Reference { get; set; }
  decimal Amount { get; set; }
}
