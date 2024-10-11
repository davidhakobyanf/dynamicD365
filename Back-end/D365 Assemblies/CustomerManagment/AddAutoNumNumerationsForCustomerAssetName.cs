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
            
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Plugin execution started.");

            try
            {
                
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                 
                    Entity customerAsset = (Entity)context.InputParameters["Target"];
                    tracingService.Trace("Target entity retrieved.");

                    
                    if (customerAsset.Contains("cr651_fk_account") && customerAsset["cr651_fk_account"] is EntityReference)
                    {
                        EntityReference accountReference = (EntityReference)customerAsset["cr651_fk_account"];
                        tracingService.Trace($"Account reference found: {accountReference.Id}");

                        
                        Entity account = service.Retrieve("cr651_account", accountReference.Id, new ColumnSet("cr651_name"));
                        tracingService.Trace("Account details retrieved.");

                        
                        if (account != null && account.Contains("cr651_name"))
                        {
                            string accountName = account["cr651_name"].ToString();
                            tracingService.Trace($"Account name: {accountName}");

                            
                            QueryExpression query = new QueryExpression("cr651_asset")
                            {
                                ColumnSet = new ColumnSet("cr651_name"),
                                Orders =
                                {
                                    new OrderExpression("createdon", OrderType.Descending)
                                },
                                TopCount = 1,
                                Criteria =
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("cr651_fk_account", ConditionOperator.Equal, accountReference.Id)
                                    }
                                }
                            };
                            tracingService.Trace("Query to retrieve the last asset created (across all accounts).");

                            
                            EntityCollection result = service.RetrieveMultiple(query);
                            tracingService.Trace("Query executed successfully.");

                            
                            int lastNumber = 0;
                            if (result.Entities.Count > 0 && result.Entities[0].Contains("cr651_name"))
                            {
                                string lastAssetName = result.Entities[0]["cr651_name"].ToString();
                                tracingService.Trace($"Last asset name retrieved: {lastAssetName}");

                                
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

                            
                            string newAssetName = $"{accountName}-{(lastNumber + 1):D4}";
                            tracingService.Trace($"Generated new asset name: {newAssetName}");

                            
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
