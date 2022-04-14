using System;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;

public record ConfirmedTransaction (
  DateOnly Date,
  string Description,
  string Reference,
  decimal Amount
);
