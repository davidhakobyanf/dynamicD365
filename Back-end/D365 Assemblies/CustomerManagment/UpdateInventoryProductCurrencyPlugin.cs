using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;

public class UpdateInventoryProductCurrencyPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        tracingService.Trace("UpdateInventoryProductCurrencyPlugin: Start execution.");

        if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity inventory)
        {
            tracingService.Trace($"UpdateInventoryProductCurrencyPlugin: Processing Inventory with ID {inventory.Id}");

        
            if (context.PreEntityImages.Contains("preimage20"))
            {
                Entity preimage20 = context.PreEntityImages["preimage20"];

              
                EntityReference oldCurrencyRef = preimage20.GetAttributeValue<EntityReference>("transactioncurrencyid");
                EntityReference newCurrencyRef = inventory.GetAttributeValue<EntityReference>("transactioncurrencyid");

                if (newCurrencyRef != null && (oldCurrencyRef == null || oldCurrencyRef.Id != newCurrencyRef.Id))
                {
                    tracingService.Trace($"UpdateInventoryProductCurrencyPlugin: Currency changed from {oldCurrencyRef?.Id} to {newCurrencyRef.Id}");

                  
                    QueryExpression inventoryProductsQuery = new QueryExpression
                    {
                        EntityName = "cr651_inventory_product",
                        ColumnSet = new ColumnSet("transactioncurrencyid", "cr651_fk_inventory"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                        {
                            new ConditionExpression("cr651_fk_inventory", ConditionOperator.Equal, inventory.Id)
                        }
                        }
                    };

                    EntityCollection inventoryProducts = service.RetrieveMultiple(inventoryProductsQuery);
                    foreach (Entity inventoryProduct in inventoryProducts.Entities)
                    {
                        inventoryProduct["transactioncurrencyid"] = newCurrencyRef;
                        service.Update(inventoryProduct);
                        tracingService.Trace($"Updated InventoryProduct ID {inventoryProduct.Id} with new currency {newCurrencyRef.Id}.");
                    }
                }
                else
                {
                    tracingService.Trace("UpdateInventoryProductCurrencyPlugin: Currency has not changed, no action taken.");
                }
            }
            else
            {
                tracingService.Trace("UpdateInventoryProductCurrencyPlugin: No Pre-Image preimage20 found.");
            }
        }

        tracingService.Trace("UpdateInventoryProductCurrencyPlugin: End execution.");
    }



}
