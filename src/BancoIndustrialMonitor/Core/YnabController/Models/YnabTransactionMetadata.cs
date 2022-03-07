namespace BancoIndustrialMonitor.Application.YnabController.Models;

public record YnabTransactionMetadata
(
  string? Reference,
  bool Auto,
  string? Description
)
{
  public static YnabTransactionMetadata DeserializeMemo(string memo)
  {
    string? reference = null;
    string? description = null;
    var auto = false;
    memo.Split(";")
      .Select(part => part.Trim())
      .Where(part => part != string.Empty)
      .ToList()
      .ForEach(part => {
        var parts = part.Split(":").Select(part => part.Trim()).ToList();
        if (parts.Count == 2) {
          var key = parts[0].ToLower();
          var value = parts[1];
          switch (key) {
            case "ref" or "reference":
              reference = value;
              break;
            case "desc" or "description":
              description = value;
              break;
            case "auto":
              auto = value == "1";
              break;
          }
        }
      });
    return new(reference, auto,
      description);
  }

  public string SerializeMemo()
  {
    var dict = new Dictionary<string, string>();
    if (Reference != null) {
      dict.Add("ref", Reference);
    }
    if (Description != null) {
      dict.Add("desc", Description);
    }
    dict.Add("auto", Auto ? "1" : "0");
    var memo = string
      .Join("; ", dict
        .Select((kv) => $"{kv.Key}: {kv.Value}")
      );
    return memo;
  }
};
