using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Model
{
    class Inventories
    {
        public Guid inventorieId { get; set; }
        public string inventoriesName { get; set; }
        public OptionSetType inventoriesType { get; set; }
        public Guid priceListId { get; set; }
    }
}
