
// Task 26.3: Connect scanned QR codes to existing task validation.
function submitScannedCode(taskId, scannedCode) {

    // Reject missing or empty QR code values.
    if (typeof scannedCode !== "string" || scannedCode.trim() === "") {
        return false;
    }

    // Find the existing completion form for the selected task.
    const form = document.getElementById(
        "completeTaskForm-" + taskId
    );

    // Completed or nonexistent tasks do not have a completion form.
    if (!form) {
        return false;
    }

    // Find the existing manual completion code input.
    const codeInput = form.querySelector('input[name="code"]');

    if (!codeInput) {
        return false;
    }

    // Place the scanned value into the existing completion form.
    codeInput.value = scannedCode;

    // Submit through the existing server-side validation system.
    // This also preserves the form's anti-forgery token.
    form.requestSubmit();

    return true;
}

