document.addEventListener('DOMContentLoaded', function() {
    // Get the MQTT base topic from the container data attribute
    const mqttDashboard = document.getElementById('mqttDashboard');
    const baseTopic = mqttDashboard ? mqttDashboard.dataset.basetopic : '';
    
    // Normalize the base topic to ensure it ends with a slash
    let normalizedBaseTopic = baseTopic;
    if (normalizedBaseTopic && !normalizedBaseTopic.endsWith('/')) {
        normalizedBaseTopic += '/';
    }
    
    // Create the SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .build();

    connection.on("ReceiveNotification", (topic, payload) => {
        // Build regular expressions using the normalized base topic.
        // Expected topic formats:
        //   {BaseTopic}room{n}/status
        //   {BaseTopic}room{n}/temp
        const statusRegex = new RegExp(normalizedBaseTopic + "room(\\d+)/status");
        const tempRegex = new RegExp(normalizedBaseTopic + "room(\\d+)/temp");

        let match;
        if ((match = statusRegex.exec(topic))) {
            const roomNumber = match[1];
            const statusElement = document.getElementById(`roomStatus_${roomNumber}`);
            if (statusElement) {
                statusElement.innerText = payload;
            }
        }
        if ((match = tempRegex.exec(topic))) {
            const roomNumber = match[1];
            const tempElement = document.getElementById(`roomTemp_${roomNumber}`);
            if (tempElement) {
                tempElement.innerText = payload;
            }
        }
    });

    connection.start()
        .then(() => console.log("Connected to NotificationHub"))
        .catch(err => console.error("Error connecting to NotificationHub: ", err));

    // Helper: Get the anti-forgery token if it exists
    function getAntiForgeryToken() {
        const tokenElem = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElem ? tokenElem.value : '';
    }

    // Bind event listeners for "Set Temp" buttons
    document.querySelectorAll('.btn-set-temp').forEach(function(btn) {
        btn.addEventListener('click', function() {
            const roomId = this.dataset.roomid;
            const newTemp = prompt("Enter new temperature for Room " + roomId + ":");
            if (newTemp !== null && newTemp.trim() !== "") {
                fetch('/Mqtt/ChangeTemp', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        // Include anti-forgery token if available.
                        ...(getAntiForgeryToken() && { 'RequestVerificationToken': getAntiForgeryToken() })
                    },
                    body: JSON.stringify({ id: roomId, Temperature: newTemp })
                })
                .then(response => {
                    if (response.ok) {
                        alert("Temperature update published!");
                    } else {
                        alert("Failed to update temperature.");
                    }
                })
                .catch(error => console.error("Error:", error));
            }
        });
    });

    // Bind event listener for "Shutdown All Rooms" button (only if it exists)
    const btnShutdownAll = document.getElementById('btnShutdownAll');
    if (btnShutdownAll) {
        btnShutdownAll.addEventListener('click', function() {
            if (confirm("Are you sure you want to shutdown all rooms (set temperature to 0)?")) {
                fetch('/Mqtt/ShutdownAll', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        ...(getAntiForgeryToken() && { 'RequestVerificationToken': getAntiForgeryToken() })
                    }
                })
                .then(response => {
                    if (response.ok) {
                        alert("Shutdown command published for all rooms!");
                    } else {
                        alert("Failed to shutdown all rooms.");
                    }
                })
                .catch(error => console.error("Error:", error));
            }
        });
    }
});
