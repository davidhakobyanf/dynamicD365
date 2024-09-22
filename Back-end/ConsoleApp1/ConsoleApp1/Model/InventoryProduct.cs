using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Model
{
    class InventoryProduct
    {

        public Guid inventoryProductId { get; set; }
        public Guid inventoryId { get; set; }
        public Guid productId { get; set; }
        public Guid currencyId { get; set; }

        public int quantity { get; set; }
        public Money monPricePerUnit { get; set; }
        public Money monTotalAmount { get; set; }


    }
}
