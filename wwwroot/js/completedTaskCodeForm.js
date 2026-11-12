function completeTask(taskId) 
{
    var buttonClicked = document.getElementById('btn-' + taskId);

    if (buttonClicked)
    {
        buttonClicked.style.display = "none";
    }


    var inputField = document.getElementById('completeTaskForm-' + taskId);

    if (inputField)
    {
        inputField.style.display = 'inline-flex'
        inputField.style.gap = '6px';
        inputField.style.alignItems = 'center';
        inputField.style.verticalAlign = 'middle';
    }

}

function cancelTask(taskId)
{
    var inputField = document.getElementById('completeTaskForm-' + taskId);

    if (inputField)
    {
        inputField.style.display = "none";
    }

    var buttonClicked = document.getElementById('btn-' + taskId);

    if (buttonClicked)
    {
        buttonClicked.style.display = "inline-block";
    }
}