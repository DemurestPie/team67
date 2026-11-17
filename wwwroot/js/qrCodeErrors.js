// Task 27.3: Handle QR scanning errors without changing player progress.

// Return a user-friendly message based on the scanning error.
function getQrErrorMessage(error) {
    const errorName = typeof error === "string"
        ? error
        : error?.name;

    switch (errorName) {
        case "NotAllowedError":
        case "PermissionDeniedError":
            return "Camera permission was denied. Please enter the code manually.";

        case "NotFoundError":
        case "DevicesNotFoundError":
            return "No camera is available. Please enter the code manually.";

        case "NotReadableError":
        case "TrackStartError":
            return "The camera could not be accessed. Please try again or enter the code manually.";

        case "InvalidQRCode":
            return "The QR code could not be read. Please try again or enter the code manually.";

        case "SubmissionError":
            return "The scanned code could not be submitted. Please try again or enter the code manually.";

        default:
            return "QR scanning failed. Please try again or enter the code manually.";
    }
}

// Display an error for the selected task without submitting its form.
function showQrError(taskId, error) {
    const errorContainer = document.getElementById(
        "qr-error-" + taskId
    );

    // Completed or nonexistent tasks may not have an error container.
    if (!errorContainer) {
        return false;
    }

    // Display a readable error message for the player.
    errorContainer.textContent = getQrErrorMessage(error);
    errorContainer.hidden = false;

    return true;
}

// Clear previous scanning errors before another attempt.
function clearQrError(taskId) {
    const errorContainer = document.getElementById(
        "qr-error-" + taskId
    );

    if (!errorContainer) {
        return false;
    }

    errorContainer.textContent = "";
    errorContainer.hidden = true;

    return true;
}