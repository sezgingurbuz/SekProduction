// data-confirm taşıyan formlar gönderilmeden önce onay sorar.
document.addEventListener("submit", function (event) {
    var form = event.target;
    if (!form || !form.getAttribute) {
        return;
    }

    var message = form.getAttribute("data-confirm");
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});
