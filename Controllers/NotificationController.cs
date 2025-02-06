using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Login.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Login.Controllers
{
    [Route("[controller]")]
    public class NotificationController : Controller
    {
        private readonly IMqttService _mqttService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ILogger<NotificationController> logger,IMqttService mqttService)
        {
            _logger = logger;
            _mqttService = mqttService;
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification(string message)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _mqttService.PublishAsync($"user/{userId}/notifications", message);
            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}