function testFunction(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_product').getValue();
    if (lookupValue != null && lookupValue[0] != null) {
        let productName = lookupValue[0].name;
        formContext.getAttribute('cr651_name').setValue(productName);
    }
}

function togglePricePerUnitVisibility(executionContext) {
    let formContext = executionContext.getFormContext();
    let typeValues = formContext.getAttribute('cr651_os_type').getValue();
    let pricePerUnitControl = formContext.getControl('cr651_mon_default_price_per_unit');
    if (pricePerUnitControl && typeValues && Array.isArray(typeValues) && typeValues.includes(523250000)) {
        pricePerUnitControl.setVisible(true);
    } else {
        pricePerUnitControl.setVisible(false);
    }
}

function calculateTotalAmount(executionContext) {
    let formContext = executionContext.getFormContext();
    let quantity = formContext.getAttribute('cr651_int_quantity').getValue();
    let pricePerUnit = formContext.getAttribute('cr651_mon_price_per_unit').getValue();
    let totalAmount = 0;
    if (quantity !== null && pricePerUnit !== null) {
        totalAmount = quantity * pricePerUnit;
    }

    formContext.getAttribute('cr651_mon_total_amount').setValue(totalAmount);
    formContext.getControl('cr651_mon_total_amount').setDisabled(true);
}

function toggleFieldsBasedOnFormType(executionContext) {
    let formContext = executionContext.getFormContext();
    let formType = formContext.ui.getFormType();
    let controls = formContext.ui.controls.get();
    if (formType === 2) {
        controls.forEach(function (control) {
            control.setDisabled(true);
        });
    } else if (formType === 1) {
        controls.forEach(function (control) {
            control.setDisabled(false);
        });
    }
}

async function onLoadCurrency(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_inventory').getValue();
    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;

        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }

        try {
            // FetchXML to retrieve related Price List IDs and transactioncurrencyid
            let fetchXml = `
               <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
<entity name="cr651_inventory">
<attribute name="cr651_inventoryid"/>
<attribute name="cr651_name"/>
<order attribute="cr651_name" descending="false"/>
<filter type="and">
<condition attribute="cr651_inventoryid" operator="eq" uiname="w1" uitype="cr651_inventory" value="${recordId}"/>
</filter>
<link-entity name="cr651_pricelist" from="cr651_pricelistid" to="cr651_fk_price_list" link-type="inner" alias="aa">
<attribute name="transactioncurrencyid"/>
</link-entity>
</entity>
</fetch>
               `

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let inventoryResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_inventory', encodedFetchXml);
            if (inventoryResults.entities.length > 0) {
                let transactionCurrencyId = [
                    {
                        id: inventoryResults.entities[0][`aa.transactioncurrencyid`],
                        name: inventoryResults.entities[0][`aa.transactioncurrencyid@OData.Community.Display.V1.FormattedValue`],
                        entityType: inventoryResults.entities[0][`aa.transactioncurrencyid@Microsoft.Dynamics.CRM.lookuplogicalname`],
                    }
                ];

                formContext.getAttribute('transactioncurrencyid').setValue(transactionCurrencyId);
            } else {
                alert("Currency ID not found in any Price List.");
            }
        } catch
            (error) {
            alert("Error retrieving records: " + error.message);
            console.error(error);
        }
    }
}

async function onLoadPricePerUnit(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_product').getValue();
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

async function onLoadPriceList(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_price_list').getValue();
    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;

        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }

        try {
            // FetchXML to retrieve related Price List IDs and transactioncurrencyid
            let fetchXml = `
               <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
<entity name="cr651_pricelist">
<attribute name="cr651_pricelistid"/>
<attribute name="cr651_name"/>
<attribute name="transactioncurrencyid"/>
<order attribute="cr651_name" descending="false"/>
<filter type="and">
<condition attribute="cr651_pricelistid" operator="eq" value="${recordId}"/>
</filter>
</entity>
</fetch>

               `

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let inventoryResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_pricelist', encodedFetchXml);

            if (inventoryResults.entities.length > 0) {
                let transactionCurrencyId = [
                    {
                        id: inventoryResults.entities[0][`_transactioncurrencyid_value`],
                        name: inventoryResults.entities[0][`_transactioncurrencyid_value@OData.Community.Display.V1.FormattedValue`],
                        entityType: inventoryResults.entities[0][`_transactioncurrencyid_value@Microsoft.Dynamics.CRM.lookuplogicalname`],
                    }
                ];

                formContext.getAttribute('transactioncurrencyid').setValue(transactionCurrencyId);
            } else {
                alert("Currency ID not found in any Price List.");
            }
        } catch
            (error) {
            alert("Error retrieving records: " + error.message);
            console.error(error);
        }
    }
}

