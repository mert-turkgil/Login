document.addEventListener('DOMContentLoaded', () => {
    // Example: attach a click listener to all "Set Temp" buttons
    document.querySelectorAll('.btn-set-temp').forEach(btn => {
        btn.addEventListener('click', () => {
            const roomId = btn.dataset.roomid;
            requestSetTemperature(roomId);
        });
    });

    // Example: attach a click listener to all "Start Cooling" buttons
    document.querySelectorAll('.btn-start-cooling').forEach(btn => {
        btn.addEventListener('click', () => {
            const roomId = btn.dataset.roomid;
            startCooling(roomId)
                .then(() => alert(`Cooling started for Room ${roomId}`))
                .catch(err => console.error("Error:", err));
        });
    });
});

/**
 * If room status is "0" (closed), start cooling first, then set temperature.
 * If room status is "1" (working), set temperature directly.
 */
function requestSetTemperature(roomId) {
    // Read the status from the DOM
    const statusElement = document.getElementById(`roomStatus_${roomId}`);
    const status = statusElement ? statusElement.innerText.trim() : "0";

    // Prompt user for new temperature
    const newTemp = prompt(`Enter new temperature for Room ${roomId}:`);
    if (!newTemp || newTemp.trim() === "") return; // user cancelled

    if (status === "0") {
        // Room is off => start cooling first
        startCooling(roomId)
            .then(() => changeTemperature(roomId, parseInt(newTemp, 10)))
            .catch(err => console.error("Error starting cooling first:", err));
    } else {
        // Room is on => just set the temperature
        changeTemperature(roomId, parseInt(newTemp, 10));
    }
}

/** 
 * Calls /Mqtt/StartCooling to publish "1" 
 * to pwr_rqst/room{X}/control_room/ciceklisogukhavadeposu 
 */
function startCooling(roomId) {
    return fetch('/Mqtt/StartCooling', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: roomId })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`Failed to start cooling for room ${roomId}`);
        }
        return response.json();
    })
    .then(data => {
        console.log("startCooling response:", data);
        return data; // pass data along
    });
}

/** 
 * Calls /Mqtt/ChangeTemp to publish the new temperature 
 * to set_temp/room{X}/control_room/ciceklisogukhavadeposu 
 */
function changeTemperature(roomId, newTemp) {
    return fetch('/Mqtt/ChangeTemp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: roomId, Temperature: newTemp })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`Failed to set temperature for room ${roomId}`);
        }
        return response.json();
    })
    .then(data => {
        console.log("changeTemperature response:", data);
        alert(`Room ${roomId} temperature set to ${newTemp}°C`);
        return data; // pass data along
    });
}

