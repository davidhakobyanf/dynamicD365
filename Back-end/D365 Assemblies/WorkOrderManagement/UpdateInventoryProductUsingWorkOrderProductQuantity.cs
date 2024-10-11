using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;

namespace WorkOrderManagement
{
    public class UpdateInventoryProductUsingWorkOrderProductQuantity : IPlugin
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
                    Entity workOrder = (Entity)context.InputParameters["Target"];

                    if (workOrder.LogicalName == "cr651_work_order" &&
                        workOrder.Contains("cr651_os_status") &&
                        ((OptionSetValue)workOrder["cr651_os_status"]).Value == 523250001)
                    {
                        Guid workOrderId = workOrder.Id;

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

                        foreach (Entity workOrderProduct in workOrderProducts.Entities)
                        {
                            Guid fkInventory = workOrderProduct.GetAttributeValue<EntityReference>("cr651_fk_inventory").Id;
                            Guid fkProduct = workOrderProduct.GetAttributeValue<EntityReference>("cr651_fk_product").Id;
                            int workOrderQuantity = workOrderProduct.GetAttributeValue<int>("cr651_int_quantity");

                            QueryExpression inventoryProductQuery = new QueryExpression("cr651_inventory_product")
                            {
                                ColumnSet = new ColumnSet("cr651_int_quantity", "cr651_mon_price_per_unit"),
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

                            if (inventoryProducts.Entities.Count == 0)
                            {
                                throw new InvalidPluginExecutionException($"No matching InventoryProduct found for Inventory ID {fkInventory} and Product ID {fkProduct}.");
                            }

                            foreach (Entity inventoryProduct in inventoryProducts.Entities)
                            {
                                int inventoryQuantity = inventoryProduct.GetAttributeValue<int>("cr651_int_quantity");
                                Money inventoryPricePerUnit = inventoryProduct.GetAttributeValue<Money>("cr651_mon_price_per_unit");

                                if (inventoryQuantity >= workOrderQuantity)
                                {
                                    inventoryProduct["cr651_int_quantity"] = inventoryQuantity - workOrderQuantity;
                                    inventoryProduct["cr651_mon_total_amount"] = new Money((inventoryQuantity - workOrderQuantity) * inventoryPricePerUnit.Value);

                                    service.Update(inventoryProduct);
                                }
                                else
                                {
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
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Plugin Error: {ex.Message}");
            }
        }
    }
}
