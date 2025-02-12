using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Login.Models;
using Login.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Login.Controllers
{
    [Route("[controller]")]
    public class MqttController : Controller
    {
        private readonly IMqttService _mqttService;
        private readonly IConfiguration _configuration;

        public MqttController(IMqttService mqttService,IConfiguration configuration)
        {
            _configuration = configuration;
            _mqttService = mqttService;
        }

        [HttpGet("Publish")]
        public IActionResult Publish()
        {
            var rooms = new List<RoomCardModel>();
            for (int i = 1; i <= 12; i++)
            {
                rooms.Add(new RoomCardModel
                {
                    id = i,
                    RoomName = $"Room {i}",
                    Status = "Unknown",
                    Temperature = 0
                });
            }
            return View(rooms);
        }

        // POST: /Mqtt/ChangeTemp
        [HttpPost("ChangeTemp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTemp([FromBody] RoomCardModel model)
        {
            if (model == null || model.id < 1 || model.id > 12)
                return BadRequest("Invalid room ID.");

            string topic = $"ciceklisogukhavadeposu/control_room/room{model.id}/temp";
            await _mqttService.PublishAsync(topic, model.Temperature.ToString());

            return Ok(new { message = "Temperature update published" });
        }

        // POST: /Mqtt/ShutdownAll
        [Authorize(Roles = "Admin")]
        [HttpPost("ShutdownAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShutdownAll()
        {
            for (int i = 1; i <= 12; i++)
            {
                string topic = $"{_configuration["MQTT:BaseTopic"]}room{i}/temp";
                await _mqttService.PublishAsync(topic, "0");
            }
            return Ok(new { message = "Shutdown command published for all rooms" });
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