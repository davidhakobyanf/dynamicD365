using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;

namespace WorkOrderManagement
{
    public class DeleteInvoiceWorkOrder : CodeActivity
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
                    throw new InvalidPluginExecutionException("Work order reference is null.");
                }

                var workOrderId = workOrderReference.Id;

                tracingService.Trace($"Work order ID: {workOrderId}");

                
                QueryExpression query = new QueryExpression("cr651_invoice")
                {
                    ColumnSet = new ColumnSet("cr651_invoiceid", "cr651_name", "createdon"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("cr651_fk_work_order", ConditionOperator.Equal, workOrderId)
                        }
                    }
                };
                query.AddOrder("cr651_name", OrderType.Ascending);

                
                var invoices = service.RetrieveMultiple(query).Entities;

                tracingService.Trace($"Found {invoices.Count} invoices to delete.");

                
                foreach (var invoice in invoices)
                {
                    service.Delete("cr651_invoice", invoice.Id);
                    tracingService.Trace($"Deleted invoice with ID: {invoice.Id}");
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
