let contactLookupPointer = null;




async function getContactAccount(executionContext) {
    let formContext = executionContext.getFormContext();
    let lookupValue = formContext.getAttribute('cr651_fk_customer').getValue();
    if (contactLookupPointer != null) {
        formContext.getControl("cr651_fk_contact").removePreSearch(contactLookupPointer);
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
<entity name="cr651_contact">
<attribute name="cr651_contactid"/>
<attribute name="cr651_name"/>
<attribute name="createdon"/>
<order attribute="cr651_name" descending="false"/>
<link-entity name="cr651_position" from="cr651_fk_contact" to="cr651_contactid" link-type="inner" alias="aa">
<filter type="and">
<condition attribute="cr651_fk_account" operator="eq" value="${recordId}"/>
</filter>
</link-entity>
</entity>
</fetch>
               `

            let encodedFetchXml = "?fetchXml=" + encodeURIComponent(fetchXml);
            let fetchResult = await Xrm.WebApi.retrieveMultipleRecords('cr651_contact', encodedFetchXml);
            let contactResult = fetchResult.entities;
                console.log(fetchResult, 'Contact Result3');
                contactLookupPointer = filterContact.bind({ 'contactResult': contactResult});
            formContext.getControl("cr651_fk_contact").addPreSearch(contactLookupPointer);
            console.log(contactResult,'contactResult.entities')

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
    let contactEntities = this.contactResult;
    let contactFilter = `<filter type="or">`;
    if (contactEntities.length > 0) {
        contactEntities.forEach(item => {
            contactFilter += `
                <condition attribute="cr651_contactid" operator="eq" value="${item['cr651_contactid']}" />
            `;
        });
    } else {
        contactFilter += `
            <condition attribute="cr651_contactid" operator="eq" value="{00000000-0000-0000-0000-000000000000}" />
        `;}

    contactFilter += `</filter>`;
    console.log(contactEntities,'contactEntities')
    formContext.getControl("cr651_fk_contact").addCustomFilter(contactFilter, "cr651_contact");
}












