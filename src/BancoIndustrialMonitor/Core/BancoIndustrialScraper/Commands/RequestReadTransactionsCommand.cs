using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BancoIndustrialMonitor.Application.BIScraper.Events;
using MediatR;

namespace BancoIndustrialMonitor.Application.BIScraper.Commands;

public enum ReadTransactionsType
{
  Reserved,
  Confirmed
}

public record RequestReadTransactionsCommand
  (ReadTransactionsType Type) : IRequest<bool>
{
}

public class RequestReadReservedTransactionsCommandHandler : IRequestHandler<
  RequestReadTransactionsCommand, bool>
{
  private readonly Channel<RequestReadReservedTransactionsEvent>
    _requestReadReservedTransactionsEventChannel;

  private readonly Channel<RequestReadConfirmedTransactionsEvent>
    _requestReadConfirmedTransactionsEventChannel;

  public RequestReadReservedTransactionsCommandHandler(
    Channel<RequestReadReservedTransactionsEvent>
      requestReadReservedTransactionsEventChannel,
    Channel<RequestReadConfirmedTransactionsEvent>
      requestReadConfirmedTransactionsEventChannel)
  {
    _requestReadReservedTransactionsEventChannel =
      requestReadReservedTransactionsEventChannel;
    _requestReadConfirmedTransactionsEventChannel =
      requestReadConfirmedTransactionsEventChannel;
  }

  public Task<bool> Handle(RequestReadTransactionsCommand request,
    CancellationToken cancellationToken)
  {
    var result = request.Type == ReadTransactionsType.Reserved
      ? _requestReadReservedTransactionsEventChannel.Writer.TryWrite(new())
      : _requestReadConfirmedTransactionsEventChannel.Writer.TryWrite(new());
    return Task.FromResult(result);
  }
}
