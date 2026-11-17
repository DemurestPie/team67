const test = require("node:test");
const assert = require("node:assert/strict");
const { JSDOM } = require("jsdom");
const fs = require("node:fs");
const path = require("node:path");

// Task 27.3: Test QR error handling without changing player progress.

// Load the actual production JavaScript files.
const errorScript = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/qrCodeErrors.js"),
    "utf8"
);

const submissionScript = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/qrCodeSubmission.js"),
    "utf8"
);

const manualScript = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/completedTaskCodeForm.js"),
    "utf8"
);

// Create an isolated browser environment for each test.
function createTestEnvironment(t) {
    const dom = new JSDOM(`
        <ul id="taskList">
            <li class="task-row" data-task-id="1">
                <button id="btn-1">Enter Code</button>
                <button id="scan-btn-1">Scan QR Code</button>

                <div id="qr-error-1" role="alert" hidden></div>

                <form id="completeTaskForm-1" style="display: none;">
                    <input type="hidden" name="taskId" value="1">
                    <input type="text" name="code" required>
                </form>
            </li>

            <li class="task-row" data-task-id="2">
                <button id="btn-2">Enter Code</button>
                <button id="scan-btn-2">Scan QR Code</button>

                <div id="qr-error-2" role="alert" hidden></div>

                <form id="completeTaskForm-2" style="display: none;">
                    <input type="hidden" name="taskId" value="2">
                    <input type="text" name="code" required>
                </form>
            </li>

            <li class="task-row task-completed" data-task-id="3">
                <span>Completed</span>
            </li>
        </ul>
    `, {
        runScripts: "outside-only"
    });

    const document = dom.window.document;
    const submissions = [];

    // Record submissions without sending requests to the server.
    document.querySelectorAll("form").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            submissions.push(form.id);
        });
    });

    // Load the actual application scripts.
    dom.window.eval(manualScript);
    dom.window.eval(errorScript);
    dom.window.eval(submissionScript);

    // Close the browser environment after each test.
    t.after(function () {
        dom.window.close();
    });

    return {
        document,
        submissions,
        window: dom.window
    };
}

// Test 1: Camera permission denial.
test("Camera permission denial displays manual entry guidance", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, { name: "NotAllowedError" });

    const error = env.document.getElementById("qr-error-1");

    assert.equal(error.hidden, false);
    assert.match(error.textContent, /permission was denied/i);
    assert.match(error.textContent, /manually/i);
    assert.equal(env.submissions.length, 0);
});

// Test 2: Unavailable camera.
test("Unavailable camera displays an error without submitting", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, { name: "NotFoundError" });

    const error = env.document.getElementById("qr-error-1");

    assert.equal(error.hidden, false);
    assert.match(error.textContent, /no camera/i);
    assert.equal(env.submissions.length, 0);
});

// Test 3: Camera access failure.
test("Camera access failure displays recovery guidance", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, { name: "NotReadableError" });

    const error = env.document.getElementById("qr-error-1");

    assert.equal(error.hidden, false);
    assert.match(error.textContent, /could not be accessed/i);
    assert.match(error.textContent, /manually/i);
    assert.equal(env.submissions.length, 0);
});

// Test 4: Failed QR scan.
test("Failed QR scan displays an error without submitting", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, "InvalidQRCode");

    const error = env.document.getElementById("qr-error-1");

    assert.equal(error.hidden, false);
    assert.match(error.textContent, /could not be read/i);
    assert.equal(env.submissions.length, 0);
});

// Test 5: Unexpected scanner error.
test("Unexpected scanner errors display a general error message", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, { name: "UnexpectedError" });

    const error = env.document.getElementById("qr-error-1");

    assert.equal(error.hidden, false);
    assert.match(error.textContent, /QR scanning failed/i);
    assert.equal(env.submissions.length, 0);
});

// Test 6: Errors remain associated with the selected task.
test("QR errors do not affect other tasks", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, "InvalidQRCode");

    const firstError = env.document.getElementById("qr-error-1");
    const secondError = env.document.getElementById("qr-error-2");

    assert.equal(firstError.hidden, false);
    assert.equal(secondError.hidden, true);
    assert.equal(secondError.textContent, "");
});

