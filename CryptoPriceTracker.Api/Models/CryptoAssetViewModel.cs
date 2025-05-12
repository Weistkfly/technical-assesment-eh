namespace CryptoPriceTracker.Api.Models
{
    public sealed class CryptoAssetViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CurrentPrice { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public TrendViewModel? Trend { get; set; }
    }
}
