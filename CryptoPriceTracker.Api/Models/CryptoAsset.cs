namespace CryptoPriceTracker.Api.Models
{
    public sealed class CryptoAsset
    {
        public int Id { get; set; } 
        public string ExternalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public ICollection<CryptoPriceHistory> PriceHistory { get; set; } = Enumerable.Empty<CryptoPriceHistory>().ToList();
    }
}