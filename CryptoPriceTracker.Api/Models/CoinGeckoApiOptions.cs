namespace CryptoPriceTracker.Api.Models
{
    public class CoinGeckoApiOptions
    {
        public string BaseUrl { get; set; } = "https://api.coingecko.com";
        public string MarketDataEndpoint { get; set; } = "/api/v3/coins/markets";
        public string VsCurrency { get; set; } = "usd";
        public int PerPage { get; set; } = 250;
        public int MaxPage { get; set; } = 1;
        public string UserAgent { get; set; } = "CryptoPriceService/1.1";
    }
}
