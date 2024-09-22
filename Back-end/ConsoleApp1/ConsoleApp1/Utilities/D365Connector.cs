using ConsoleApp1.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Utilities
{
    class D365Connector
    {
        private string D365username;
        private string D365password;
        private string D365URL;
        private CrmServiceClient service;

        public D365Connector(string d365username, string d365password, string d365URL)
        {
            this.D365username = d365username;
            this.D365password = d365password;
            this.D365URL = d365URL;

            string authType = "OAuth";
            string appId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
            string reDirectURI = "app://58145B91-0C36-4500-8554-080854F2AC97";
            string loginPrompt = "Auto";

            string ConnectionString = string.Format("AuthType = {0};Username = {1};Password = {2}; Url = {3}; AppId={4}; RedirectURI={5}; LoginPrompt={6};",
                  authType, D365username, D365password, D365URL, appId, reDirectURI, loginPrompt);
            this.service = new CrmServiceClient(ConnectionString);
        }

        // Метод для управления продуктами в инвентаре (добавление/вычитание)
        public InventoryProduct manageInventoryProduct(string inventoryName, string productName, int quantity, string type)
        {
            try
            {
                Console.WriteLine($"Attempting to retrieve Inventory and Product for {type}...");

                // Получаем идентификаторы Inventory и Product
                Guid inventoryId = GetEntityIdByName("cr651_inventory", "cr651_name", inventoryName);
                Guid productId = GetEntityIdByName("cr651_products", "cr651_name", productName);

                if (inventoryId == Guid.Empty || productId == Guid.Empty)
                {
                    Console.WriteLine("Inventory or Product not found.");
                    return null;
                }

                Console.WriteLine($"Inventory ID: {inventoryId}, Product ID: {productId}");

                // Запрос для получения продукта в инвентаре
                QueryExpression inventoryProductQuery = new QueryExpression
                {
                    EntityName = "cr651_inventory_product",
                    ColumnSet = new ColumnSet("cr651_name", "cr651_fk_inventory", "cr651_fk_product", "cr651_int_quantity", "cr651_mon_price_per_unit", "cr651_mon_total_amount"),
                    Criteria =
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("cr651_fk_product", ConditionOperator.Equal, productId),
                    new ConditionExpression("cr651_fk_inventory", ConditionOperator.Equal, inventoryId),
                }
            }
                };

                // Выполняем запрос
                Console.WriteLine("Executing query...");
                EntityCollection inventoryProductList = service.RetrieveMultiple(inventoryProductQuery);

                // Объект InventoryProduct для возврата результата
                InventoryProduct inventoryProductObj = null;

                // Проверяем, нашелся ли продукт в инвентаре
                if (inventoryProductList.Entities.Count > 0)
                {
                    // Существующая запись
                    Entity list = inventoryProductList.Entities[0];
                    inventoryProductObj = new InventoryProduct();
                    inventoryProductObj.inventoryProductId = list.Id;

                    // Получаем значения из сущности
                    EntityReference inventory = list.GetAttributeValue<EntityReference>("cr651_fk_inventory");
                    EntityReference product = list.GetAttributeValue<EntityReference>("cr651_fk_product");
                    EntityReference transactionCurrency = list.GetAttributeValue<EntityReference>("transactioncurrencyid");

                    inventoryProductObj.quantity = list.GetAttributeValue<int>("cr651_int_quantity");
                    inventoryProductObj.monPricePerUnit = list.GetAttributeValue<Money>("cr651_mon_price_per_unit");
                    inventoryProductObj.monTotalAmount = list.GetAttributeValue<Money>("cr651_mon_total_amount");

                    Console.WriteLine($"Quantity: {inventoryProductObj.quantity}");
                    Console.WriteLine($"Price per unit: {inventoryProductObj.monPricePerUnit.Value}");
                    Console.WriteLine($"Total amount: {inventoryProductObj.monTotalAmount.Value}");

                    if (transactionCurrency != null && inventory != null && product != null)
                    {
                        inventoryProductObj.inventoryId = inventory.Id;
                        inventoryProductObj.productId = product.Id;
                        inventoryProductObj.currencyId = transactionCurrency.Id;
                    }

                    // Обработка типов операций
                    if (type == "subtraction")
                    {
                        // Проверяем, достаточно ли количества для вычитания
                        if (inventoryProductObj.quantity >= quantity)
                        {
                            inventoryProductObj.quantity -= quantity;
                            Console.WriteLine("Quantity updated after subtraction.");
                        }
                        else
                        {
                            Console.WriteLine("Insufficient quantity for subtraction.");
                            return null;
                        }
                    }
                    else if (type == "addition")
                    {
                        // Добавляем количество
                        inventoryProductObj.quantity += quantity;
                        Console.WriteLine("Quantity updated after addition.");
                    }

                    // Обновляем запись в CRM
                    Entity updatedInventoryProduct = new Entity("cr651_inventory_product", inventoryProductObj.inventoryProductId);
                    updatedInventoryProduct["cr651_int_quantity"] = inventoryProductObj.quantity;
                    service.Update(updatedInventoryProduct);
                }
                else if (type == "addition")
                {
                    // Получаем валюту через функцию GetFirstPriceListCurrency
                    EntityReference currency = GetFirstPriceListCurrency();

                    if (currency == null)
                    {
                        Console.WriteLine("No valid currency found in price lists.");
                        return null;
                    }

                    // Создаем новую запись в инвентаре
                    inventoryProductObj = new InventoryProduct
                    {
                        inventoryId = inventoryId,
                        productId = productId,
                        quantity = quantity,
                        monPricePerUnit = new Money(1),
                        monTotalAmount = new Money(quantity * 1),
                        currencyId = currency.Id
                    };

                    Entity newInventoryProduct = new Entity("cr651_inventory_product");
                    newInventoryProduct["cr651_fk_inventory"] = new EntityReference("cr651_inventory", inventoryProductObj.inventoryId);
                    newInventoryProduct["cr651_fk_product"] = new EntityReference("cr651_products", inventoryProductObj.productId);
                    newInventoryProduct["cr651_int_quantity"] = inventoryProductObj.quantity;
                    newInventoryProduct["cr651_mon_price_per_unit"] = inventoryProductObj.monPricePerUnit;
                    newInventoryProduct["cr651_mon_total_amount"] = inventoryProductObj.monTotalAmount;

                    newInventoryProduct["transactioncurrencyid"] = currency;

                    service.Create(newInventoryProduct);
                    Console.WriteLine("New inventory product line created.");
                }

                return inventoryProductObj;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                return null;
            }
        }

        // Функция для получения первой доступной валюты из прайс-листа
        private EntityReference GetFirstPriceListCurrency()
        {
            try
            {
                QueryExpression priceListQuery = new QueryExpression
                {
                    EntityName = "cr651_pricelist",
                    ColumnSet = new ColumnSet("transactioncurrencyid"),
                };

                EntityCollection priceLists = service.RetrieveMultiple(priceListQuery);

                if (priceLists.Entities.Count > 0)
                {
                    Entity priceList = priceLists.Entities[0];
                    EntityReference currency = priceList.GetAttributeValue<EntityReference>("transactioncurrencyid");

                    if (currency != null)
                    {
                        Console.WriteLine($"Currency found: {currency.Id}");
                        return currency;
                    }
                }

                Console.WriteLine("No valid price list currency found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while retrieving the price list currency: " + ex.Message);
                return null;
            }
        }

        // Метод для удаления всех продуктов в инвентаре
        public void DeleteAllInventoryProducts()
        {
            try
            {
                Console.WriteLine("Deleting all inventory products...");

                // Запрос для получения всех продуктов в инвентаре
                QueryExpression query = new QueryExpression
                {
                    EntityName = "cr651_inventory_product",
                    ColumnSet = new ColumnSet("cr651_name")
                };

                EntityCollection inventoryProductList = service.RetrieveMultiple(query);

                // Проверяем, есть ли записи
                if (inventoryProductList.Entities.Count > 0)
                {
                    foreach (Entity product in inventoryProductList.Entities)
                    {
                        // Удаляем каждую запись
                        service.Delete("cr651_inventory_product", product.Id);
                        Console.WriteLine($"Deleted product with ID: {product.Id}");
                    }
                    Console.WriteLine("All inventory products have been deleted.");
                }
                else
                {
                    Console.WriteLine("No inventory products found to delete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while deleting inventory products: " + ex.Message);
            }
        }

        // Метод для получения идентификатора сущности по имени
        private Guid GetEntityIdByName(string entityName, string attributeName, string entityNameValue)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet(attributeName),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(attributeName, ConditionOperator.Equal, entityNameValue)
                    }
                }
            };

            EntityCollection entityCollection = service.RetrieveMultiple(query);
            if (entityCollection.Entities.Count > 0)
            {
                return entityCollection.Entities[0].Id;
            }
            return Guid.Empty;
        }
    }
}