// TASK 10
async function onCheckProduct(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_inventory').getValue();
    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;

        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }
        let formContext = executionContext.getFormContext();
        let fieldSchemaName = "cr651_fk_product"; // Replace this with the actual field schema name

        if (recordId) {
            try {
                // FetchXML to retrieve related Product IDs for the given Inventory
                let fetchXml = `
               <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
                   <entity name="cr651_inventory_product">
                       <attribute name="cr651_inventory_productid"/>
                       <attribute name="cr651_name"/>
                       <attribute name="cr651_fk_product"/>
                       <order attribute="cr651_name" descending="false"/>
                       <filter type="and">
                           <condition attribute="cr651_fk_inventory" operator="eq" value="${recordId}"/>
                       </filter>
                   </entity>
               </fetch>
            `;

                let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
                let inventoryProductResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_inventory_product', encodedFetchXml);

                if (inventoryProductResults.entities.length > 0) {
                    let currentProductId = formContext.getAttribute(fieldSchemaName).getValue();
                    if (currentProductId && currentProductId[0]) {
                        // Normalize both IDs by removing curly braces and converting to lowercase
                        let normalizedCurrentProductId = currentProductId[0].id.replace(/{|}/g, "").toLowerCase();

                        let productExists = inventoryProductResults.entities.some(entity => {
                            let normalizedEntityProductId = entity["_cr651_fk_product_value"].toLowerCase();
                            return normalizedEntityProductId === normalizedCurrentProductId;
                        });

                        if (productExists) {
                            formContext.getControl(fieldSchemaName).setNotification("Product is already added", 1);
                        } else {
                            formContext.getControl(fieldSchemaName).clearNotification(1);
                        }
                    }
                } else {
                    console.log("No related products found.");
                }
            } catch (error) {
                console.error("Error retrieving records: " + error.message);
            }
        }
    }
}

