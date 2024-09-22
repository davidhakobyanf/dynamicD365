let inventoryLookupPointer = null;




async function getProductInventory(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_inventory').getValue();
    if (inventoryLookupPointer != null) {
        formContext.getControl("cr651_fk_product").removePreSearch(inventoryLookupPointer);
    }
    if (lookupValue && lookupValue[0]) {
        let recordId = lookupValue[0].id;

        if (!recordId) {
            console.log("This is a new record, the script will not run.");
            return;
        }

        try {
            let fetchXml = `
               <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
<entity name="cr651_products">
<attribute name="cr651_productsid"/>
<attribute name="cr651_name"/>
<attribute name="createdon"/>
<order attribute="cr651_name" descending="false"/>
<link-entity name="cr651_inventory_product" from="cr651_fk_product" to="cr651_productsid" link-type="inner" alias="aa">
<filter type="and">
<condition attribute="cr651_fk_inventory" operator="eq" value="${recordId}"/>
</filter>
</link-entity>
</entity>
</fetch>

               `

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let fetchResult = await Xrm.WebApi.retrieveMultipleRecords('cr651_products', encodedFetchXml);
            let productResult = fetchResult.entities;
            console.log(fetchResult, 'Contact Result3');
            inventoryLookupPointer = filterContact.bind({ 'productResult': productResult});
            formContext.getControl("cr651_fk_product").addPreSearch(inventoryLookupPointer);
            console.log(productResult,'productResult.entities')

        }
        catch
            (error)
        {
            alert("Error retrieving records: " + error.message);
            console.error(error);
        }
    }
}


function filterContact(executionContext) {
    console.log('barev')
    let formContext = executionContext.getFormContext();
    let productEntities = this.productResult;
    let productFilter = `<filter type="or">`;
    if (productEntities.length > 0) {
        productEntities.forEach(item => {
            productFilter += `
                <condition attribute="cr651_productsid" operator="eq" value="${item['cr651_productsid']}" />
            `;
        });
    } else {
        productFilter += `
            <condition attribute="cr651_productsid" operator="eq" value="{00000000-0000-0000-0000-000000000000}" />
        `;}

    productFilter += `</filter>`;
    console.log(productEntities,'productEntities')
    formContext.getControl("cr651_fk_product").addCustomFilter(productFilter, "cr651_products");

}