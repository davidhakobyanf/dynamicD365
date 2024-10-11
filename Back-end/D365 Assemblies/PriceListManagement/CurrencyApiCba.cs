using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;
using System.IO;
using System.Net;
using System.Xml;

namespace PriceListManagement
{
    public class CurrencyApiCba : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace("CurrencyApiCba execution started.");

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://api.cba.am/exchangerates.asmx?op=ExchangeRatesByDate");
                webRequest.ContentType = "text/xml; charset=utf-8";
                webRequest.Method = "POST";

                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                string soapXml = $@"<?xml version='1.0' encoding='utf-8'?>
                            <soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                            xmlns:xsd='http://www.w3.org/2001/XMLSchema'
                            xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                              <soap:Body>
                                <ExchangeRatesByDate xmlns='http://www.cba.am/'>
                                  <date>{currentDate}</date>
                                </ExchangeRatesByDate>
                              </soap:Body>
                            </soap:Envelope>";

                using (Stream stream = webRequest.GetRequestStream())
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(soapXml);
                    }
                }

                WebResponse webResponse = webRequest.GetResponse();
                XmlDocument responseXml = new XmlDocument();

                using (Stream responseStream = webResponse.GetResponseStream())
                {
                    responseXml.Load(responseStream);
                }

                tracingService.Trace($"API response received. XML response:\n{responseXml.OuterXml}");

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(responseXml.NameTable);
                nsmgr.AddNamespace("cba", "http://www.cba.am/");

                XmlNode usdRateNode = responseXml.SelectSingleNode("//cba:ExchangeRate[cba:ISO='USD']/cba:Rate", nsmgr);

                if (usdRateNode == null)
                {
                    tracingService.Trace("USD rate not found in the response.");
                    return;
                }

                decimal exchangeRate = decimal.Parse(usdRateNode.InnerText);
                tracingService.Trace($"USD exchange rate today: {exchangeRate}");

                decimal amdToUsdRate = 1 / exchangeRate;
                tracingService.Trace($"AMD 1.00 = USD {amdToUsdRate}");

                Entity currency = new Entity("transactioncurrency");
                currency.Id = new Guid("43871D60-347A-EF11-AC20-6045BDA18BD1");
                currency["exchangerate"] = amdToUsdRate;

                tracingService.Trace($"Updating currency record in D365. Rate: {amdToUsdRate}");
                service.Update(currency);
                tracingService.Trace("Currency record successfully updated in D365.");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Error in CurrencyApiCba: {ex.Message}");
            }
        }
    }
}
