const connection = new signalR.HubConnectionBuilder()
.withUrl("/notificationHub")
.build();

// Listen for incoming MQTT notifications
connection.on("ReceiveNotification", (topic, payload) => {
// Topics:
// ciceklisogukhavadeposu/control_room/room1/status
// ciceklisogukhavadeposu/control_room/room1/temp
const statusRegex = /ciceklisogukhavadeposu\/control_room\/room(\d+)\/status/;
const tempRegex = /ciceklisogukhavadeposu\/control_room\/room(\d+)\/temp/;

let match;
if (match = statusRegex.exec(topic)) {
    const roomNumber = match[1];
    const statusElement = document.getElementById(`roomStatus_${roomNumber}`);
    if (statusElement) {
        statusElement.innerText = payload;
        // Update badge class: "1" means Working
        statusElement.className = payload === "1" ? "badge bg-success" : "badge bg-danger";
    }
    updateRoomData(roomNumber);
}
if (match = tempRegex.exec(topic)) {
    const roomNumber = match[1];
    // Update temperature display
    const tempElement = document.getElementById(`roomTemp_${roomNumber}`);
    if (tempElement) {
        tempElement.innerText = payload + "°C";
        tempElement.style.color = (parseFloat(payload) > 30) 
        ? "red" 
        : (parseFloat(payload) < 10 ? "blue" : "black");
    }
    // Calculate and update weather condition based on temperature
    const weatherElement = document.getElementById(`weather_${roomNumber}`);
    if (weatherElement) {
        const tempVal = parseFloat(payload);
        let weather = "";
        if (tempVal >= 50) weather = "🔥 Burning Hot";
        else if (tempVal > 30) weather = "☀️ Sunny";
        else if (tempVal > 20) weather = "🌤️ Pleasant";
        else if (tempVal > 10) weather = "🌥️ Chilly";
        else if (tempVal >= 0) weather = "❄️ Snowy";
        else weather = "🧊 Ice Cold";
        weatherElement.innerText = weather;
    }
    updateRoomData(roomNumber);
}
});

connection.start()
.then(() => console.log("Connected to NotificationHub"))
.catch(err => console.error("Error connecting to NotificationHub: ", err));

   // Helper function: Reads current DOM values and sends them to the server
   function updateRoomData(roomNumber) {
    const statusElement = document.getElementById(`roomStatus_${roomNumber}`);
    const tempElement = document.getElementById(`roomTemp_${roomNumber}`);
    const status = statusElement ? statusElement.innerText : "";
    const tempText = tempElement ? tempElement.innerText : "0°C";
    // Remove the "°C" suffix and convert to number
    const temperature = parseFloat(tempText.replace("°C", "")) || 0;
    
    // Build an object matching your RoomCardModel
    const data = {
        id: parseInt(roomNumber),
        Status: status,       // Note: your ChangeTemp endpoint will override this to "1"
        Temperature: temperature
    };

    // Call your existing endpoint in MqttController
    fetch('/Mqtt/ChangeTemp', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    })
    .then(response => response.json())
    .then(result => console.log("Room updated", result))
    .catch(err => console.error("Error updating room", err));
}
function refreshButtons(roomNumber) {
    const statusElement = document.getElementById(`roomStatus_${roomNumber}`);
    const startCoolingBtn = document.getElementById(`btnStartCooling_${roomNumber}`);
    const tempGroup = document.getElementById(`tempGroup_${roomNumber}`);
    
    if (!statusElement || !startCoolingBtn || !tempGroup) return;
    
    const status = statusElement.innerText.trim();
    
    if (status === "0") {
        // Room is closed
        // Show "Start Cooling", hide or disable the temp group
        startCoolingBtn.style.display = "inline-block";
        tempGroup.style.display = "none";
    } else if (status === "1") {
        // Room is working
        // Hide or disable "Start Cooling", show the temp group
        startCoolingBtn.style.display = "none";
        tempGroup.style.display = "flex"; // or "block"
    } else {
        // If status is something else (e.g. "Loading..."), show/hide as needed
        startCoolingBtn.style.display = "none";
        tempGroup.style.display = "none";
    }
}
