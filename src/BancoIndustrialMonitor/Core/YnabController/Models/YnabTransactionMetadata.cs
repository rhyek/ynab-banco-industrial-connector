namespace BancoIndustrialMonitor.Application.YnabController.Models;

public record YnabTransactionMetadata
{
  public string? Reference { get; init; }
  public bool Auto { get; init; }
  public string? Description { get; init; }
  public string? MyDescription { get; init; }
  public DateOnly? OverrideDate { get; init; }

  public YnabTransactionMetadata(string? reference, bool auto,
    string? description, string? myDescription = null,
    DateOnly? overrideDate = null)
  {
    Reference = reference;
    Auto = auto;
    Description = description?.Replace(";", "_");
    MyDescription = myDescription?.Replace(";", "_");
    OverrideDate = overrideDate;
  }

  public static YnabTransactionMetadata DeserializeMemo(string memo)
  {
    string? reference = null;
    string? description = null;
    string? myDescription = null;
    DateOnly? overrideDate = null;
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
            case "mydesc" or "midesc":
              myDescription = value;
              break;
            case "auto":
              auto = value == "1";
              break;
            case "ovrddate":
              overrideDate = DateOnly.Parse(value);
              break;
          }
        }
      });
    return new(reference, auto,
      description, myDescription, overrideDate);
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
    if (!string.IsNullOrWhiteSpace(MyDescription)) {
      dict.Add("mydesc", MyDescription);
    }
    if (OverrideDate.HasValue) {
      dict.Add("ovrddate", OverrideDate.Value.ToString("o"));
    }
    dict.Add("auto", Auto ? "1" : "0");
    var memo = string
      .Join("; ", dict
        .Select((kv) => $"{kv.Key}: {kv.Value}")
      );
    return memo;
  }
};
