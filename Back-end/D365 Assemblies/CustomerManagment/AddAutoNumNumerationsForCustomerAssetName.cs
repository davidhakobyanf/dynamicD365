using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text.RegularExpressions;

namespace CustomerManagment
{
    public class AddAutoNumerationForCustomerAssetName : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the organization service reference.
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // Obtain tracing service for logging.
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Plugin execution started.");

            try
            {
                // Check if the target entity is available and is of type "Entity"
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    // Get the target entity
                    Entity customerAsset = (Entity)context.InputParameters["Target"];
                    tracingService.Trace("Target entity retrieved.");

                    // Check if cr651_fk_account is present (the lookup to the account)
                    if (customerAsset.Contains("cr651_fk_account") && customerAsset["cr651_fk_account"] is EntityReference)
                    {
                        EntityReference accountReference = (EntityReference)customerAsset["cr651_fk_account"];
                        tracingService.Trace($"Account reference found: {accountReference.Id}");

                        // Retrieve the account details
                        Entity account = service.Retrieve("cr651_account", accountReference.Id, new ColumnSet("cr651_name"));
                        tracingService.Trace("Account details retrieved.");

                        // Make sure the account has a cr651_name field
                        if (account != null && account.Contains("cr651_name"))
                        {
                            string accountName = account["cr651_name"].ToString();
                            tracingService.Trace($"Account name: {accountName}");

                            // Retrieve the last asset created (across all accounts), ordered by createdon
                            QueryExpression query = new QueryExpression("cr651_asset")
                            {
                                ColumnSet = new ColumnSet("cr651_name"),
                                Orders =
                                {
                                    new OrderExpression("createdon", OrderType.Descending)
                                },
                                TopCount = 1
                            };
                            tracingService.Trace("Query to retrieve the last asset created (across all accounts).");

                            // Execute the query
                            EntityCollection result = service.RetrieveMultiple(query);
                            tracingService.Trace("Query executed successfully.");

                            // Extract the last number from the last asset name, if any
                            int lastNumber = 0;
                            if (result.Entities.Count > 0 && result.Entities[0].Contains("cr651_name"))
                            {
                                string lastAssetName = result.Entities[0]["cr651_name"].ToString();
                                tracingService.Trace($"Last asset name retrieved: {lastAssetName}");

                                // Use regular expression to extract the numeric part from the last asset name
                                Match match = Regex.Match(lastAssetName, @"-(\d+)$");
                                if (match.Success)
                                {
                                    lastNumber = int.Parse(match.Groups[1].Value);
                                    tracingService.Trace($"Last asset number extracted: {lastNumber}");
                                }
                                else
                                {
                                    tracingService.Trace("No numeric part found in the last asset name.");
                                }
                            }
                            else
                            {
                                tracingService.Trace("No previous asset found.");
                            }

                            // Generate the new asset name by incrementing the last number
                            string newAssetName = $"{accountName}-{(lastNumber + 1):D4}";
                            tracingService.Trace($"Generated new asset name: {newAssetName}");

                            // Set the cr651_name field on the asset
                            customerAsset["cr651_name"] = newAssetName;
                            tracingService.Trace("New asset name set on the target entity.");
                        }
                        else
                        {
                            tracingService.Trace("Account name not found.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("cr651_fk_account not found on the target entity.");
                    }
                }
                else
                {
                    tracingService.Trace("Target entity not found in InputParameters.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error occurred: {ex.Message}");
                throw;
            }

            tracingService.Trace("Plugin execution finished.");
        }
    }
}
