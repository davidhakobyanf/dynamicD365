using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CustomerManagment
{
    public class TestDelete: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
            {
                EntityReference priceList = (EntityReference)context.InputParameters["Target"];
                try
                {
                    EntityReference priceListItemRef = (EntityReference)context.InputParameters["Target"];
                    Entity priceListItem = context.PreEntityImages["preimage"];

                    //Entity priceListItem = service.Retrieve(priceListItemRef.LogicalName, priceListItemRef.Id, new ColumnSet("cr651_fk_price_list", "cr651_fk_product", "transactioncurrencyid", "cr651_mon_price_per_unit"));
                    EntityReference priceListRef = priceListItem.GetAttributeValue<EntityReference>("cr651_fk_price_list");
                    EntityReference productRef = priceListItem.GetAttributeValue<EntityReference>("cr651_fk_product");
                    EntityReference currencyRef = priceListItem.GetAttributeValue<EntityReference>("transactioncurrencyid");
                    Money pricePerUnit = priceListItem.GetAttributeValue<Money>("cr651_mon_price_per_unit");


                    if(pricePerUnit!=null)
                    {
                        tracingService.Trace("Money value is " + pricePerUnit);
                    }

                    if (priceListRef != null)
                    {
                        tracingService.Trace("priceList value is " + priceListRef);
                    }
                    if (productRef != null)
                    {
                        tracingService.Trace("product value is " + productRef);
                    }
                    if (currencyRef != null)
                    {
                        tracingService.Trace("currency value is " + currencyRef);
                    }
                    
                
                
                
                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Error occured in plugin: " + ex.Message);
                }



            }


        }
    }
}
