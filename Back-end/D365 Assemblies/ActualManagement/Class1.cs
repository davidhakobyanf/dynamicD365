using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;

namespace WorkOrder
{
    public class ActualsWorkOrderProduct : CodeActivity
    {
        [Output("TotalCost")]
        public OutArgument<Money> TotalCost { get; set; } // Используем Money для правильного типа значения

        protected override void Execute(CodeActivityContext executionContext)
        {
            // Получение необходимых служб и контекста для выполнения действия
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // Работаем напрямую с crmContext, как вы указали
            IExecutionContext crmContext = executionContext.GetExtension<IExecutionContext>();

            try
            {
                // Извлечение входных параметров напрямую через crmContext.InputParameters
                EntityReference workOrderProductRef = (EntityReference)crmContext.InputParameters["WorkOrderProductRef"];
                Money costPerUnit = (Money)crmContext.InputParameters["CostPerUnit"];
                decimal quantity = (decimal)crmContext.InputParameters["Quantity"];

                // Вычисление общей стоимости
                decimal totalCostValue = costPerUnit.Value * quantity;
                Money totalCost = new Money(totalCostValue);

                // Установка общего значения в выходной параметр
                crmContext.OutputParameters["TotalCost"] = totalCost;

                // Логирование для отладки
                tracingService.Trace($"TotalCost calculated and set: {totalCostValue}");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
