function showMessage(message, type = "danger", duration = 3000) {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    const messageAlert = document.getElementById("message-alert");
    const messageText = document.getElementById("message-text");
    const messageIcon = document.getElementById("message-icon");

    messageText.textContent = message;
    messageIcon.setAttribute("xlink:href", `/images/icons.svg#${type}`);
    messageAlert.className = `alert alert-${type} d-flex align-items-center fade show`;

    setTimeout(() => {
        messageAlert.classList.remove("show");
        setTimeout(() => messageAlert.classList.add("d-none"), 500);
    }, duration);
}