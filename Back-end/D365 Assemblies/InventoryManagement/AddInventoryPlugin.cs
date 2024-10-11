using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement
{
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

                    
                    if (inventory.Contains("cr651_fk_price_list"))
                    {
                        EntityReference priceListRef = inventory.GetAttributeValue<EntityReference>("cr651_fk_price_list");
                        tracingService.Trace($"Price list found: {priceListRef.Id}");

                        
                        Entity priceList = service.Retrieve("cr651_pricelist", priceListRef.Id, new ColumnSet("transactioncurrencyid"));
                        EntityReference priceListCurrencyRef = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");
                        tracingService.Trace($"Currency found in price list: {priceListCurrencyRef.Id}");

                       
                        inventory["transactioncurrencyid"] = priceListCurrencyRef;
                        tracingService.Trace("Inventory currency set from price list.");

                        
                        if (inventory.Contains("cr651_mon_total_amount"))
                        {
                            Money inventoryTotalAmount = inventory.GetAttributeValue<Money>("cr651_mon_total_amount");
                            tracingService.Trace($"Inventory total amount: {inventoryTotalAmount.Value}");
                        }
                        else
                        {
                            
                            inventory["cr651_mon_total_amount"] = new Money(0);
                            tracingService.Trace("Inventory total amount set to 0.");
                        }

                        
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
}
