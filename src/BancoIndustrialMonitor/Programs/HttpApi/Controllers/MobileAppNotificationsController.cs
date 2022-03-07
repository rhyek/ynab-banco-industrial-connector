using BancoIndustrialMonitor.Application.YnabController.Repositories;
using BancoIndustrialScraper.Models;
using HttpApi.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using YnabController.Commands;

namespace HttpApi.Controllers
{
  [Route("mobile-app-notifications")]
  [ApiController]
  public class MobileAppNotificationsController : ControllerBase
  {
    private readonly ILogger<MobileAppNotificationsController> _logger;
    private readonly IMediator _mediator;

    public MobileAppNotificationsController(
      ILogger<MobileAppNotificationsController> logger,
      IMediator mediator)
    {
      _logger = logger;
      _mediator = mediator;
    }

    [HttpPost("register-new")]
    public async Task<IActionResult> RegisterNew(MobileNotificationDto payload)
    {
      _logger.LogInformation("Mobile notification of transaction received: {Message}",
        payload.Text);
      await _mediator.Send(new NewMobileNotificationTransactionCommand() {
        MobileNotificationText = payload.Text
      });
      
      return Ok();
    }
  }
}
