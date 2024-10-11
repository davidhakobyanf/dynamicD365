using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;

namespace WorkOrderManagement
{
    public class UpdateInvoiceNumberNameInv : CodeActivity
    {
        
        [Input("Work Order")]
        [ReferenceTarget("cr651_work_order")]
        public InArgument<EntityReference> WorkOrder { get; set; }

       
        public OutArgument<string> Status { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                
                var workOrderReference = WorkOrder.Get(executionContext);

                if (workOrderReference == null)
                {
                    throw new InvalidPluginExecutionException("Work Order reference is null.");
                }

                
                Entity workOrder = service.Retrieve(workOrderReference.LogicalName, workOrderReference.Id, new ColumnSet("cr651_name"));
                if (workOrder == null || !workOrder.Contains("cr651_name"))
                {
                    throw new InvalidPluginExecutionException("Work Order or Work Order Name not found.");
                }

                string workOrderName = workOrder.GetAttributeValue<string>("cr651_name");

                
                QueryExpression query = new QueryExpression("cr651_invoice")
                {
                    ColumnSet = new ColumnSet("cr651_name"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("cr651_fk_work_order", ConditionOperator.Equal, workOrderReference.Id)
                        }
                    }
                };

                EntityCollection invoices = service.RetrieveMultiple(query);

                if (invoices.Entities.Count == 0)
                {
                    throw new InvalidPluginExecutionException("No associated invoices found for the work order.");
                }

                
                foreach (var invoice in invoices.Entities)
                {
                    invoice["cr651_name"] = "INV-" + workOrderName;
                    service.Update(invoice);
                }

                
                Status.Set(executionContext, "Success");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
