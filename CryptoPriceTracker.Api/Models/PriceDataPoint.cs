using System;

namespace CryptoPriceTracker.Api.Models
{
    public class PriceDataPoint
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }
}
