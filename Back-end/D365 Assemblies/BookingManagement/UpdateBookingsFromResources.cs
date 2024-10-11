using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingManagement
{
    public class UpdateBookingsFromResources : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
       
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

           
            if (context.MessageName != "Update" || context.Stage != 10) 
            {
                return;
            }

           
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity booking)
            {
                if (booking.LogicalName != "cr651_booking")
                    return;

                
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory?.CreateOrganizationService(context.UserId);

                if (service == null)
                {
                    throw new InvalidPluginExecutionException("Organization service is not available.");
                }

                
                if (context.PreEntityImages.Contains("preimagerecources") && context.PreEntityImages["preimagerecources"] is Entity preImage)
                {
                    
                    EntityReference oldResource = preImage.GetAttributeValue<EntityReference>("cr651_fk_resource");
                    EntityReference newResource = booking.GetAttributeValue<EntityReference>("cr651_fk_resource");

                    
                    Guid resourceId = newResource?.Id ?? oldResource?.Id ?? Guid.Empty;

                    
                    DateTime startDate = booking.GetAttributeValue<DateTime>("cr651_dt_start_date");
                    DateTime endDate = booking.GetAttributeValue<DateTime>("cr651_dt_end_date");

                    
                    if (preImage.GetAttributeValue<DateTime>("cr651_dt_start_date") != startDate ||
                        preImage.GetAttributeValue<DateTime>("cr651_dt_end_date") != endDate)
                    {
                        
                        CheckForConflicts(service, resourceId, startDate, endDate, booking.Id);
                    }
                    else
                    {
                        
                        CheckForConflicts(service, oldResource?.Id ?? Guid.Empty, startDate, endDate, booking.Id);
                    }
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("No valid booking entity found in the input parameters.");
            }
        }

        private void CheckForConflicts(IOrganizationService service, Guid resourceId, DateTime startDate, DateTime endDate, Guid bookingId)
        {
            
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

            
            query.Criteria.AddCondition(new ConditionExpression("cr651_bookingid", ConditionOperator.NotEqual, bookingId));

            
            EntityCollection results = service.RetrieveMultiple(query);

            if (results.Entities.Count > 0)
            {
                
                throw new InvalidPluginExecutionException("This booking conflicts with another booking for the same resource.");
            }
        }
    }
}
