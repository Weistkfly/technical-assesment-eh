namespace CryptoPriceTracker.Api.Models
{
    public sealed class CryptoAssetResponse
    {
        public string id { get; set; } = string.Empty;
        public string symbol { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string image { get; set; } = string.Empty;
        public decimal? current_price { get; set; }
        public DateTime? last_updated { get; set; }

        public CryptoAsset MapToCryptoAsset()
        {
            return new CryptoAsset
            {
                ExternalId = id,
                Currency = "usd",
                Name = name,
                IconUrl = image,
                Symbol = symbol,
            };
        }
    }
}