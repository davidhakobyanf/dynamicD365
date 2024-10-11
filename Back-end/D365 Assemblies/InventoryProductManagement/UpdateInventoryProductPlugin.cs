using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryProductManagement
{
    public class UpdateInventoryProductPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                tracingService.Trace("The plugin has started executing.");

                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity inventoryProduct = (Entity)context.InputParameters["Target"];
                    tracingService.Trace("The essence of the product has been obtained.");


                    EntityReference inventoryReference = null;
                    if (inventoryProduct.Contains("cr651_fk_inventory"))
                    {
                        inventoryReference = inventoryProduct.GetAttributeValue<EntityReference>("cr651_fk_inventory");
                        tracingService.Trace("cr651_fk_inventory found in Target.");
                    }
                    else if (context.PreEntityImages.Contains("preimage2") && context.PreEntityImages["preimage2"].Contains("cr651_fk_inventory"))
                    {
                        inventoryReference = context.PreEntityImages["preimage2"].GetAttributeValue<EntityReference>("cr651_fk_inventory");
                        tracingService.Trace("cr651_fk_inventory found in PreImage.");
                    }

                    if (inventoryReference != null && inventoryProduct.Contains("cr651_mon_total_amount"))
                    {
                        Guid inventoryId = inventoryReference.Id;
                        Money newProductTotalAmount = inventoryProduct.GetAttributeValue<Money>("cr651_mon_total_amount");
                        tracingService.Trace($"Inventory ID: {inventoryId}, New Product Total Amount: {newProductTotalAmount.Value}");


                        Entity inventory = service.Retrieve("cr651_inventory", inventoryId, new ColumnSet("cr651_mon_total_amount"));
                        Money currentInventoryTotalAmount = inventory.Contains("cr651_mon_total_amount")
                            ? inventory.GetAttributeValue<Money>("cr651_mon_total_amount")
                            : new Money(0);

                        tracingService.Trace($"Current Inventory Total Amount: {currentInventoryTotalAmount.Value}");


                        Money oldProductTotalAmount = null;
                        if (context.PreEntityImages.Contains("preimage2"))
                        {
                            Entity preImage = (Entity)context.PreEntityImages["preimage2"];
                            oldProductTotalAmount = preImage.Contains("cr651_mon_total_amount")
                                ? preImage.GetAttributeValue<Money>("cr651_mon_total_amount")
                                : new Money(0);
                            tracingService.Trace($"Old Product Total Amount: {oldProductTotalAmount.Value}");
                        }


                        if (oldProductTotalAmount != null)
                        {
                            currentInventoryTotalAmount = new Money(currentInventoryTotalAmount.Value - oldProductTotalAmount.Value + newProductTotalAmount.Value);
                        }
                        else
                        {

                            currentInventoryTotalAmount = new Money(currentInventoryTotalAmount.Value + newProductTotalAmount.Value);
                        }

                        tracingService.Trace($"Final Updated Inventory Total Amount: {currentInventoryTotalAmount.Value}");


                        inventory["cr651_mon_total_amount"] = currentInventoryTotalAmount;
                        service.Update(inventory);

                        tracingService.Trace("Inventory updated successfully.");
                    }
                    else
                    {
                        tracingService.Trace("Could not find required fields 'cr651_mon_total_amount' or 'cr651_fk_inventory' in Target or PreImage.");
                    }
                }
                else
                {
                    tracingService.Trace("Target not found in InputParameters.");
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
