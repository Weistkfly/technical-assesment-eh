using CryptoPriceTracker.Api.Models;

namespace CryptoPriceTracker.Api.Validators
{
    public class PriceValidator
    {
        public static void ValidateApiOptions(CoinGeckoApiOptions apiOptions)
        {
            if (apiOptions == null) throw new ArgumentNullException(nameof(apiOptions));
            if (string.IsNullOrWhiteSpace(apiOptions.UserAgent))
                throw new ArgumentException("UserAgent must be configured in CoinGeckoApiOptions.");
            if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
                throw new ArgumentException("BaseUrl must be configured in CoinGeckoApiOptions.");
            if (string.IsNullOrWhiteSpace(apiOptions.MarketDataEndpoint))
                throw new ArgumentException("MarketDataEndpoint must be configured in CoinGeckoApiOptions.");
            if (string.IsNullOrWhiteSpace(apiOptions.VsCurrency))
                throw new ArgumentException("VsCurrency must be configured in CoinGeckoApiOptions.");
            if (apiOptions.PerPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(apiOptions.PerPage));
            if (apiOptions.MaxPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(apiOptions.MaxPage));
        }

        public static bool AreApiAssetsRetrieved(CryptoAssetResponse[]? apiAssets)
        {
            return apiAssets != null && apiAssets.Length > 0;
        }

        public static bool IsValidCryptoAssetResponse(CryptoAssetResponse? responseAsset)
        {
            if (responseAsset == null) return false;
            if (string.IsNullOrWhiteSpace(responseAsset.id)) return false;
            return true;
        }

        public static DateTime EnsureUtc(DateTime? dateTime, DateTime defaultValueUtc)
        {
            if (!dateTime.HasValue) return defaultValueUtc;
            return EnsureUtc(dateTime.Value); 
        }

        public static DateTime EnsureUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }
            return dateTime; 
        }

        public static bool IsPriceUpdateNeeded(DateTime? lastKnownHistoryDateUtc, DateTime effectiveApiUpdateTimeUtc)
        {
            if (lastKnownHistoryDateUtc.HasValue)
            {
                DateTime ensuredLastKnownDateUtc = EnsureUtc(lastKnownHistoryDateUtc.Value);
                DateTime ensuredEffectiveApiUpdateDateUtc = EnsureUtc(effectiveApiUpdateTimeUtc);

                if (ensuredEffectiveApiUpdateDateUtc <= ensuredLastKnownDateUtc)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool HasSufficientPriceHistoryForTrend(List<PriceHistoryEntry>? priceHistoryEntries)
        {
            return priceHistoryEntries != null && priceHistoryEntries.Any();
        }
    }
}