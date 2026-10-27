const test = require("node:test");
const assert = require("node:assert/strict");
const { JSDOM } = require("jsdom");

test("Completing a task updates its displayed status", function () {
    // Arrange
    const dom = new JSDOM(`
        <ul id="taskList">
            <li class="task-row" data-task-id="1">
                Task 1: Find the Buccaneer Statue
                <button class="complete-button">Complete Task</button>
            </li>
        </ul>
    `);

    const document = dom.window.document;

    const task = document.querySelector('[data-task-id="1"]');
    const button = task.querySelector(".complete-button");

    // Act
    button.addEventListener("click", function () {
        task.classList.add("task-completed");
        button.remove();

        const completedStatus = document.createElement("span");
        completedStatus.textContent = "Completed";
        task.appendChild(completedStatus);
    });

    button.click();

    // Assert
    assert.equal(task.classList.contains("task-completed"), true);
    assert.equal(task.textContent.includes("Completed"), true);
    assert.equal(task.querySelector(".complete-button"), null);

    dom.window.close();
});