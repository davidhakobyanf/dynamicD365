using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace CustomerManagment
{
    public class UpdateBookingsFromResources : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Check if the plugin is triggered on Update
            if (context.MessageName != "Update" || context.Stage != 10) // PreValidation stage
            {
                return;
            }

            // Get the target entity from the context (the entity that is being updated)
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity booking)
            {
                if (booking.LogicalName != "cr651_booking")
                    return;

                // Get the organization service
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory?.CreateOrganizationService(context.UserId);

                if (service == null)
                {
                    throw new InvalidPluginExecutionException("Organization service is not available.");
                }

                // Get the pre-image (the original values of the booking entity before the update)
                if (context.PreEntityImages.Contains("preimagerecources") && context.PreEntityImages["preimagerecources"] is Entity preImage)
                {
                    // Check if cr651_fk_resource has changed
                    EntityReference oldResource = preImage.GetAttributeValue<EntityReference>("cr651_fk_resource");
                    EntityReference newResource = booking.GetAttributeValue<EntityReference>("cr651_fk_resource");

                    // If resource has changed, use the new resource value
                    Guid resourceId = newResource?.Id ?? oldResource?.Id ?? Guid.Empty;

                    // Get start and end dates
                    DateTime startDate = booking.GetAttributeValue<DateTime>("cr651_dt_start_date");
                    DateTime endDate = booking.GetAttributeValue<DateTime>("cr651_dt_end_date");

                    // If start or end date changed, adjust start and end date variables
                    if (preImage.GetAttributeValue<DateTime>("cr651_dt_start_date") != startDate ||
                        preImage.GetAttributeValue<DateTime>("cr651_dt_end_date") != endDate)
                    {
                        // Perform conflict check with the current start and end dates
                        CheckForConflicts(service, resourceId, startDate, endDate, booking.Id);
                    }
                    else
                    {
                        // No changes in dates; check conflicts with the old resource
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
            // Query to check for conflicting bookings
            QueryExpression query = new QueryExpression("cr651_booking")
            {
                ColumnSet = new ColumnSet("cr651_dt_start_date", "cr651_dt_end_date"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Same resource
                        new ConditionExpression("cr651_fk_resource", ConditionOperator.Equal, resourceId),
                        // Overlapping bookings
                        new ConditionExpression("cr651_dt_start_date", ConditionOperator.LessThan, endDate),
                        new ConditionExpression("cr651_dt_end_date", ConditionOperator.GreaterThan, startDate)
                    }
                }
            };

            // Exclude the current booking from the query results
            query.Criteria.AddCondition(new ConditionExpression("cr651_bookingid", ConditionOperator.NotEqual, bookingId));

            // Retrieve potential conflicts
            EntityCollection results = service.RetrieveMultiple(query);

            if (results.Entities.Count > 0)
            {
                // Conflict found, throw an exception
                throw new InvalidPluginExecutionException("This booking conflicts with another booking for the same resource.");
            }
        }
    }
}
