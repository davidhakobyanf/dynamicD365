using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerManagment
{
    public class updatePriceListItemsCurrencyFromPriceList: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity priceList = (Entity)context.InputParameters["Target"];
                if (priceList.Contains("transactioncurrencyid"))
                {
                    EntityReference currency = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");

                    QueryExpression priceListItemsQuery = new QueryExpression
                    {
                        EntityName = "cr651_price_list_item",
                        ColumnSet = new ColumnSet(null),
                        Criteria =
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("cr651_fk_price_list", ConditionOperator.Equal, priceList.Id)
                }
            }
                    };

                    EntityCollection priceListItems = service.RetrieveMultiple(priceListItemsQuery);
                    foreach(Entity priceListItem in priceListItems.Entities)
                    {
                        priceListItem["transactioncurrencyid"] = currency;
                        service.Update(priceListItem);
                    }
                }
            }

            // Uncomment the following line if you want to update the price list item in the system
            // service.Update(priceListItem);
        }

    }
}
