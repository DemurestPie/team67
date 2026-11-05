const test = require("node:test");
const assert = require("node:assert/strict");

const mockStorage = {};

global.localStorage = {
    setItem: (key, val) => {
        mockStorage[key] = String(val);
    },
    getItem: (key) => mockStorage[key] ?? null,
    clear: () => {
        for (const key in mockStorage) {
            delete mockStorage[key];
        }
    }
};

let tasksStates = {
    completedTaskList: []
};

function savingTasks() {
    localStorage.setItem("SavedData", JSON.stringify(tasksStates));
}

function loadTasks() {
    const data = localStorage.getItem("SavedData");

    tasksStates = data
        ? JSON.parse(data)
        : { completedTaskList: [] };
}

test("Completed tasks are empty when no saved data exists", () => {
    localStorage.clear();
    tasksStates = { completedTaskList: [] };

    loadTasks();

    assert.deepEqual(tasksStates.completedTaskList, []);
    assert.equal(tasksStates.completedTaskList.length, 0);
});

test("Completed tasks are restored after refreshing", () => {
    localStorage.clear();
    tasksStates = { completedTaskList: ["task-89"] };

    savingTasks();

    // Simulate losing the in-memory state during a refresh.
    tasksStates = { completedTaskList: [] };

    loadTasks();

    assert.ok(tasksStates.completedTaskList.includes("task-89"));
    assert.equal(tasksStates.completedTaskList.length, 1);
});