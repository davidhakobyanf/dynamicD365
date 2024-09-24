using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;

public class AddInventoryProductPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
        {
            Entity inventoryProduct = (Entity)context.InputParameters["Target"];


            if (inventoryProduct.Contains("cr651_mon_total_amount"))
            {
                Money productTotalAmount = inventoryProduct.GetAttributeValue<Money>("cr651_mon_total_amount");

           
                if (inventoryProduct.Contains("cr651_fk_inventory"))
                {
                    Guid inventoryId = inventoryProduct.GetAttributeValue<EntityReference>("cr651_fk_inventory").Id;

                    Entity inventory = service.Retrieve("cr651_inventory", inventoryId, new ColumnSet("cr651_mon_total_amount"));

                    Money currentInventoryTotalAmount = inventory.Contains("cr651_mon_total_amount")
                        ? inventory.GetAttributeValue<Money>("cr651_mon_total_amount")
                        : new Money(0);

                   
                    Money updatedTotalAmount = new Money(currentInventoryTotalAmount.Value + productTotalAmount.Value);

                 
                    inventory["cr651_mon_total_amount"] = updatedTotalAmount;
                    service.Update(inventory);
                }
            }
        }
    }
}
