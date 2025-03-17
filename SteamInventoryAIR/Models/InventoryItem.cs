using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamInventoryAIR.Models
{
    public class InventoryItem
    {
        public string AssetId { get; set; }
        public string Name { get; set; }
        public string MarketHashName { get; set; }
        public string IconUrl { get; set; }
        public string Rarity { get; set; }
        public string Quality { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal MarketValue { get; set; }

        public string FormattedValue => $"{MarketValue:N2} €";

        public override string ToString()
        {
            return $"{Name} ({AssetId}) - {MarketHashName} - {Rarity} - {FormattedValue}";
        }
    }
}
