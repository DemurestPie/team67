// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// reads the entire page before starting

document.addEventListener("DOMContentLoaded", function()
{ 
    window.appState= JSON.parse(localStorage.getItem('SavedData')) ||   // this means that if there is already saved data load the save progress the skip to the next function to read it and if not go ahead and store the task in a collection with the id values and status
    {
        taskCompletedStatus: {}
    };


    //this function reads all the task and checks them to see if they are true (completed) or not. it will then assign the appriporiate css design for the task such as being crossed out when completed or not
    document.querySelectorAll('.task-row, [data-task-id]').forEach(task => 
    {
        let taskID = task.getAttribute('data-task-id')


        if(window.appState.taskCompletedStatus[taskID] === true)
        {
            if(task.tagName === 'LI')
            {
                task.classList.add('task-completed');

                let taskButton = task.querySelector('.btn');
                if(taskButton && taskButton.type === "submit")
                    {
                        taskButton.parentElement.outerHTML = '<span>Completed</span>';
                    }
            }

        }
    });



    document.querySelectorAll('button[data-task-id]').forEach(button => 
    {
        button.addEventListener('click', function()
        {
                let taskID = this.getAttribute('data-task-id');

                window.appState.taskCompletedStatus[taskID] = true;


                localStorage.setItem('SavedData', JSON.stringify(window.appState));
        })
        
    });
});
