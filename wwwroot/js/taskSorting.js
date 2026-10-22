function sortTasks() {
    const taskList = document.getElementById("taskList");

    if (!taskList) {
        return;
    }

    const tasks = Array.from(taskList.querySelectorAll("li"));

    tasks.sort(function (first, second) {
        const firstCompleted = first.classList.contains("task-completed");
        const secondCompleted = second.classList.contains("task-completed");

        return Number(firstCompleted) - Number(secondCompleted);
    });

    tasks.forEach(function (task) {
        taskList.appendChild(task);
    });
}

document.addEventListener("DOMContentLoaded", function () {
    const taskList = document.getElementById("taskList");

    if (!taskList) {
        return;
    }

    const observer = new MutationObserver(function (mutations) {
        for (const mutation of mutations) {
            if (mutation.type === "attributes" &&
                mutation.attributeName === "class") {
                sortTasks();
                break;
            }
        }
    });

    observer.observe(taskList, {
        subtree: true,
        attributes: true,
        attributeFilter: ["class"]
    });
});
