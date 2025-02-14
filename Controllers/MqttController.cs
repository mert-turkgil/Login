using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<int, bool> RoomLockStatus = new ConcurrentDictionary<int, bool>();

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(string topic, string message)
        {
            await _mqttService.PublishAsync(topic, message);
            ViewBag.Message = "Message published successfully!";
            return View();
        }
        // POST: /Mqtt/ShutdownAll
        [Authorize(Roles = "Admin")]
        [HttpPost("ShutdownAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShutdownAll()
        {
            for (int i = 1; i <= 12; i++)
            {
                string topic2 = $"{_configuration["MQTT:BaseTopic"]}room{i}/status";
                string topic = $"{_configuration["MQTT:BaseTopic"]}room{i}/temp";
                await _mqttService.PublishAsync(topic2, "0");
                await _mqttService.PublishAsync(topic, "0");
            }
            return Ok(new { message = "Shutdown command published for all rooms" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("LockRoom")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockRoom(int id)
        {
            if (id < 1 || id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account", "Home");
            }

            // Turn off all systems in the room
            string topic = $"{_configuration["MQTT:BaseTopic"]}room{id}/temp";
            await _mqttService.PublishAsync(topic, "0");

            // Lock the room
            topic = $"ciceklisogukhavadeposu/control_room/room{id}/lock";
            await _mqttService.PublishAsync(topic, "lock");

            // Update lock status
            RoomLockStatus[id] = true;

            TempData["SuccessMessage"] = $"Room {id} is now locked.";
            return RedirectToAction("Account", "Home");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("UnlockRoom")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockRoom(int id)
        {
            if (id < 1 || id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account", "Home");
            }

            // Construct the topic for unlocking this specific room.
            string topic = $"ciceklisogukhavadeposu/control_room/room{id}/unlock";
            await _mqttService.PublishAsync(topic, "unlock");

            // Update lock status
            RoomLockStatus[id] = false;

            TempData["SuccessMessage"] = $"Room {id} is now unlocked.";
            return RedirectToAction("Account", "Home");
        }

        // Modify existing methods to check lock status
        [HttpPost("ChangeTemp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTemp([FromBody] RoomCardModel model)
        {
            if (model == null || model.id < 1 || model.id > 12)
                return BadRequest("Invalid room ID.");

            if (RoomLockStatus.TryGetValue(model.id, out bool isLocked) && isLocked)
                return BadRequest("Room is locked and cannot be controlled.");

            string topic = $"{_configuration["MQTT:BaseTopic"]}room{model.id}/temp";
            await _mqttService.PublishAsync(topic, model.Temperature.ToString());

            return Ok(new { message = "Temperature update published" });
        }

        [HttpPost("StartCooling")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartCooling(int id)
        {
            if (id < 1 || id > 12)
                return BadRequest("Invalid room ID.");

            if (RoomLockStatus.TryGetValue(id, out bool isLocked) && isLocked)
                return BadRequest("Room is locked and cannot be controlled.");

            string topic2 = $"{_configuration["MQTT:BaseTopic"]}room{id}/status";
            string topic = $"{_configuration["MQTT:BaseTopic"]}room{id}/temp";
            await _mqttService.PublishAsync(topic, "1");
            await _mqttService.PublishAsync(topic2, "1");

            TempData["SuccessMessage"] = $"Cooling system started for Room {id}.";
            return RedirectToAction("Account", "Home");
        }

        [HttpPost("ChangeTempForm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTempForm([FromForm] RoomCardModel model)
        {
            if (model == null || model.id < 1 || model.id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account","Home");
            }

            string topic = $"{_configuration["MQTT:BaseTopic"]}room{model.id}/temp";
            await _mqttService.PublishAsync(topic, model.Temperature.ToString());

            TempData["SuccessMessage"] = $"Temperature update published: {model.Temperature}°C for Room {model.id}.";
            return RedirectToAction("Account","Home");
        }

        // POST: /Mqtt/ShutdownRoom
        [HttpPost("ShutdownRoom")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShutdownRoom([FromForm] int id)
        {
            if (id < 1 || id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account","Home");
            }
            int kapa = 0;
            // Construct the topic for shutting down this specific room.
            string topic2 = $"{_configuration["MQTT:BaseTopic"]}room{id}/status";
            await _mqttService.PublishAsync(topic2, kapa.ToString());

            TempData["SuccessMessage"] = $"Shutdown command published for Room {id}.";
            return RedirectToAction("Account","Home");
        }

    }
}