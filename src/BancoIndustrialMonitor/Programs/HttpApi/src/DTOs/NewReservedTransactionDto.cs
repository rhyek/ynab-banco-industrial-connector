namespace YnabBancoIndustrialConnector.Programs.HttpApi.DTOs;

public record NewReservedTransactionDto
(
  string Reference,
  DateTime Date,
  decimal Amount
);
