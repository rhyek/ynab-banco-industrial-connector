namespace HttpApi.DTOs;

public record NewReservedTransactionDto
(
  string Reference,
  DateTime Date,
  decimal Amount
);
