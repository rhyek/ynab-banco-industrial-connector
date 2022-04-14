using System.Threading.Channels;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Events;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;
using Microsoft.AspNetCore.Mvc;
using YnabBancoIndustrialConnector.Programs.HttpApi.DTOs;

namespace YnabBancoIndustrialConnector.Programs.HttpApi.Controllers
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
