using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerManagment
{
    public class TestUpdate: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity priceListItem = (Entity)context.InputParameters["Target"];
                Entity priceListItem2 = service.Retrieve(priceListItem.LogicalName, priceListItem.Id, new ColumnSet("cr651_fk_product", "cr651_mon_price_per_unit"));
                int intValue = 0;
                if (priceListItem.Contains("cr651_mon_price_per_unit"))
                {
                    intValue = priceListItem.GetAttributeValue<int>("bvr_new_int");

                }
                else
                {
                    intValue = priceListItem2.GetAttributeValue<int>("bvr_new_int");
                }

                Money moneyValue = priceListItem2.GetAttributeValue<Money>("cr651_mon_price_per_unit");
                EntityReference productValue = priceListItem2.GetAttributeValue<EntityReference>("cr651_fk_product");

                

            }


        }
    }
}
