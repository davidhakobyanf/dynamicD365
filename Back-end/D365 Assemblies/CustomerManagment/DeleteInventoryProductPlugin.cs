using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System;

public class DeleteInventoryProductPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        tracingService.Trace("Plugin execution started (Pre-Operation).");

   
        if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
        {
            tracingService.Trace("Target entity reference is present.");

      
            if (context.PreEntityImages.Contains("preimage1") && context.PreEntityImages["preimage1"] is Entity inventoryProductPreImage)
            {
                tracingService.Trace("Pre-image 'preimage1' found.");

                try
                {
                 
                    if (inventoryProductPreImage.Contains("cr651_fk_inventory") && inventoryProductPreImage.Contains("cr651_mon_total_amount"))
                    {
                        tracingService.Trace("Required fields found in pre-image.");

                  
                        Guid inventoryId = inventoryProductPreImage.GetAttributeValue<EntityReference>("cr651_fk_inventory").Id;
                        Money productTotalAmount = inventoryProductPreImage.GetAttributeValue<Money>("cr651_mon_total_amount");

              
                        Entity inventory = service.Retrieve("cr651_inventory", inventoryId, new ColumnSet("cr651_mon_total_amount"));
                        tracingService.Trace("Inventory retrieved. ID: " + inventoryId);

                        Money currentInventoryTotalAmount = inventory.GetAttributeValue<Money>("cr651_mon_total_amount");
                        tracingService.Trace("Current inventory total amount: " + currentInventoryTotalAmount.Value);

                   
                        Money updatedTotalAmount = new Money(currentInventoryTotalAmount.Value - productTotalAmount.Value);
                        inventory["cr651_mon_total_amount"] = updatedTotalAmount;

             
                        service.Update(inventory);
                        tracingService.Trace("Inventory updated with new total amount: " + updatedTotalAmount.Value);
                    }
                    else
                    {
                        tracingService.Trace("Required fields not found in pre-image.");
                    }
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace("Error: " + ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Unexpected error: " + ex.Message);
                    throw;
                }
            }
            else
            {
                tracingService.Trace("Pre-image 'preimage1' not found.");
            }
        }
        else
        {
            tracingService.Trace("Target entity reference is not present.");
        }

        tracingService.Trace("Plugin execution completed (Pre-Operation).");
    }
}
