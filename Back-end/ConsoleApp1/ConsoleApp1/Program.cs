using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Windows;
using ConsoleApp1.Utilities;
using ConsoleApp1.Model;
using System.Windows.Documents;
using System.Collections.Generic;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                D365Connector d365Connector = new D365Connector("DavidHakobyan@Bever184.onmicrosoft.com", "Dav.Bever2003", "https://org8e062090.api.crm4.dynamics.com/api/data/v9.2/");
                Console.WriteLine("Successfully connected to D365.");

                string type = GetOperationType();
                Console.WriteLine($"You selected: {type}");

                // Если выбрано удаление всех продуктов
                if (type == "delete_all")
                {
                    d365Connector.DeleteAllInventoryProducts();
                    return; // Завершаем выполнение после удаления всех продуктов
                }

                Console.WriteLine("Please enter Inventory Name:");
                string inventoryName = Console.ReadLine();

                Console.WriteLine("Please enter Product Name:");
                string productName = Console.ReadLine();

                Console.WriteLine("Please enter Quantity:");
                string quantityInput = Console.ReadLine();
                int quantity;

                if (int.TryParse(quantityInput, out quantity))
                {
                    // Значение успешно преобразовано в int, можно продолжать
                }
                else
                {
                    Console.WriteLine("Invalid quantity entered. Please enter a valid number.");
                    return;  // Завершить выполнение, если введено некорректное значение
                }

                InventoryProduct inventoryProductList = d365Connector.manageInventoryProduct(inventoryName, productName, quantity, type);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Strange error occurred: " + ex.Message);
            }
        }

        // Метод GetOperationType
        public static string GetOperationType()
        {
            while (true)
            {
                Console.WriteLine("Please select Type of operation:");
                Console.WriteLine("1. Addition");
                Console.WriteLine("2. Subtraction");
                Console.WriteLine("3. Delete All Inventory Products");
                Console.Write("Enter your choice (1, 2 or 3): ");

                // Чтение ввода пользователя
                string input = Console.ReadLine();

                // Проверка ввода и возврат соответствующего типа операции
                if (input == "1")
                {
                    return "addition";
                }
                else if (input == "2")
                {
                    return "subtraction";
                }
                else if (input == "3")
                {
                    return "delete_all";
                }
                else
                {
                    // Сообщение об ошибке и повтор запроса
                    Console.WriteLine("Invalid choice. Please select 1 for Addition, 2 for Subtraction, or 3 to Delete All Inventory Products.");
                }
            }
        }
    }
}
