// Task 26.3: Connect scanned QR codes to existing task validation.
// Task 27.3: Handle unsuccessful QR submissions without changing progress.

function submitScannedCode(taskId, scannedCode) {

    // Reject missing or empty QR code values.
    if (typeof scannedCode !== "string" || scannedCode.trim() === "") {
        showQrError(taskId, "InvalidQRCode");
        return false;
    }

    // Find the existing completion form for the selected task.
    const form = document.getElementById(
        "completeTaskForm-" + taskId
    );

    // Completed or nonexistent tasks do not have a completion form.
    if (!form) {
        showQrError(taskId, "SubmissionError");
        return false;
    }

    // Find the existing manual completion code input.
    const codeInput = form.querySelector('input[name="code"]');

    if (!codeInput) {
        showQrError(taskId, "SubmissionError");
        return false;
    }

    // Preserve any existing manually entered code.
    const previousCode = codeInput.value;

    try {
        // Remove previous scanning errors.
        clearQrError(taskId);

        // Place the scanned value into the existing completion form.
        codeInput.value = scannedCode;

        // Submit through the existing server-side validation system.
        // This also preserves the form's anti-forgery token.
        form.requestSubmit();

        return true;
    } catch (error) {
        // Restore the previous input if submission throws an error.
        codeInput.value = previousCode;

        showQrError(taskId, "SubmissionError");

        return false;
    }
}