async function deleteRecordPriceList(formContext) {
    if (formContext) {
        let recordId = formContext.data.entity.getId();

        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }
        try {
            let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
                    <entity name="cr651_price_list_item">
                        <attribute name="cr651_price_list_itemid"/>
                        <attribute name="cr651_name"/>
                        <attribute name="createdon"/>
                        <order attribute="cr651_name" descending="false"/>
                        <filter type="and">
                            <condition attribute="cr651_fk_price_list" operator="eq" uitype="cr651_pricelist" value="${recordId}"/>
                        </filter>
                    </entity>
                </fetch>
            `;

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let PriceListItemResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_price_list_item', encodedFetchXml);
            if (PriceListItemResults.entities.length > 0) {
                for (let i = 0; i < PriceListItemResults.entities.length; i++) {
                    let id = PriceListItemResults.entities[i]['cr651_price_list_itemid'];
                    let result = await Xrm.WebApi.deleteRecord("cr651_price_list_item", id);
                    console.log("Record deleted:", result);
                }
            } else {
                console.log("No records found.");
            }
            console.log(PriceListItemResults);
        } catch (error) {
            console.log(error);
        }

        let lookupCurrency = null;
        let lookupPriceList = null;
        try {
            let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
                    <entity name="cr651_pricelist">
                        <attribute name="cr651_pricelistid"/>
                        <attribute name="cr651_name"/>
                        <attribute name="transactioncurrencyid"/>
                        <order attribute="cr651_name" descending="false"/>
                        <filter type="and">
                            <condition attribute="cr651_pricelistid" operator="eq" value="${recordId}"/>
                        </filter>
                    </entity>
                </fetch>
            `;
            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);

            let currencyResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_pricelist', encodedFetchXml);
            if (currencyResults.entities.length > 0) {
                let pricelistid = currencyResults.entities[0][`cr651_pricelistid`];
                let pricelistName = currencyResults.entities[0][`cr651_name`];
                let priceEntityType = 'cr651_pricelist';

                lookupPriceList = {
                    id: pricelistid,
                    name: pricelistName,
                    entityType: priceEntityType,
                };

                let currencyId = currencyResults.entities[0][`_transactioncurrencyid_value`];
                let currencyName = currencyResults.entities[0][`_transactioncurrencyid_value@OData.Community.Display.V1.FormattedValue`];
                let currencyEntityType = currencyResults.entities[0][`_transactioncurrencyid_value@Microsoft.Dynamics.CRM.lookuplogicalname`];

                lookupCurrency = {
                    id: currencyId,
                    name: currencyName,
                    entityType: currencyEntityType,
                };
                console.log(lookupCurrency);
            } else {
                console.log("No currency records found.");
            }
            console.log(currencyResults);
        } catch (error) {
            console.log(error);
        }

        try {
            let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
                    <entity name="cr651_products">
                        <attribute name="cr651_productsid"/>
                        <attribute name="cr651_name"/>
                        <attribute name="createdon"/>
                        <order attribute="cr651_name" descending="false"/>
                    </entity>
                </fetch>
            `;
            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);

            let productResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_products', encodedFetchXml);
            if (productResults.entities.length > 0) {
                for (let i = 0; i < productResults.entities.length; i++) {
                    let product = {
                        id: productResults.entities[i]['cr651_productsid'],
                        name: productResults.entities[i]['cr651_name'],
                        entityType: 'cr651_products'
                    };

                    let data = {
                        "cr651_name": product.name,
                        "cr651_fk_price_list@odata.bind": `/cr651_pricelists(${lookupPriceList.id})`,
                        "transactioncurrencyid@odata.bind": `/transactioncurrencies(${lookupCurrency.id})`,
                        "cr651_fk_product@odata.bind": `/cr651_productses(${product.id})`,
                        "cr651_mon_price_per_unit": 1
                    };

                    try {
                        let result = await Xrm.WebApi.createRecord("cr651_price_list_item", data);
                        console.log("Record created:", result);
                    } catch (error) {
                        console.log("Error creating record:", error);
                    }
                }
            } else {
                console.log("No product records found.");
            }
            console.log(productResults);
        } catch (error) {
            console.log(error);
        }
    }
}


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



async function onLoadCost(executionContext) {

    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_product').getValue();
    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;
        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }

        let cost;

        try {
            // Attempt to retrieve Price Per Unit from cr651_price_list_item
            let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
<entity name="cr651_products">
<attribute name="cr651_productsid"/>
<attribute name="cr651_name"/>
<attribute name="cr651_mon_cost"/>
<order attribute="cr651_name" descending="false"/>
<link-entity name="cr651_inventory_product" from="cr651_fk_product" to="cr651_productsid" link-type="inner" alias="ai">
<link-entity name="cr651_products" from="cr651_productsid" to="cr651_fk_product" link-type="inner" alias="aj">
<filter type="and">
<condition attribute="cr651_productsid" operator="eq"  value="${recordId}"/>
</filter>
</link-entity>
</link-entity>
</entity>
</fetch>
            `;

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let productResults = await Xrm.WebApi.retrieveMultipleRecords('cr651_products', encodedFetchXml);
            if (productResults.entities.length > 0) {
                cost = productResults.entities[0]["cr651_mon_cost"];
                console.log("cost ", cost);
            } else {
                throw new Error("No cost.");
            }
        } catch (error) {
            console.warn(error.message);
        }
        if (cost !== undefined) {
            let costField = formContext.getAttribute('cr651_mon_cost');
            costField.setValue(cost);
        }
    }
}

function checkCostChane(executionContext) {
    let formContext = executionContext.getFormContext();
    let cost = formContext.getAttribute('cr651_mon_cost').getValue();
    let pricePerUnit = formContext.getAttribute('cr651_mon_default_price_per_unit').getValue();
    console.log(cost, 'cost')
    console.log(pricePerUnit, 'pricePerUnit')
    let fieldSchemaName = "cr651_mon_cost"; // Replace this with the actual field schema name

    if (cost > pricePerUnit) {
        formContext.getControl(fieldSchemaName).setNotification("Warning cost > pricePerUnit", 2);
    } else {
        formContext.getControl(fieldSchemaName).clearNotification(2);
    }
}


