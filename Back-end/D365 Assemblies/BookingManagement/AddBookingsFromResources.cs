using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingManagement
{
    public class AddBookingsFromResources : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));


            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity booking = (Entity)context.InputParameters["Target"];


                if (booking.LogicalName != "cr651_booking")
                    return;


                Guid resourceId = booking.GetAttributeValue<EntityReference>("cr651_fk_resource").Id;
                DateTime startDate = booking.GetAttributeValue<DateTime>("cr651_dt_start_date");
                DateTime endDate = booking.GetAttributeValue<DateTime>("cr651_dt_end_date");


                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


                QueryExpression query = new QueryExpression("cr651_booking")
                {
                    ColumnSet = new ColumnSet("cr651_dt_start_date", "cr651_dt_end_date"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {

                            new ConditionExpression("cr651_fk_resource", ConditionOperator.Equal, resourceId),

                            new ConditionExpression("cr651_dt_start_date", ConditionOperator.LessThan, endDate),

                            new ConditionExpression("cr651_dt_end_date", ConditionOperator.GreaterThan, startDate)
                        }
                    }
                };


                EntityCollection results = service.RetrieveMultiple(query);

                if (results.Entities.Count > 0)
                {

                    throw new InvalidPluginExecutionException("This reservation conflicts with another reservation for this resource.");
                }
            }
        }
    }
}
