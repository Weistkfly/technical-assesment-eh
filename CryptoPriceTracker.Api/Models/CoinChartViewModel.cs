using System;
using System.Collections.Generic;

namespace CryptoPriceTracker.Api.Models
{
    public class CoinChartViewModel
    {
        public string CoinName { get; set; } = string.Empty; // Initialize with default value
        public string CoinSymbol { get; set; } = string.Empty; // Initialize with default value
        public List<PriceDataPoint> PriceHistory { get; set; } = new List<PriceDataPoint>(); // Initialize with new list
    }
}
