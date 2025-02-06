using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Login.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Login.Controllers
{
    [Route("[controller]")]
    public class MqttController : Controller
    {
        private readonly IMqttService _mqttService;

        public MqttController(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        [HttpGet]
        public IActionResult Publish()
        {
            // Example subscribed topics
            var subscribedTopics = new List<string>();
            for (int i = 1; i <= 12; i++)
            {
                subscribedTopics.Add($"ciceklisogukhavadeposu/control_room/room{i}/status");
                subscribedTopics.Add($"ciceklisogukhavadeposu/control_room/room{i}/temp");
            }

            ViewBag.Topics = subscribedTopics;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(string topic, string message)
        {
            await _mqttService.PublishAsync(topic, message);
            ViewBag.Message = "Message published successfully!";
            return View();
        }
    }
}