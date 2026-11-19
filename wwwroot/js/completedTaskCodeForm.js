
// Task 27.1: Allow manual code entry as an alternative to QR scanning.
function completeTask(taskId) {
    const button = document.getElementById("btn-" + taskId);
    const form = document.getElementById("completeTaskForm-" + taskId);

    // Stop if the selected task has no manual completion form.
    if (!button || !form) {
        return;
    }

    // Hide the manual entry button and display the code entry form.
    button.style.display = "none";
    form.style.display = "inline-flex";
    form.style.gap = "6px";
    form.style.alignItems = "center";
    form.style.verticalAlign = "middle";

    // Focus the input so the player can immediately enter a code.
    const codeInput = form.querySelector('input[name="code"]');

    if (codeInput) {
        codeInput.focus();
    }
}

// Cancel manual code entry without changing task completion status.
function cancelTask(taskId) {
    const button = document.getElementById("btn-" + taskId);
    const form = document.getElementById("completeTaskForm-" + taskId);

    // Stop if the selected task has no manual completion form.
    if (!button || !form) {
        return;
    }

    // Clear the previous code to prevent accidental reuse.
    const codeInput = form.querySelector('input[name="code"]');

    if (codeInput) {
        codeInput.value = "";
    }

    // Hide the form and restore the manual entry button.
    form.style.display = "none";
    button.style.display = "inline-block";

    // Return keyboard focus to the manual entry button.
    button.focus();
}
