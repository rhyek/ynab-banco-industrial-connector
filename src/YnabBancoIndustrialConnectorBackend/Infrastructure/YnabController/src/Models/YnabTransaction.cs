// ReSharper disable once CheckNamespace

using Newtonsoft.Json;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController.
  Models;

public static class YnabTransactionCleared
{
  public const string Reconciled = "reconciled";
  public const string Cleared = "cleared";
  public const string Uncleared = "uncleared";
}

public record YnabTransaction
(
  string Id,
  DateTime Date,
  decimal Amount,
  string? Memo,
  string Cleared,
  bool Approved,
  [JsonProperty("payee_id")] string? PayeeId,
  [JsonProperty("category_id")] string? CategoryId
)
{
  public bool IsOpen => Cleared != YnabTransactionCleared.Reconciled;

  public YnabTransactionMetadata Metadata =>
    YnabTransactionMetadata.DeserializeMemo(Memo);
};
