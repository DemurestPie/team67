
const test = require("node:test");
const assert = require("node:assert/strict");
const { JSDOM } = require("jsdom");
const fs = require("node:fs");
const path = require("node:path");

// Load the actual QR error handling JavaScript file.
const errorScript = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/qrCodeErrors.js"),
    "utf8"
);

// Load the actual QR submission JavaScript file.
const script = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/qrCodeSubmission.js"),
    "utf8"
);

// Create an isolated browser environment for each test.
function createTestEnvironment(t) {
    const dom = new JSDOM(`
        <form id="completeTaskForm-1">
            <input type="hidden" name="taskId" value="1">
            <input type="text" name="code" required>
        </form>

        <form id="completeTaskForm-2">
            <input type="hidden" name="taskId" value="2">
            <input type="text" name="code" required>
        </form>

        <form id="completeTaskForm-3">
            <input type="hidden" name="taskId" value="3">
        </form>
    `, {
        runScripts: "outside-only"
    });

    const document = dom.window.document;
    const submissions = [];

    // Record form submissions without sending a real server request.
    document.querySelectorAll("form").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            submissions.push(form.id);
        });
    });

    // Load error handling before QR submission to match the Razor view.
    dom.window.eval(errorScript);

    // Execute the actual production QR submission JavaScript.
    dom.window.eval(script);

    // Close the isolated browser after each test.
    t.after(function () {
        dom.window.close();
    });

    return {
        document,
        submissions,
        submitScannedCode: dom.window.submitScannedCode
    };
}

// Verify that a scanned value submits the selected task form.
test("Scanned QR code submits the selected task form", (t) => {
    const env = createTestEnvironment(t);

    const result = env.submitScannedCode(1, "BUC-001");

    assert.equal(result, true);
    assert.deepEqual(env.submissions, ["completeTaskForm-1"]);
});

// Verify that the scanned value is placed into the code input.
test("Scanned QR value is assigned to the code input", (t) => {
    const env = createTestEnvironment(t);

    env.submitScannedCode(1, "BUC-001");

    const input = env.document.querySelector(
        '#completeTaskForm-1 input[name="code"]'
    );

    assert.equal(input.value, "BUC-001");
});

// Verify that scanning does not modify another task's form.
test("QR submission does not modify other task forms", (t) => {
    const env = createTestEnvironment(t);

    env.submitScannedCode(2, "BUC-002");

    const otherInput = env.document.querySelector(
        '#completeTaskForm-1 input[name="code"]'
    );

    assert.equal(otherInput.value, "");
    assert.deepEqual(env.submissions, ["completeTaskForm-2"]);
});

// Verify that empty scanned values are rejected.
test("Empty QR code is rejected", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.submitScannedCode(1, ""), false);
    assert.equal(env.submissions.length, 0);
});

// Verify that whitespace-only values are rejected.
test("Whitespace-only QR code is rejected", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.submitScannedCode(1, "   "), false);
    assert.equal(env.submissions.length, 0);
});

// Verify that null values are rejected.
test("Null QR code is rejected", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.submitScannedCode(1, null), false);
    assert.equal(env.submissions.length, 0);
});

// Verify that numeric values are rejected.
test("Numeric QR code is rejected", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.submitScannedCode(1, 12345), false);
    assert.equal(env.submissions.length, 0);
});

// Verify that a task without a completion form is rejected.
test("Missing task form prevents QR submission", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.submitScannedCode(999, "BUC-001"), false);
    assert.equal(env.submissions.length, 0);
});

// Verify that a missing code input prevents submission.
test("Missing code input prevents QR submission", (t) => {
    const env = createTestEnvironment(t);

    assert.equal(env.submitScannedCode(3, "BUC-003"), false);
    assert.equal(env.submissions.length, 0);
});

// Verify that incorrect nonempty codes are sent to the server
// instead of being validated by the browser.
test("Incorrect QR code is passed to server validation", (t) => {
    const env = createTestEnvironment(t);

    const result = env.submitScannedCode(1, "INVALID");

    assert.equal(result, true);
    assert.deepEqual(env.submissions, ["completeTaskForm-1"]);
});
