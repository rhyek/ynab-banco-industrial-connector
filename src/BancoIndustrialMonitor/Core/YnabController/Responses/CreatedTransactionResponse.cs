using BancoIndustrialMonitor.Application.YnabController.Models;

namespace BancoIndustrialMonitor.Application.YnabController.Responses;

internal class CreatedTransactionResponse
{
  internal class DataModel
  {
    public YnabTransaction Transaction { get; set; } = default!;
  }
  public DataModel Data { get; set; } = default!;
}
