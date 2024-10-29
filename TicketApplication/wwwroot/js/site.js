// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function showNotification(message, type = 'success') {
    let notificationBox = document.getElementById("notification-box");
    if (!notificationBox) {
        notificationBox = document.createElement("div");
        notificationBox.id = "notification-box";
        notificationBox.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
        document.body.appendChild(notificationBox);
    }

    notificationBox.textContent = message;
    notificationBox.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
    notificationBox.classList.remove("d-none");
    
    setTimeout(() => {
        notificationBox.classList.add("d-none");
        notificationBox.remove();
    }, 5000);
}
