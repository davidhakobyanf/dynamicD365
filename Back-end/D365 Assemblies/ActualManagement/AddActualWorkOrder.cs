using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;

namespace ActualManagement
{
    public class AddActualWorkOrder : CodeActivity
    {
        [Output("Status")]
        public OutArgument<bool> Status { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            
            Status.Set(context, false);
            ITracingService tracingService = context.GetExtension<ITracingService>();
            tracingService.Trace("Execution started.");

            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            IExecutionContext crmContext = context.GetExtension<IExecutionContext>();

            
            tracingService.Trace("Attempting to retrieve Work Order reference.");
            EntityReference workOrderRef = (EntityReference)crmContext.InputParameters["Target"];
            if (workOrderRef == null)
            {
                tracingService.Trace("Work Order reference is null.");
                throw new InvalidPluginExecutionException("Work Order reference is null.");
            }
            tracingService.Trace($"Work Order reference retrieved: {workOrderRef.Id}");

            
            tracingService.Trace("Deleting existing Actual records associated with the Work Order.");
            var fetchXml = $@"
                <fetch>
                    <entity name='cr651_actual'>
                        <attribute name='cr651_actualid' />
                        <filter>
                            <condition attribute='cr651_fk_work_order' operator='eq' value='{workOrderRef.Id}' />
                        </filter>
                    </entity>
                </fetch>";

            EntityCollection actualRecords = service.RetrieveMultiple(new FetchExpression(fetchXml));

            foreach (var actual in actualRecords.Entities)
            {
                service.Delete(actual.LogicalName, actual.Id);
                tracingService.Trace($"Deleted Actual record with ID: {actual.Id}");
            }

            tracingService.Trace("Deleted existing Actual records successfully.");

            
            tracingService.Trace("Retrieving Work Order from CRM.");
            Entity workOrder = service.Retrieve("cr651_work_order", workOrderRef.Id, new ColumnSet("cr651_name"));
            if (workOrder == null)
            {
                tracingService.Trace("Work Order not found.");
                throw new InvalidPluginExecutionException("Work Order not found.");
            }
            tracingService.Trace("Work Order retrieved successfully.");

            
            tracingService.Trace("Querying Work Order Products.");
            var queryProducts = new QueryExpression("cr651_workorderproduct")
            {
                ColumnSet = new ColumnSet("cr651_fk_product", "cr651_int_quantity", "cr651_mon_price_per_unit", "cr651_mon_cost")
            };
            queryProducts.Criteria.AddCondition("cr651_fk_work_order", ConditionOperator.Equal, workOrderRef.Id);

            var workOrderProducts = service.RetrieveMultiple(queryProducts).Entities;
            tracingService.Trace($"Number of Work Order Products retrieved: {workOrderProducts.Count}");

            
            tracingService.Trace("Starting creation of Actual records for products.");
            foreach (var product in workOrderProducts)
            {
                EntityReference productRef = product.GetAttributeValue<EntityReference>("cr651_fk_product");
                if (productRef != null)
                {
                    tracingService.Trace($"Processing Product: {productRef.Name}");

                    int? quantityInt = product.GetAttributeValue<int?>("cr651_int_quantity");
                    decimal quantity = (decimal)(quantityInt ?? 0);
                    tracingService.Trace($"Quantity retrieved: {quantity}");

                    Money costPerUnit = product.GetAttributeValue<Money>("cr651_mon_cost") ?? new Money(0);
                    tracingService.Trace($"Cost per unit retrieved: {costPerUnit.Value}");
                    Money totalCost = new Money(quantity * costPerUnit.Value);
                    tracingService.Trace($"Cost per unit: {costPerUnit.Value}, Total cost: {totalCost.Value}");

                  
                    Entity actualProduct = new Entity("cr651_actual");
                    actualProduct["cr651_name"] = "Product: " + (productRef.Name ?? "Unknown Product");
                    actualProduct["cr651_mon_cost_per_unit"] = costPerUnit;
                    actualProduct["cr651_dec_quantity"] = quantity;
                    actualProduct["cr651_mon_total_cost"] = totalCost;
                    actualProduct["cr651_fk_work_order"] = workOrderRef; 

                    tracingService.Trace("Creating Actual Product record.");
                    Guid actualProductId = service.Create(actualProduct);
                    tracingService.Trace($"Actual Product created with ID: {actualProductId}");
                }
            }

            
            tracingService.Trace("Querying Work Order Services.");
            var queryServices = new QueryExpression("cr651_workorderservices")
            {
                ColumnSet = new ColumnSet("cr651_fk_service", "cr651_fk_resource", "cr651_int_duration")
            };
            queryServices.Criteria.AddCondition("cr651_fk_work_order", ConditionOperator.Equal, workOrderRef.Id);

            var workOrderServices = service.RetrieveMultiple(queryServices).Entities;
            tracingService.Trace($"Number of Work Order Services retrieved: {workOrderServices.Count}");

        
            tracingService.Trace("Starting creation of Actual records for services.");
            foreach (var serviceItem in workOrderServices)
            {
                EntityReference serviceRef = serviceItem.GetAttributeValue<EntityReference>("cr651_fk_service");
                EntityReference resourceRef = serviceItem.GetAttributeValue<EntityReference>("cr651_fk_resource"); 
                if (serviceRef != null)
                {
                    tracingService.Trace($"Processing Service: {serviceRef.Name}");

                    int? durationInMinutes = serviceItem.GetAttributeValue<int?>("cr651_int_duration");
                    int durationInHours = durationInMinutes.HasValue ? (int)Math.Floor((decimal)durationInMinutes.Value / 60) : 0;
                    decimal quantity = (decimal)durationInHours; 
                    tracingService.Trace($"Duration in hours: {durationInHours}, Quantity: {quantity}");

                    
                    Money costPerUnit = new Money(0);
                    if (resourceRef != null)
                    {
                        tracingService.Trace($"Retrieving resource for hourly rate: {resourceRef.Id}");
                        Entity resource = service.Retrieve("cr651_resource", resourceRef.Id, new ColumnSet("cr651_mon_hourly_rate"));
                        costPerUnit = resource.GetAttributeValue<Money>("cr651_mon_hourly_rate") ?? new Money(0);
                        tracingService.Trace($"Hourly rate retrieved: {costPerUnit.Value}");
                    }

                    Money totalCost = new Money(durationInHours * costPerUnit.Value);
                    tracingService.Trace($"Cost per unit: {costPerUnit.Value}, Total cost: {totalCost.Value}");

                    
                    Entity actualService = new Entity("cr651_actual");
                    actualService["cr651_name"] = "Service: " + (serviceRef.Name ?? "Unknown Service");
                    actualService["cr651_mon_cost_per_unit"] = costPerUnit; 
                    actualService["cr651_dec_quantity"] = quantity;
                    actualService["cr651_mon_total_cost"] = totalCost;
                    actualService["cr651_fk_work_order"] = workOrderRef; 

                    tracingService.Trace("Creating Actual Service record.");
                    Guid actualServiceId = service.Create(actualService);
                    tracingService.Trace($"Actual Service created with ID: {actualServiceId}");
                }
            }

            
            Status.Set(context, true);
            tracingService.Trace("Execution completed successfully.");
        }
    }
}
