function checkAndMakeFieldsReadOnly(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_work_order').getValue();

    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;
        let entityName = formContext.data.entity.getEntityName();

        let fetchXml = `
            <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
                <entity name="cr651_work_order">
                    <attribute name="cr651_work_orderid"/>
                    <attribute name="cr651_name"/>
                    <attribute name="cr651_os_status"/>
                    <order attribute="cr651_name" descending="false"/>
                    <link-entity name="${entityName}" from="cr651_fk_work_order" to="cr651_work_orderid" link-type="inner" alias="ax">
                        <filter type="and">
                            <condition attribute="cr651_fk_work_order" operator="eq" value="${recordId}"/> 
                        </filter>
                    </link-entity>
                </entity>
            </fetch>`;

        Xrm.WebApi.retrieveMultipleRecords("cr651_work_order", "?fetchXml=" + encodeURIComponent(fetchXml)).then(
            function (result) {
                if (result.entities.length > 0) {
                    let workOrder = result.entities[0];
                    let osStatus = workOrder.cr651_os_status;

                    if (osStatus === 523250001) {
                        makeFieldsReadOnly(formContext);
                    }
                }
            },
            function (error) {
                console.log("Ошибка при выполнении запроса: " + error.message);
            }
        );
    } else {
        console.log("Не удалось получить Work Order ID.");
    }
}

function makeFieldsReadOnly(formContext) {
    let attributes = formContext.data.entity.attributes.get();
    attributes.forEach(function (attribute) {
        let control = formContext.getControl(attribute.getName());
        if (control && control.getVisible()) {
            control.setDisabled(true);
        }
    });
}

















