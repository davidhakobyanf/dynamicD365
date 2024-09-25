using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;

public class AddInventoryPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        try
        {
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity inventory = (Entity)context.InputParameters["Target"];

                // Check if inventory has a price list lookup
                if (inventory.Contains("cr651_fk_price_list"))
                {
                    EntityReference priceListRef = inventory.GetAttributeValue<EntityReference>("cr651_fk_price_list");
                    tracingService.Trace($"Price list found: {priceListRef.Id}");

                    // Retrieve price list to check currency
                    Entity priceList = service.Retrieve("cr651_pricelist", priceListRef.Id, new ColumnSet("transactioncurrencyid"));
                    EntityReference priceListCurrencyRef = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");
                    tracingService.Trace($"Currency found in price list: {priceListCurrencyRef.Id}");

                    // Set price list currency to inventory if not already set
                    inventory["transactioncurrencyid"] = priceListCurrencyRef;
                    tracingService.Trace("Inventory currency set from price list.");

                    // Check for total amount in inventory and set to 0 if not present
                    if (inventory.Contains("cr651_mon_total_amount"))
                    {
                        Money inventoryTotalAmount = inventory.GetAttributeValue<Money>("cr651_mon_total_amount");
                        tracingService.Trace($"Inventory total amount: {inventoryTotalAmount.Value}");
                    }
                    else
                    {
                        // Set total amount to 0 if it's missing
                        inventory["cr651_mon_total_amount"] = new Money(0);
                        tracingService.Trace("Inventory total amount set to 0.");
                    }

                    // No need to call service.Update() in pre-operation plugins
                }
                else
                {
                    throw new InvalidPluginExecutionException("Inventory does not have a price list set.");
                }
            }
        }
        catch (Exception ex)
        {
            tracingService.Trace($"Error: {ex.Message}");
            throw;
        }
    }
}
