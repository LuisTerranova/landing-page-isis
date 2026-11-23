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

// CPF mask applied in the browser. MudBlazor's Mask is implemented in C# and needs a
// server round-trip per keystroke to move the caret, which drops/overwrites digits on Blazor Server.
(function() {
    function formatCpf(value) {
        var digits = value.replace(/\D/g, '').slice(0, 11);
        var result = '';
        for (var i = 0; i < digits.length; i++) {
            if (i === 3 || i === 6) {
                result += '.';
            } else if (i === 9) {
                result += '-';
            }
            result += digits[i];
        }
        return result;
    }

    function applyCpfMask(input) {
        var value = input.value || '';
        var formatted = formatCpf(value);
        if (formatted === value) {
            return;
        }

        var caret = input.selectionStart;
        var digitsBeforeCaret = value.slice(0, caret).replace(/\D/g, '').length;

        input.value = formatted;

        if (typeof input.setSelectionRange !== 'function') {
            return;
        }

        var position = 0;
        for (var i = 0, seen = 0; i < formatted.length && seen < digitsBeforeCaret; i++) {
            if (/\d/.test(formatted[i])) {
                seen++;
            }
            position = i + 1;
        }
        input.setSelectionRange(position, position);
    }

    function handleCpfInput(event) {
        var input = event.target;
        if (input && input.hasAttribute && input.hasAttribute('data-cpf-mask')) {
            applyCpfMask(input);
        }
    }

    document.addEventListener('input', handleCpfInput, true);
    document.addEventListener('change', handleCpfInput, true);
})();

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
