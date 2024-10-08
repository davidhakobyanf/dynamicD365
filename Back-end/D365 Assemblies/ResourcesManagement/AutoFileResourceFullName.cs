using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerManagment
{
    public class AutoFileResourceFullName : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity contact = (Entity)context.InputParameters["Target"];

                if (contact.Contains("cr651_slot_firstname") && contact.Contains("cr651_slot_lastname"))
                {
                    string firstName = contact.GetAttributeValue<string>("cr651_slot_firstname");
                    string lastName = contact.GetAttributeValue<string>("cr651_slot_lastname");
                    string fullName = firstName + "  " + lastName;
                    contact["cr651_name"] = fullName;
                    service.Update(contact);
                }
            }
        }

    }
}