// Test 7: Previously completed tasks remain completed.
test("Scanning failure preserves existing completion status", (t) => {
    const env = createTestEnvironment(t);

    const originalTasks = env.document
        .getElementById("taskList")
        .querySelectorAll("li");

    const originalClasses = Array.from(originalTasks)
        .map(task => task.className);

    env.window.showQrError(1, "NotFoundError");

    const currentClasses = Array.from(originalTasks)
        .map(task => task.className);

    assert.deepEqual(currentClasses, originalClasses);
    assert.equal(originalTasks[2].textContent.trim(), "Completed");
    assert.equal(env.submissions.length, 0);
});

// Test 8: Manual entry remains available after failure.
test("Manual entry remains available after a scanning failure", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, "NotAllowedError");

    const button = env.document.getElementById("btn-1");

    assert.notEqual(button.style.display, "none");
    assert.equal(button.disabled, false);

    env.window.completeTask(1);

    const form = env.document.getElementById("completeTaskForm-1");
    const input = form.querySelector('input[name="code"]');

    assert.equal(form.style.display, "inline-flex");
    assert.equal(env.document.activeElement, input);
});

// Test 9: Previous errors can be cleared.
test("Clearing a QR error restores the hidden error container", (t) => {
    const env = createTestEnvironment(t);

    env.window.showQrError(1, "InvalidQRCode");
    env.window.clearQrError(1);

    const error = env.document.getElementById("qr-error-1");

    assert.equal(error.hidden, true);
    assert.equal(error.textContent, "");
});

// Test 10: Completed tasks cannot receive scanner errors.
test("QR error handler safely handles completed tasks", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.window.showQrError(3, "InvalidQRCode"), false);
    assert.equal(env.window.clearQrError(3), false);
    assert.equal(env.submissions.length, 0);
});

// Test 11: Nonexistent tasks are safely handled.
test("QR error handler safely handles nonexistent tasks", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.window.showQrError(999, "InvalidQRCode"), false);
    assert.equal(env.window.clearQrError(999), false);
    assert.equal(env.submissions.length, 0);
});

// Test 12: Empty scanned codes display an error.
test("Empty scanned code displays an error without submitting", (t) => {
    const env = createTestEnvironment(t);

    const result = env.window.submitScannedCode(1, "");

    const error = env.document.getElementById("qr-error-1");

    assert.equal(result, false);
    assert.equal(error.hidden, false);
    assert.match(error.textContent, /could not be read/i);
    assert.equal(env.submissions.length, 0);
});

// Test 13: Failed scans preserve manually entered codes.
test("Failed QR scan preserves an existing manual code", (t) => {
    const env = createTestEnvironment(t);

    const input = env.document.querySelector(
        '#completeTaskForm-1 input[name="code"]'
    );

    input.value = "BUC-001";

    env.window.submitScannedCode(1, "");

    assert.equal(input.value, "BUC-001");
    assert.equal(env.submissions.length, 0);
});

// Test 14: Submission exceptions are handled safely.
test("Submission exception restores previous code and displays error", (t) => {
    const env = createTestEnvironment(t);

    const form = env.document.getElementById("completeTaskForm-1");
    const input = form.querySelector('input[name="code"]');

    input.value = "PREVIOUS-CODE";

    // Simulate an unexpected submission failure.
    form.requestSubmit = function () {
        throw new Error("Simulated submission failure");
    };

    const result = env.window.submitScannedCode(1, "BUC-001");
    const error = env.document.getElementById("qr-error-1");

    assert.equal(result, false);
    assert.equal(input.value, "PREVIOUS-CODE");
    assert.equal(error.hidden, false);
    assert.match(error.textContent, /could not be submitted/i);
    assert.equal(env.submissions.length, 0);
});

// Test 15: Players can retry after an unsuccessful scan.
test("Successful QR submission clears a previous scanning error", (t) => {
    const env = createTestEnvironment(t);

    env.window.submitScannedCode(1, "");

    const result = env.window.submitScannedCode(1, "BUC-001");
    const error = env.document.getElementById("qr-error-1");

    assert.equal(result, true);
    assert.equal(error.hidden, true);
    assert.equal(error.textContent, "");
    assert.deepEqual(env.submissions, ["completeTaskForm-1"]);
});