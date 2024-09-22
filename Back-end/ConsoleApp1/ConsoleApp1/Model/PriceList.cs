using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Model
{
    class PriceList
    {
        public Guid priceListId { get; set; }
        public string priceListName { get; set; }
        public Guid currencyId { get; set; }
    }
}
