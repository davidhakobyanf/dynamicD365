function openInventoryProductPopup(formContext) {

    let inventoryId = formContext.data.entity.getId()

    var pageInput = {
        pageType: "webresource",
        webresourceName: "cr651_html_inventory_products_popup",
        data:JSON.stringify({"inventoryId": inventoryId})
    };
    var navigationOptions = {
        target: 2,
        width: 400, // value specified in pixel
        height: 400, // value specified in pixel
        position: 1,
        title: "Number of Clones"
    };
    console.log(inventoryId,'inventoryId')
    Xrm.Navigation.navigateTo(pageInput,navigationOptions).then(
        function  success(){
            // console.log("success")
        },
        function error(){
            // console.log("error")
        }
    )
}
function openInventoryProductPopupCheckbox(executionContext) {
    let formContext = executionContext.getFormContext();
    let inventoryId = formContext.data.entity.getId()

    var pageInput = {
        pageType: "webresource",
        webresourceName: "cr651_html_inventory_products_popup",
        data:JSON.stringify({"inventoryId": inventoryId})
    };
    var navigationOptions = {
        target: 2,
        width: 400, // value specified in pixel
        height: 400, // value specified in pixel
        position: 1,
        title: "Number of Clones"
    };
    console.log(inventoryId,'inventoryId')
    Xrm.Navigation.navigateTo(pageInput,navigationOptions).then(
        function  success(){
            // console.log("success")
        },
        function error(){
            // console.log("error")
        }
    )
}