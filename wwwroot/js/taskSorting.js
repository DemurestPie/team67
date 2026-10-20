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