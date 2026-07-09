function togglePassword() {
    var x = document.querySelector("#password-container input");
    if (x) {
        if (x.type === "password") {
            x.type = "text";
        } else {
            x.type = "password";
        }
    }
}

function blurActiveElement() {
    if (document.activeElement && typeof document.activeElement.blur === 'function') {
        document.activeElement.blur();
    }
}

function monitorContractScroll(element, dotNetRef) {
    if (!element) return;

    function checkScroll() {
        var threshold = 30;
        if (element.scrollTop + element.clientHeight >= element.scrollHeight - threshold) {
            dotNetRef.invokeMethodAsync('OnScrolledToBottom');
            element.removeEventListener('scroll', checkScroll);
        }
    }

    element.addEventListener('scroll', checkScroll);
    checkScroll();
}
