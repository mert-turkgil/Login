using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Login.Hubs;
using Login.Models;
using Login.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace Login.Controllers
{
    [Route("[controller]")]
    public class MqttController : Controller
    {
        private readonly IMqttService _mqttService;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        private static readonly ConcurrentDictionary<int, bool> RoomLockStatus = new ConcurrentDictionary<int, bool>();

        public MqttController(IMqttService mqttService, IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            _configuration = configuration;
            _mqttService = mqttService;
            _hubContext = hubContext;
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
                    Temperature = 0,
                    IsLocked = RoomLockStatus.TryGetValue(i, out bool locked) && locked
                });
            }
            return View(rooms);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
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
                string topicStatus = $"{_configuration["MQTT:BaseTopic"]}room{i}/status";
                string topicTemp = $"{_configuration["MQTT:BaseTopic"]}room{i}/temp";
                await _mqttService.PublishAsync(topicStatus, "0");
                await _mqttService.PublishAsync(topicTemp, "0");

                // Broadcast update for each room (shutdown: status "0", temperature 0)
                var roomUpdate = new RoomCardModel
                {
                    id = i,
                    RoomName = $"Room {i}",
                    Status = "0",
                    Temperature = 0,
                    IsLocked = RoomLockStatus.TryGetValue(i, out bool locked) && locked
                };
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);
            }
            TempData["SuccessMessage"] = "Shutdown command published for all rooms.";
            return RedirectToAction("Account", "Home");
        }

        // POST: /Mqtt/LockAll
        [Authorize(Roles = "Admin")]
        [HttpPost("LockAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAll()
        {
            for (int i = 1; i <= 12; i++)
            {
                // Turn off systems first
                string topicTemp = $"{_configuration["MQTT:BaseTopic"]}room{i}/temp";
                await _mqttService.PublishAsync(topicTemp, "0");

                // Lock each room using the lock topic
                string topicLock = $"{_configuration["MQTT:BaseTopic"]}room{i}/lock";
                await _mqttService.PublishAsync(topicLock, "lock");

                RoomLockStatus[i] = true;

                // Broadcast update: locked (status "0", temperature 0)
                var roomUpdate = new RoomCardModel
                {
                    id = i,
                    RoomName = $"Room {i}",
                    Status = "0",
                    Temperature = 0,
                    IsLocked = true
                };
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);
            }
            TempData["SuccessMessage"] = "All rooms locked.";
            return RedirectToAction("Account", "Home");
        }

        // POST: /Mqtt/UnlockAll
        [Authorize(Roles = "Admin")]
        [HttpPost("UnlockAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAll()
        {
            for (int i = 1; i <= 12; i++)
            {
                // Unlock each room using the unlock topic
                string topicUnlock = $"{_configuration["MQTT:BaseTopic"]}room{i}/unlock";
                await _mqttService.PublishAsync(topicUnlock, "unlock");

                RoomLockStatus[i] = false;

                // Broadcast update: unlocked (status "1" indicating working; temperature 0 by default)
                var roomUpdate = new RoomCardModel
                {
                    id = i,
                    RoomName = $"Room {i}",
                    Status = "1",
                    Temperature = 0,
                    IsLocked = false
                };
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);
            }
            TempData["SuccessMessage"] = "All rooms unlocked.";
            return RedirectToAction("Account", "Home");
        }

        [HttpPost("LockRoom")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockRoom(int id)
        {
            if (id < 1 || id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account", "Home");
            }

            string topicTemp = $"{_configuration["MQTT:BaseTopic"]}room{id}/temp";
            await _mqttService.PublishAsync(topicTemp, "0");

            string topicLock = $"{_configuration["MQTT:BaseTopic"]}room{id}/lock";
            await _mqttService.PublishAsync(topicLock, "lock");

            RoomLockStatus[id] = true;

            var roomUpdate = new RoomCardModel
            {
                id = id,
                RoomName = $"Room {id}",
                Status = "0",
                Temperature = 0,
                IsLocked = true
            };
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);

            TempData["SuccessMessage"] = $"Room {id} is now locked.";
            return RedirectToAction("Account", "Home");
        }

        [HttpPost("UnlockRoom")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockRoom(int id)
        {
            if (id < 1 || id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account", "Home");
            }

            string topicUnlock = $"{_configuration["MQTT:BaseTopic"]}room{id}/unlock";
            await _mqttService.PublishAsync(topicUnlock, "unlock");

            RoomLockStatus[id] = false;

            var roomUpdate = new RoomCardModel
            {
                id = id,
                RoomName = $"Room {id}",
                Status = "1",
                Temperature = 0,
                IsLocked = false
            };
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);

            TempData["SuccessMessage"] = $"Room {id} is now unlocked.";
            return RedirectToAction("Account", "Home");
        }

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

            var roomUpdate = new RoomCardModel
            {
                id = model.id,
                RoomName = $"Room {model.id}",
                Status = "1",
                Temperature = model.Temperature,
                IsLocked = isLocked
            };
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);

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

            string topicStatus = $"{_configuration["MQTT:BaseTopic"]}room{id}/status";
            string topicTemp = $"{_configuration["MQTT:BaseTopic"]}room{id}/temp";
            await _mqttService.PublishAsync(topicTemp, "1");
            await _mqttService.PublishAsync(topicStatus, "1");

            var roomUpdate = new RoomCardModel
            {
                id = id,
                RoomName = $"Room {id}",
                Status = "1",
                Temperature = 1,
                IsLocked = false
            };
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);

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
                return RedirectToAction("Account", "Home");
            }

            string topic = $"{_configuration["MQTT:BaseTopic"]}room{model.id}/temp";
            await _mqttService.PublishAsync(topic, model.Temperature.ToString());

            var roomUpdate = new RoomCardModel
            {
                id = model.id,
                RoomName = $"Room {model.id}",
                Status = model.Status,
                Temperature = model.Temperature,
                IsLocked = RoomLockStatus.TryGetValue(model.id, out bool locked) && locked
            };
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);

            TempData["SuccessMessage"] = $"Temperature update published: {model.Temperature}°C for Room {model.id}.";
            return RedirectToAction("Account", "Home");
        }

        [HttpPost("ShutdownRoom")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShutdownRoom([FromForm] int id)
        {
            if (id < 1 || id > 12)
            {
                TempData["ErrorMessage"] = "Invalid room ID.";
                return RedirectToAction("Account", "Home");
            }
            int shutdownValue = 0;
            string topicStatus = $"{_configuration["MQTT:BaseTopic"]}room{id}/status";
            await _mqttService.PublishAsync(topicStatus, shutdownValue.ToString());

            var roomUpdate = new RoomCardModel
            {
                id = id,
                RoomName = $"Room {id}",
                Status = "0",
                Temperature = 0,
                IsLocked = RoomLockStatus.TryGetValue(id, out bool locked) && locked
            };
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", roomUpdate);

            TempData["SuccessMessage"] = $"Shutdown command published for Room {id}.";
            return RedirectToAction("Account", "Home");
        }
    }
}
