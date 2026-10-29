// the data to test the function. this is the peice of data used for this test

const mockStorage = {};
global.localStorage = {
    setItem: (key, val) => { mockStorage[key] = String(val); },
    getItem: (key) => mockStorage[key] || null,
    clear: () => { for (let key in mockStorage) delete mockStorage[key]; }
};


let tasksStates = {
    completedTaskList: []
};


// This is extracting the saved data from local storage using its Element ID and parsing it into a json file

function savingTasks()
{
    localStorage.setItem("SavedData", JSON.stringify(tasksStates));
}


function loadTasks()
{
    let data = localStorage.getItem("SavedData");


    if(data)
    {
        tasksStates = JSON.parse(data);
    }
    else
    {
        tasksStates = {
            completedTaskList: []
        };
    }
}


test("Testing to make sure that the saved completetion tasks states are empty in local storage", () =>
    {
        loadTasks();

        expect(tasksStates.completedTaskList).toEqual([]);
        expect(tasksStates.completedTaskList.length).toBe(0);
    }

)

test("Testing to make sure that the saved completetion tasks states aere saved while refreshing in local storage", () =>
    {
          tasksStates.completedTaskList = [];
          tasksStates.completedTaskList.push("task-89");
          savingTasks();


          tasksStates = {completedTaskList: []};


          loadTasks();

          expect(tasksStates.completedTaskList).toContain("task-89");
          expect(tasksStates.completedTaskList.length).toBe(1);
    }

)






