using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;

namespace WorkOrderManagement
{
    public class AddInvoiceAndInvoiceProductWorkOrder : CodeActivity
    {
        [Input("Work Order")]
        [ReferenceTarget("cr651_work_order")]
        public InArgument<EntityReference> WorkOrderReference { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            
            ITracingService tracingService = context.GetExtension<ITracingService>();
            tracingService.Trace("Execution started.");

            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            
            EntityReference workOrderRef = WorkOrderReference.Get(context);
            if (workOrderRef == null)
            {
                tracingService.Trace("Work Order reference is null.");
                throw new InvalidPluginExecutionException("Work Order reference is null.");
            }
            tracingService.Trace($"Work Order reference retrieved: {workOrderRef.Id}");

            
            Entity workOrder = service.Retrieve("cr651_work_order", workOrderRef.Id, new ColumnSet("cr651_name", "cr651_fk_customer", "cr651_fk_price_list", "cr651_fk_customer_asset", "cr651_fk_contact", "cr651_os_status"));

            if (workOrder == null)
            {
                tracingService.Trace("Work Order not found.");
                throw new InvalidPluginExecutionException("Work Order not found.");
            }

            tracingService.Trace("Work Order retrieved successfully.");

            
            Entity invoice = new Entity("cr651_invoice");
            invoice["cr651_name"] = "Invoice for Work Order: " + (workOrder.Contains("cr651_name") ? workOrder.GetAttributeValue<string>("cr651_name") : string.Empty);
            invoice["cr651_fk_customer"] = workOrder.GetAttributeValue<EntityReference>("cr651_fk_customer");
            invoice["cr651_fk_contact"] = workOrder.GetAttributeValue<EntityReference>("cr651_fk_contact");
            invoice["cr651_fk_pricelist"] = workOrder.GetAttributeValue<EntityReference>("cr651_fk_price_list");
            invoice["cr651_fk_work_order"] = workOrder.ToEntityReference();

            Guid invoiceId = service.Create(invoice);
            tracingService.Trace($"Invoice created with ID: {invoiceId}");

            
            var query = new QueryExpression("cr651_workorderproduct");
            query.ColumnSet = new ColumnSet("cr651_workorderproductid", "cr651_name", "cr651_fk_work_order", "cr651_mon_total_amount", "cr651_int_quantity", "cr651_fk_product", "cr651_mon_price_per_unit", "cr651_fk_inventory", "transactioncurrencyid", "cr651_mon_cost");
            query.Criteria.AddCondition("cr651_fk_work_order", ConditionOperator.Equal, workOrderRef.Id);

            var workOrderProducts = service.RetrieveMultiple(query).Entities;

            tracingService.Trace($"Number of Work Order Products retrieved: {workOrderProducts.Count}");

            foreach (var product in workOrderProducts)
            {
                EntityReference productRef = product.GetAttributeValue<EntityReference>("cr651_fk_product");

                if (productRef != null)
                {
                    Entity invoiceProduct = new Entity("cr651_invoice_product");
                    invoiceProduct["cr651_name"] = "Product: " + (productRef.Name ?? "Unknown Product");
                    invoiceProduct["cr651_fk_invoice"] = new EntityReference("cr651_invoice", invoiceId);
                    invoiceProduct["cr651_fk_product"] = productRef;
                    invoiceProduct["cr651_int_quantity"] = product.GetAttributeValue<int?>("cr651_int_quantity") ?? 0;
                    invoiceProduct["cr651_mon_price_per_unit"] = product.GetAttributeValue<Money>("cr651_mon_price_per_unit") ?? new Money(0);
                    invoiceProduct["cr651_mon_total_amount"] = product.GetAttributeValue<Money>("cr651_mon_total_amount") ?? new Money(0);

                    service.Create(invoiceProduct);
                    tracingService.Trace($"Invoice Product created: {invoiceProduct["cr651_name"]}");
                }
                else
                {
                    tracingService.Trace($"Product ID: {product.Id}, Attribute: cr651_workorderproductid, Value: {product.Id} does not have a valid product reference. Skipping this product.");
                }
            }

            
            var queryServices = new QueryExpression("cr651_workorderservices");
            queryServices.ColumnSet = new ColumnSet("cr651_fk_service", "cr651_int_duration", "cr651_mon_price_per_unit", "cr651_mon_total_amount");
            queryServices.Criteria.AddCondition("cr651_fk_work_order", ConditionOperator.Equal, workOrderRef.Id);

            var workOrderServices = service.RetrieveMultiple(queryServices).Entities;

            tracingService.Trace($"Number of Work Order Services retrieved: {workOrderServices.Count}");

            foreach (var workorderservice in workOrderServices)
            {
                EntityReference serviceRef = workorderservice.GetAttributeValue<EntityReference>("cr651_fk_service");

                if (serviceRef != null)
                {
                    
                    int? durationInMinutes = workorderservice.GetAttributeValue<int?>("cr651_int_duration");
                    int durationInHours = durationInMinutes.HasValue ? (int)Math.Floor((decimal)durationInMinutes.Value / 60) : 0;

                    
                    Entity invoiceProduct = new Entity("cr651_invoice_product");
                    invoiceProduct["cr651_fk_invoice"] = new EntityReference("cr651_invoice", invoiceId);
                    invoiceProduct["cr651_name"] = "Service: " + (serviceRef.Name ?? "Unknown Service");
                    invoiceProduct["cr651_fk_product"] = serviceRef;
                    invoiceProduct["cr651_int_quantity"] = durationInHours;  
                    invoiceProduct["cr651_mon_price_per_unit"] = workorderservice.GetAttributeValue<Money>("cr651_mon_price_per_unit") ?? new Money(0);
                    invoiceProduct["cr651_mon_total_amount"] = workorderservice.GetAttributeValue<Money>("cr651_mon_total_amount") ?? new Money(0);

                    
                    service.Create(invoiceProduct);
                    tracingService.Trace($"Invoice Service created: {invoiceProduct["cr651_name"]}");
                }
                else
                {
                    tracingService.Trace($"Service ID: {workorderservice.Id} does not have a valid service reference. Skipping this service.");
                }
            }


            tracingService.Trace("Execution completed successfully.");
        }
    }
}
