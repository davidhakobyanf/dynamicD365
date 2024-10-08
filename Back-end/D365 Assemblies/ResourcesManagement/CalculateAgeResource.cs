using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourcesManagement
{
    public class CalculateAgeResource : CodeActivity
    {
        [Input("resourceDateOfBirth")]
        [AttributeTarget("cr651_resource", "cr651_dt_date_of_birth")]
        public InArgument<DateTime> ResourceDateOfBirth { get; set; }

        [Output("resourceAge")]
        public OutArgument<int> ResourceAge { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                DateTime resourceDateOfBirth = ResourceDateOfBirth.Get(executionContext);


                DateTime today = DateTime.Today;


                int age = today.Year - resourceDateOfBirth.Year;


                if (today < resourceDateOfBirth.AddYears(age))
                {
                    age--;
                }


                ResourceAge.Set(executionContext, age);
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");

                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
