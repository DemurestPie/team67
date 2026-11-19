const test = require("node:test");
const assert = require("node:assert/strict");
const { JSDOM } = require("jsdom");
const fs = require("node:fs");
const path = require("node:path");

// Task 27.2:
// Verify that manual code entry and QR scanning use the same
// task completion form and server-side validation endpoint.

const qrSubmissionScript = fs.readFileSync(
    path.join(__dirname, "../wwwroot/js/qrCodeSubmission.js"),
    "utf8"
);

function createTestEnvironment(t) {
    const dom = new JSDOM(`
        <form id="completeTaskForm-1"
              action="/Player/SubmitCode"
              method="post">

            <input type="hidden"
                   name="taskId"
                   value="1">

            <input type="text"
                   name="code"
                   required>

            <button type="submit">
                Submit
            </button>
        </form>
    `, {
        runScripts: "outside-only"
    });

    const document = dom.window.document;
    const submissions = [];

    // QR submission uses these functions for error handling.
    dom.window.showQrError = function () {};
    dom.window.clearQrError = function () {};

    const form = document.getElementById("completeTaskForm-1");

    form.addEventListener("submit", function (event) {
        event.preventDefault();

        submissions.push({
            action: form.action,
            taskId: form.querySelector('[name="taskId"]').value,
            code: form.querySelector('[name="code"]').value
        });
    });

    dom.window.eval(qrSubmissionScript);

    t.after(() => dom.window.close());

    return {
        window: dom.window,
        document,
        form,
        submissions
    };
}


// Task 27.2:
// Manual entry submits through the existing SubmitCode endpoint.
test("Manual entry uses the shared task validation form", (t) => {
    const env = createTestEnvironment(t);

    const codeInput =
        env.form.querySelector('[name="code"]');

    codeInput.value = "BUC-001";

    env.form.requestSubmit();

    assert.equal(env.submissions.length, 1);
    assert.equal(env.submissions[0].taskId, "1");
    assert.equal(env.submissions[0].code, "BUC-001");
    assert.match(
        env.submissions[0].action,
        /\/Player\/SubmitCode$/
    );
});


// Task 27.2:
// QR scanning uses the exact same form and SubmitCode endpoint.
test("QR scanning uses the shared task validation form", (t) => {
    const env = createTestEnvironment(t);

    const result =
        env.window.submitScannedCode(1, "BUC-001");

    assert.equal(result, true);
    assert.equal(env.submissions.length, 1);
    assert.equal(env.submissions[0].taskId, "1");
    assert.equal(env.submissions[0].code, "BUC-001");
    assert.match(
        env.submissions[0].action,
        /\/Player\/SubmitCode$/
    );
});