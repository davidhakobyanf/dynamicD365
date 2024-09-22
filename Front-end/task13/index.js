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
