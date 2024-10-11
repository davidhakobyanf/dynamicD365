function testFunction(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_product').getValue();
    if (lookupValue != null && lookupValue[0] != null) {
        let productName = lookupValue[0].name;
        formContext.getAttribute('cr651_name').setValue(productName);
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
                let transactionCurrencyId  = [
                    {
                        id: inventoryResults.entities[0][`_transactioncurrencyid_value`],
                        name:inventoryResults.entities[0][`_transactioncurrencyid_value@OData.Community.Display.V1.FormattedValue`],
                        entityType:inventoryResults.entities[0][`_transactioncurrencyid_value@Microsoft.Dynamics.CRM.lookuplogicalname`],
                    }
                ];

                formContext.getAttribute('transactioncurrencyid').setValue(transactionCurrencyId);
            } else {
                alert("Currency ID not found in any Price List.");
            }
        }
        catch
            (error)
        {
            alert("Error retrieving records: " + error.message);
            console.error(error);
        }
    }
}

