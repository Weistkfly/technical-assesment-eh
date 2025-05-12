namespace CryptoPriceTracker.Api.Models
{
    public class CryptoAssetPriceHistoryEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public List<PriceHistoryEntry> PriceHistoryEntries { get; set; } = Enumerable.Empty<PriceHistoryEntry>().ToList();
    }
}
