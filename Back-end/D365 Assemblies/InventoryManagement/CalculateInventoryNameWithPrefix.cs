using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment
{
    public class CalculateInventoryNameWithPrefix : CodeActivity
    {
        [Input("inventoryName")]
        public InArgument<string> InventoryName { get; set; }

        [Input("inventoryType")]
        [AttributeTarget("cr651_inventory", "cr651_os_type")]
        public InArgument<OptionSetValue> InventoryType { get; set; }

        [Output("inventoryFullName")]
        public OutArgument<string> InventoryFullName { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            

            try
            {
                string inventoryName = InventoryName.Get(executionContext);
                OptionSetValue inventoryType = InventoryType.Get(executionContext);
                string fullName = inventoryName;
                tracingService.Trace(inventoryName);
                tracingService.Trace(inventoryType.Value.ToString());


               
                if(inventoryType != null && inventoryType.Value == 523250000  && !inventoryName.StartsWith("Primary: "))
                {
                    fullName = "Primary: " + inventoryName;
                }
                if (inventoryType != null && inventoryType.Value == 523250001 && !inventoryName.StartsWith("Secondary: "))
                {
                    fullName = "Secondary: " + inventoryName;
                }

                InventoryFullName.Set(executionContext, fullName);

            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }


    }
}
