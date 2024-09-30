using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace CustomerManagment
{
    public class UpdateInventoryProductUsingWorkOrderProductQuantity : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get execution context
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                // Check if target is available and of type Entity
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity workOrder = (Entity)context.InputParameters["Target"];

                    // Ensure we are working with the cr651_work_order entity and status change is to 523250001
                    if (workOrder.LogicalName == "cr651_work_order" &&
                        workOrder.Contains("cr651_os_status") &&
                        ((OptionSetValue)workOrder["cr651_os_status"]).Value == 523250001)
                    {
                        Guid workOrderId = workOrder.Id;

                        // Query related cr651_workorderproduct records for the specific work order
                        QueryExpression workOrderProductQuery = new QueryExpression("cr651_workorderproduct")
                        {
                            ColumnSet = new ColumnSet("cr651_fk_inventory", "cr651_fk_product", "cr651_int_quantity"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("cr651_fk_work_order", ConditionOperator.Equal, workOrderId)
                                }
                            }
                        };

                        EntityCollection workOrderProducts = service.RetrieveMultiple(workOrderProductQuery);

                        // Process each cr651_workorderproduct
                        foreach (Entity workOrderProduct in workOrderProducts.Entities)
                        {
                            Guid fkInventory = workOrderProduct.GetAttributeValue<EntityReference>("cr651_fk_inventory").Id;
                            Guid fkProduct = workOrderProduct.GetAttributeValue<EntityReference>("cr651_fk_product").Id;
                            int workOrderQuantity = workOrderProduct.GetAttributeValue<int>("cr651_int_quantity");

                            // Query related cr651_inventory_product with matching fk_inventory and fk_product
                            QueryExpression inventoryProductQuery = new QueryExpression("cr651_inventory_product")
                            {
                                ColumnSet = new ColumnSet("cr651_int_quantity"),
                                Criteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("cr651_fk_inventory", ConditionOperator.Equal, fkInventory),
                                        new ConditionExpression("cr651_fk_product", ConditionOperator.Equal, fkProduct)
                                    }
                                }
                            };

                            EntityCollection inventoryProducts = service.RetrieveMultiple(inventoryProductQuery);

                            // If no inventory products found, throw an error
                            if (inventoryProducts.Entities.Count == 0)
                            {
                                throw new InvalidPluginExecutionException($"No matching InventoryProduct found for Inventory ID {fkInventory} and Product ID {fkProduct}.");
                            }

                            // Process each cr651_inventory_product
                            foreach (Entity inventoryProduct in inventoryProducts.Entities)
                            {
                                int inventoryQuantity = inventoryProduct.GetAttributeValue<int>("cr651_int_quantity");

                                // Check if inventory quantity is sufficient for work order product quantity
                                if (inventoryQuantity >= workOrderQuantity)
                                {
                                    // Adjust inventory quantity and update each InventoryProduct in service directly
                                    inventoryProduct["cr651_int_quantity"] = inventoryQuantity - workOrderQuantity;

                                    // Update each InventoryProduct individually using the service
                                    service.Update(inventoryProduct);
                                }
                                else
                                {
                                    // Throw an error if quantity is insufficient, with specific details
                                    throw new InvalidPluginExecutionException(
                                        $"Insufficient InventoryProduct quantity for Inventory (ID: {fkInventory}) and Product (ID: {fkProduct}). " +
                                        $"Available: {inventoryQuantity}, Required: {workOrderQuantity}.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Trace and throw error
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Plugin Error: {ex.Message}");
            }
        }
    }
}
