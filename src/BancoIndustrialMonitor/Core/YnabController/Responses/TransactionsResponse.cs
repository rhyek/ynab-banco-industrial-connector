using BancoIndustrialMonitor.Application.YnabController.Models;

// in typescript this could be done inline/anonymously
// <{ data: { transactions: TransactionModel[] } }>
internal class TransactionsResponse
{
  internal class DataModel
  {
    public YnabTransaction[] Transactions { get; set; } = default!;
  }
  public DataModel Data { get; set; } = default!;
}
