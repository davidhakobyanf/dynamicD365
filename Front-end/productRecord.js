function checkCostChane(executionContext) {
    let formContext = executionContext.getFormContext();
    let cost = formContext.getAttribute('cr651_mon_cost').getValue();
    let pricePerUnit = formContext.getAttribute('cr651_mon_default_price_per_unit').getValue();
    console.log(cost,'cost')
    console.log(pricePerUnit,'pricePerUnit')
    let fieldSchemaName = "cr651_mon_cost"; // Replace this with the actual field schema name

    if (cost > pricePerUnit) {
        formContext.getControl(fieldSchemaName).setNotification("Warning cost > pricePerUnit", 2);
    } else {
        formContext.getControl(fieldSchemaName).clearNotification(2);
    }
}