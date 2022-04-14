using System;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;

public record ReservedTransaction (
  string Reference,
  DateOnly Date,
  decimal Amount
);
