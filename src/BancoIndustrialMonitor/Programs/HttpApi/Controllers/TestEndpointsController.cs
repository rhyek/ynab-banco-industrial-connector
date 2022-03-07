using System.Threading.Channels;
using BancoIndustrialMonitor.Application.BIScraper.Events;
using BancoIndustrialMonitor.Application.BIScraper.Models;
using HttpApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HttpApi.Controllers
{
  [Route("test-endpoints")]
  [ApiController]
  public class TestEndpointsController : ControllerBase
  {
    private readonly Channel<ReadReservedTransactionsEvent>
      _readReservedTransactionsEventChannel;

    public TestEndpointsController(
      Channel<ReadReservedTransactionsEvent>
        readReservedTransactionsEventChannel)
    {
      _readReservedTransactionsEventChannel =
        readReservedTransactionsEventChannel;
    }

    [HttpPost("new-reserved-transaction")]
    public async Task<IActionResult> NewReservedTransaction(
      NewReservedTransactionDto payload)
    {
      var (reference, dateTime, amount) = payload;
      await _readReservedTransactionsEventChannel.Writer.WriteAsync(
        new(new List<ReservedTransaction> {
          new(
            reference,
            Date: DateOnly.FromDateTime(dateTime),
            amount
          )
        }));
      return Ok();
    }
  }
}