function calculateTotalAmountService(executionContext) {
    let formContext = executionContext.getFormContext();
    let duration = formContext.getAttribute('cr651_int_duration').getValue();
    let pricePerUnit = formContext.getAttribute('cr651_mon_price_per_unit').getValue();
    let totalAmount = 0;
    console.log(duration, 'duration')
    console.log(pricePerUnit, 'pricePerUnit')
    if (duration !== null && pricePerUnit !== null) {
        totalAmount = (duration / 60) * pricePerUnit;
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


async function onSumTotalAmountProduct(formContext) {
    try {
        // Attempt to retrieve Price Per Unit from cr651_price_list_item
        let fetchXml = `
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true" aggregate="true">
<entity name="cr651_workorderproduct">
    <attribute name="cr651_mon_total_amount" aggregate="sum" alias="cr651_mon_total_amount_sum"/>
</entity>
</fetch>
            `;
        let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
        let result = await Xrm.WebApi.retrieveMultipleRecords('cr651_workorderproduct', encodedFetchXml);
        console.log(result,'result')
        const totalAmountSum = result.entities[0]['cr651_mon_total_amount_sum'];
        console.log(totalAmountSum, 'totalAmountSum')
        if (totalAmountSum != null) {
            formContext.getAttribute('cr651_mon_total_products_amount').setValue(totalAmountSum);

        }


    } catch (error) {
        console.warn(error.message);
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


async function defaultInventory(executionContext) {
    let formContext = executionContext.getFormContext();

    // Get the selected product and check if "Inventory" is blank
    let product = formContext.getAttribute("cr651_fk_product").getValue();
    let inventory = formContext.getAttribute("cr651_fk_inventory").getValue();

    if (!product || inventory) {
        // No product selected or inventory is already filled, exit the function
        return;
    }

    let productId = product[0].id; // Get product GUID
    console.log(product,'product')
    console.log(inventory,'inventory')
    console.log(productId,'productId')
    // Fetch maximum quantity of the product from Work Order Product entity
    let fetchXml = `
<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true" aggregate="true">
<entity name="cr651_workorderproduct">
<attribute name="cr651_int_quantity" aggregate="max" alias="cr651_int_quantity_max"/>
<attribute name="cr651_fk_inventory"  alias="cr651_fk_inventory_groupby" groupby="true" />
<link-entity name="cr651_products" from="cr651_productsid" to="cr651_fk_product" link-type="inner" alias="ae">
<filter type="and">
<condition attribute="cr651_productsid" operator="eq" uiname="Rotor DR450" uitype="cr651_products" value="{89CEFDA8-EF6E-EF11-A670-000D3AB0F99F}"/>
</filter>
</link-entity>
</entity>
</fetch>
    `;

    let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);

    try {
        let result = await Xrm.WebApi.retrieveMultipleRecords("cr651_workorderproduct", encodedFetchXml);
        console.log(result,'result')
        if (result.entities.length > 0) {
            let maxQuantity = 0;
            let maxInventoryId = null;
            let maxInventoryName = null;

            // Loop through the entities and find the one with the highest quantity
            result.entities.forEach((entity) => {
                let quantity = entity["cr651_int_quantity_max"];
                if (quantity > maxQuantity) {
                    maxQuantity = quantity;
                    maxInventoryId = entity["cr651_fk_inventory_groupby"];
                    maxInventoryName = entity["cr651_fk_inventory_groupby@OData.Community.Display.V1.FormattedValue"];
                }
            });

            // Set the Inventory field with the record having the maximum quantity
            if (maxInventoryId) {
                formContext.getAttribute("cr651_fk_inventory").setValue([{
                    id: maxInventoryId,
                    entityType: "cr651_inventory",
                    name: maxInventoryName
                }]);
            }
        }
    } catch (error) {
        console.error("Error fetching inventory records: ", error);
    }
}

