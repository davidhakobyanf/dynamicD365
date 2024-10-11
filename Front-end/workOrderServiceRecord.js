function calculateTotalAmountService(executionContext) {
    let formContext = executionContext.getFormContext();
    let duration = formContext.getAttribute('cr651_int_duration').getValue();
    let pricePerUnit = formContext.getAttribute('cr651_mon_price_per_unit').getValue();
    let totalAmount = 0;
    console.log(duration,'duration')
    console.log(pricePerUnit,'pricePerUnit')
    if (duration !== null && pricePerUnit !== null) {
        totalAmount = (duration/60) * pricePerUnit;
    }

    formContext.getAttribute('cr651_mon_total_amount').setValue(totalAmount);
    formContext.getControl('cr651_mon_total_amount').setDisabled(true);
}
async function onLoadPricePerUnit(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_service').getValue();
    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;
        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }

        let pricePerUnit;

        try {
            // Attempt to retrieve Price Per Unit from cr651_price_list_item
            let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
                    <entity name="cr651_price_list_item">
                        <attribute name="cr651_price_list_itemid"/>
                        <attribute name="cr651_mon_price_per_unit"/>
                        <order attribute="cr651_mon_price_per_unit" descending="false"/>
                        <filter type="and">
                            <condition attribute="cr651_fk_product" operator="eq" value="${recordId}"/>
                        </filter>
                    </entity>
                </fetch>
            `;

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let inventoryResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_price_list_item', encodedFetchXml);
            if (inventoryResults.entities.length > 0) {
                pricePerUnit = inventoryResults.entities[0]["cr651_mon_price_per_unit"];
                console.log("Price from Price List Item: ", pricePerUnit);
            } else {
                throw new Error("No price found in Price List Item. Falling back to default price.");
            }
        } catch (error) {
            console.warn(error.message);
            try {
                // Fallback to retrieve Price Per Unit from cr651_products
                let fetchXml = `
                    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
<entity name="cr651_products">
<attribute name="cr651_productsid"/>
<attribute name="cr651_name"/>
<attribute name="cr651_mon_default_price_per_unit"/>
<order attribute="cr651_name" descending="false"/>
<filter type="and">
<condition attribute="cr651_productsid" operator="eq" uitype="cr651_products" value="${recordId}"/>
</filter>
</entity>
</fetch>
                `;

                let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
                let inventoryResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_products', encodedFetchXml);
                if (inventoryResults.entities.length > 0) {
                    pricePerUnit = inventoryResults.entities[0]["cr651_mon_default_price_per_unit"];
                    console.log("Default Price from Products: ", pricePerUnit);
                } else {
                    alert("Price not found in default settings.");
                }
            } catch (fallbackError) {
                alert("Error retrieving records: " + fallbackError.message);
                console.error(fallbackError);
            }
        }

        // Set the retrieved pricePerUnit to the form field if it was found
        if (pricePerUnit !== undefined) {
            let priceField = formContext.getAttribute('cr651_mon_price_per_unit');
            priceField.setValue(pricePerUnit);
        }
    }
}
async function onSumTotalAmountService(formContext) {
    try {
        // Attempt to retrieve Price Per Unit from cr651_price_list_item
        let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true" aggregate="true">
<entity name="cr651_workorderservices">
    <attribute name="cr651_mon_total_amount" aggregate="sum" alias="cr651_mon_total_amount_sum"/>
</entity>
</fetch>
            `;
        let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
        let result = await Xrm.WebApi.retrieveMultipleRecords('cr651_workorderservices', encodedFetchXml);
        const totalAmountSum = result.entities[0]['cr651_mon_total_amount_sum'];
        console.log(totalAmountSum, 'totalAmountSum')
        if (totalAmountSum != null) {
            formContext.getAttribute('cr651_mon_total_services_amount').setValue(totalAmountSum);
        }
    } catch (error) {
        console.warn(error.message);
    }
}


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

