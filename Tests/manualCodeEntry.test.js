
// Task 27.1: Automated tests for manual completion code entry.
const { test } = require("node:test");
const assert = require("node:assert/strict");
const { JSDOM } = require("jsdom");
const fs = require("node:fs");
const path = require("node:path");

// Load the actual production JavaScript instead of duplicating its logic.
const script = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/completedTaskCodeForm.js"),
    "utf8"
);

// Create an isolated testing environment for each test.
function createTestEnvironment(t) {
    const dom = new JSDOM(`
        <ul id="taskList">
            <li data-task-id="1">
                <button id="btn-1">Enter Code</button>
                <button id="scan-btn-1">Scan QR Code</button>

                <form id="completeTaskForm-1" style="display: none;">
                    <input type="hidden" name="taskId" value="1">
                    <input type="text" name="code" required>
                    <button type="submit">Submit</button>
                </form>
            </li>

            <li data-task-id="2">
                <button id="btn-2">Enter Code</button>
                <button id="scan-btn-2">Scan QR Code</button>

                <form id="completeTaskForm-2" style="display: none;">
                    <input type="hidden" name="taskId" value="2">
                    <input type="text" name="code" required>
                    <button type="submit">Submit</button>
                </form>
            </li>

            <li data-task-id="3" class="task-completed">
                <span>Completed</span>
            </li>
        </ul>
    `, {
        runScripts: "outside-only"
    });

    // Execute the production script inside the simulated browser.
    dom.window.eval(script);

    // Close the browser environment after each test.
    t.after(() => dom.window.close());

    return dom.window;
}

// Test 1: Manual entry displays the selected task's form.
test("Manual code entry opens the selected task form", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);

    assert.equal(
        document.getElementById("completeTaskForm-1").style.display,
        "inline-flex"
    );
});

// Test 2: The manual entry button is hidden when its form opens.
test("Manual entry button is hidden while the form is open", (t) => {
    const window = createTestEnvironment(t);

    window.completeTask(1);

    assert.equal(
        window.document.getElementById("btn-1").style.display,
        "none"
    );
});

// Test 3: The code input automatically receives keyboard focus.
test("Opening manual entry focuses the code input", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);

    assert.equal(
        document.activeElement,
        document.querySelector('#completeTaskForm-1 input[name="code"]')
    );
});

// Test 4: Opening one task does not open another task's form.
test("Manual entry does not affect other task forms", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);

    assert.equal(
        document.getElementById("completeTaskForm-2").style.display,
        "none"
    );

    assert.notEqual(
        document.getElementById("btn-2").style.display,
        "none"
    );
});

// Test 5: Manual entry does not hide the QR scanning button.
test("QR scanning remains available during manual entry", (t) => {
    const window = createTestEnvironment(t);

    window.completeTask(1);

    assert.notEqual(
        window.document.getElementById("scan-btn-1").style.display,
        "none"
    );
});

// Test 6: Cancelling hides the form and restores the button.
test("Cancelling manual entry restores the initial interface", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);
    window.cancelTask(1);

    assert.equal(
        document.getElementById("completeTaskForm-1").style.display,
        "none"
    );

    assert.equal(
        document.getElementById("btn-1").style.display,
        "inline-block"
    );
});

// Test 7: Cancelling clears the previously entered code.
test("Cancelling manual entry clears the code input", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);

    const codeInput = document.querySelector(
        '#completeTaskForm-1 input[name="code"]'
    );

    codeInput.value = "BUC-001";

    window.cancelTask(1);

    assert.equal(codeInput.value, "");
});

// Test 8: Cancelling returns keyboard focus to the manual entry button.
test("Cancelling manual entry restores keyboard focus", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);
    window.cancelTask(1);

    assert.equal(
        document.activeElement,
        document.getElementById("btn-1")
    );
});

// Test 9: Completed tasks cannot open a missing manual entry form.
test("Manual entry safely handles completed tasks", (t) => {
    const window = createTestEnvironment(t);

    assert.doesNotThrow(() => window.completeTask(3));
    assert.doesNotThrow(() => window.cancelTask(3));
});

// Test 10: Invalid task IDs do not cause JavaScript errors.
test("Manual entry safely handles nonexistent tasks", (t) => {
    const window = createTestEnvironment(t);

    assert.doesNotThrow(() => window.completeTask(999));
    assert.doesNotThrow(() => window.cancelTask(999));
});

// Test 11: Manual entry works without the QR submission script.
test("Manual code entry works independently of QR scanning", (t) => {
    const window = createTestEnvironment(t);

    assert.equal(typeof window.submitScannedCode, "undefined");

    window.completeTask(1);

    assert.equal(
        window.document.getElementById("completeTaskForm-1").style.display,
        "inline-flex"
    );
});

// Test 12: Cancelling one task preserves another task's entered code.
test("Cancelling manual entry does not clear other task codes", (t) => {
    const window = createTestEnvironment(t);
    const document = window.document;

    window.completeTask(1);
    window.completeTask(2);

    const secondCodeInput = document.querySelector(
        '#completeTaskForm-2 input[name="code"]'
    );

    secondCodeInput.value = "BUC-002";

    window.cancelTask(1);

    assert.equal(secondCodeInput.value, "BUC-002");
    assert.equal(
        document.getElementById("completeTaskForm-2").style.display,
        "inline-flex"
    );
});
