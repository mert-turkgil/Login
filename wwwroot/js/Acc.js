    async function createUser() {
        let email = document.getElementById("newUserEmail").value;
        let password = document.getElementById("newUserPassword").value;

        const response = await fetch('/UserManagement/CreateUser?email=' + email + '&password=' + password, { method: 'POST' });
        const data = await response.json();
        alert(data.message);
        location.reload();
    }

    async function deleteUser(userId) {
        const response = await fetch('/UserManagement/DeleteUser?userId=' + userId, { method: 'DELETE' });
        const data = await response.json();
        alert(data.message);
        location.reload();
    }

    async function blockUser(userId) {
        const response = await fetch('/UserManagement/BlockUser?userId=' + userId, { method: 'POST' });
        const data = await response.json();
        alert(data.message);
        location.reload();
    }

    async function unblockUser(userId) {
        const response = await fetch('/UserManagement/UnblockUser?userId=' + userId, { method: 'POST' });
        const data = await response.json();
        alert(data.message);
        location.reload();
    }
