using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement
{
    public class UpdateInventoryCurrencyPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity inventory)
                {
                    if (inventory.Contains("cr651_fk_price_list"))
                    {
                        EntityReference priceListRef = inventory.GetAttributeValue<EntityReference>("cr651_fk_price_list");
                        tracingService.Trace($"Price list found: {priceListRef.Id}");

                        
                        Entity priceList = service.Retrieve("cr651_pricelist", priceListRef.Id, new ColumnSet("transactioncurrencyid"));
                        if (priceList.Contains("transactioncurrencyid"))
                        {
                            EntityReference priceListCurrencyRef = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");
                            tracingService.Trace($"Currency found in price list: {priceListCurrencyRef.Id}");

                            
                            if (!inventory.Contains("transactioncurrencyid") || ((EntityReference)inventory["transactioncurrencyid"]).Id != priceListCurrencyRef.Id)
                            {
                                inventory["transactioncurrencyid"] = priceListCurrencyRef;
                                tracingService.Trace("Inventory currency set from price list.");
                            }
                        }
                        else
                        {
                            tracingService.Trace("No currency found in price list.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("No price list found in inventory.");
                    }
                }
                else
                {
                    tracingService.Trace("Target entity is not an Inventory.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Exception: {ex.Message}");
                throw;
            }

            tracingService.Trace("UpdateInventoryProductCurrencyPlugin: End execution.");
        }

    }
}
