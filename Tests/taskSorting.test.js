
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { JSDOM } = require("jsdom");

test("Completed tasks appear below incomplete tasks", function () {
    // Arrange
    const dom = new JSDOM(`
        <ul id="taskList">
            <li class="task-completed" data-task-id="1">Task 1</li>
            <li data-task-id="2">Task 2</li>
            <li class="task-completed" data-task-id="3">Task 3</li>
            <li data-task-id="4">Task 4</li>
        </ul>
    `);

    const filePath = path.join(
        __dirname,
        "../wwwroot/js/taskSorting.js"
    );

    const sortingCode = fs.readFileSync(filePath, "utf8");

    vm.runInContext(
        sortingCode,
        vm.createContext(dom.window)
    );

    // Act
    dom.window.sortTasks();

    const tasks = Array.from(
        dom.window.document.querySelectorAll("#taskList li")
    );

    const taskIds = tasks.map(function (task) {
        return task.getAttribute("data-task-id");
    });

    // Assert
    assert.deepEqual(taskIds, ["2", "4", "1", "3"]);

    dom.window.close();
});
