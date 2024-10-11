function updateFullName(executionContext) {
    var formContext = executionContext.getFormContext();
    var firstName = formContext.getAttribute("cr651_first_name").getValue();
    var lastName = formContext.getAttribute("cr651_last_name").getValue();

    var fullName = "";
    if (firstName && lastName) {
        fullName = firstName + " " + lastName;
    } else if (firstName) {
        fullName = firstName;
    } else if (lastName) {
        fullName = lastName;
    }

    formContext.getAttribute("cr651_name").setValue(fullName);
}

