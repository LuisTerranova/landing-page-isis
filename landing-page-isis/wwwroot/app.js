window.togglePassword = function() {
    var x = document.querySelector("#password-container input");
    if (x) {
        if (x.type === "password") {
            x.type = "text";
        } else {
            x.type = "password";
        }
    }
}

window.blurActiveElement = function() {
    if (document.activeElement && typeof document.activeElement.blur === 'function') {
        document.activeElement.blur();
    }
}

window.monitorContractScroll = function(element, dotNetRef) {
    if (!element) return;

    var fired = false;
    var threshold = 30;

    function checkScroll() {
        if (fired) return;
        if (element.scrollTop + element.clientHeight >= element.scrollHeight - threshold) {
            fired = true;
            dotNetRef.invokeMethodAsync('OnScrolledToBottom');
        }
    }

    element.addEventListener('scroll', checkScroll);
    var poll = setInterval(checkScroll, 300);

    setTimeout(function () {
        checkScroll();
        if (fired) clearInterval(poll);
    }, 500);

    element._stopScrollPoll = function () {
        clearInterval(poll);
        element.removeEventListener('scroll', checkScroll);
    };
}
