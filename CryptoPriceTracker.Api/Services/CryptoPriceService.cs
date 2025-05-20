using System.Globalization;
using System.Text.Json;
using CryptoPriceTracker.Api.Data; 
using CryptoPriceTracker.Api.Models; 
using CryptoPriceTracker.Api.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CryptoPriceTracker.Api.Services
{
    public class CryptoPriceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CryptoPriceService> _logger; 
        private readonly CoinGeckoApiOptions _apiOptions;
        private readonly CacheService _cacheService;

        public CryptoPriceService(
            ApplicationDbContext dbContext,
            HttpClient httpClient,
            IOptions<CoinGeckoApiOptions> apiOptions,
            ILogger<CryptoPriceService> logger,
            CacheService cacheService)
        {
            _dbContext = dbContext;
            _httpClient = httpClient;
            _logger = logger;
            _apiOptions = apiOptions.Value;

            PriceValidator.ValidateApiOptions(_apiOptions);

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_apiOptions.UserAgent);
            _cacheService = cacheService;
        }

        public async Task UpdatePricesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting crypto price update process at {StartTimeUtc} UTC.", DateTime.UtcNow);

            var assetCache = await BuildAssetCacheAsync(cancellationToken);
            var jobStartTimeUtc = DateTime.UtcNow; 
            int currentPage = 1;
            int totalAssetsProcessedThisRun = 0, newAssetsAddedThisRun = 0, priceUpdatesThisRun = 0;

            try
            {
                while (currentPage <= _apiOptions.MaxPage)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var apiAssets = await FetchAssetsFromApiAsync(currentPage, cancellationToken);

                    if (!PriceValidator.AreApiAssetsRetrieved(apiAssets))
                    {
                        _logger.LogInformation("No crypto assets returned by API (page {Page}). Processing may be complete or there was an issue fetching data.", currentPage);
                        break; 
                    }

                    _logger.LogInformation("Processing {Count} assets from API page {Page}.", apiAssets!.Length, currentPage);

                    var (newlyAdded, priceUpdates) = ProcessApiAssets(apiAssets!, assetCache, jobStartTimeUtc); 
                    newAssetsAddedThisRun += newlyAdded;
                    priceUpdatesThisRun += priceUpdates;
                    totalAssetsProcessedThisRun += apiAssets!.Length;

                    await SaveChangesToDbAsync(currentPage, cancellationToken);

                    if (currentPage == _apiOptions.MaxPage)
                    {
                        _logger.LogInformation("Reached configured MaxPage ({MaxPage}). Stopping price update process.", _apiOptions.MaxPage);
                        break;
                    }
                    currentPage++;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Crypto price update operation was cancelled.");
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An unexpected error occurred during the crypto price update process on page {Page}.", currentPage);
            }
            finally
            {
                _logger.LogInformation("Crypto price update process finished. Total assets processed: {TotalProcessed}, New assets added: {NewAssets}, Price updates: {PriceUpdates}",
                    totalAssetsProcessedThisRun, newAssetsAddedThisRun, priceUpdatesThisRun);
            }
        }

        private async Task<Dictionary<string, (CryptoAsset AssetEntity, DateTime? LastHistoryDate)>> BuildAssetCacheAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Building asset cache from database...");

            var assetsFromDb = await _dbContext.CryptoAssets
               .Select(asset => new
               {
                   asset.ExternalId,
                   AssetEntity = asset,
                   LastHistoryDate = asset.PriceHistory
                                       .OrderByDescending(h => h.Date)
                                       .Select(h => (DateTime?)h.Date)
                                       .FirstOrDefault()
               })
               .ToListAsync(cancellationToken);

            var cache = assetsFromDb!.ToDictionary(
               data => data.ExternalId,
               data => (data.AssetEntity, data.LastHistoryDate.HasValue ? PriceValidator.EnsureUtc(data.LastHistoryDate.Value) : (DateTime?)null)
            );

            _logger.LogInformation("Loaded {Count} existing assets into cache.", cache.Count); 
            return cache;
        }

        private async Task<CryptoAssetResponse[]?> FetchAssetsFromApiAsync(int page, CancellationToken cancellationToken)
        {
            var requestUrl = $"{_apiOptions.BaseUrl}{_apiOptions.MarketDataEndpoint}?vs_currency={_apiOptions.VsCurrency}&per_page={_apiOptions.PerPage}&page={page}";
            _logger.LogDebug("Fetching page {Page} from CoinGecko API: {Url}", page, requestUrl); 

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to CoinGecko API failed for page {Page}. URL: {Url}", page, requestUrl);
                return null; 
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "HTTP request to CoinGecko API timed out for page {Page}. URL: {Url}", page, requestUrl);
                return null;
            }
            catch (TaskCanceledException) 
            {
                _logger.LogWarning("HTTP request to CoinGecko API was cancelled for page {Page}.", page);
                throw; 
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("CoinGecko API request for page {Page} failed with status code {StatusCode}. Response (truncated): {Response}",
                    page, response.StatusCode, errorContent.Substring(0, Math.Min(errorContent.Length, 500)));
                return null; 
            }

            string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                return JsonSerializer.Deserialize<CryptoAssetResponse[]>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize CoinGecko API response for page {Page}. Content (truncated): {Json}", page, jsonContent.Substring(0, Math.Min(jsonContent.Length, 500)));
                return null; 
            }
        }

        private (int newAssetsAdded, int priceUpdates) ProcessApiAssets(
            IEnumerable<CryptoAssetResponse> apiAssets, 
            Dictionary<string, (CryptoAsset AssetEntity, DateTime? LastHistoryDate)> assetCache,
            DateTime jobStartTimeUtc) 
        {
            int newAssetsCount = 0;
            int priceUpdatesCount = 0;

            foreach (var responseAsset in apiAssets)
            {
                if (!PriceValidator.IsValidCryptoAssetResponse(responseAsset))
                {
                    _logger.LogWarning("Received invalid asset data (e.g., null or missing ID) from API. Skipping. Data: {@ResponseAsset}", responseAsset);
                    continue;
                }

                CryptoAsset assetEntity;
                DateTime? lastKnownHistoryDateUtc = null; 

                bool isNewAsset = !assetCache.TryGetValue(responseAsset!.id!, out var cachedAssetData);

                if (isNewAsset)
                {
                    assetEntity = responseAsset.MapToCryptoAsset(); 
                    _dbContext.CryptoAssets.Add(assetEntity);
                    assetCache[assetEntity.ExternalId] = (assetEntity, null); 
                    newAssetsCount++;
                    _logger.LogTrace("New asset '{AssetId}' ({Symbol}) identified. Adding to DbContext.", assetEntity.ExternalId, assetEntity.Symbol);
                }
                else
                {
                    assetEntity = cachedAssetData.AssetEntity;
                    assetEntity.Name = responseAsset.name ?? assetEntity.Name; 
                    assetEntity.Symbol = responseAsset.symbol?.ToUpperInvariant() ?? assetEntity.Symbol;
                    assetEntity.IconUrl = responseAsset.image ?? assetEntity.IconUrl;
                    lastKnownHistoryDateUtc = cachedAssetData.LastHistoryDate; 
                }

                DateTime effectiveApiUpdateTimeUtc = PriceValidator.EnsureUtc(responseAsset.last_updated, jobStartTimeUtc);

                if (!PriceValidator.IsPriceUpdateNeeded(lastKnownHistoryDateUtc, effectiveApiUpdateTimeUtc))
                {
                    _logger.LogTrace("Price update for asset '{AssetId}' at {Timestamp} UTC is not needed (already registered or stale). Skipping.", assetEntity.ExternalId, effectiveApiUpdateTimeUtc);
                    continue;
                }

                decimal price = responseAsset.current_price ?? 0m; 
                if (responseAsset.current_price == null)
                {
                    _logger.LogWarning("Asset '{AssetId}' received with null current_price at {Timestamp} UTC. Storing price as 0.", assetEntity.ExternalId, effectiveApiUpdateTimeUtc);
                }

                var newPriceHistory = new CryptoPriceHistory
                {
                    Date = effectiveApiUpdateTimeUtc, 
                    Price = price,
                    CryptoAssetId = assetEntity.Id 
                };

                assetEntity.PriceHistory ??= new List<CryptoPriceHistory>();

                assetEntity.PriceHistory.Add(newPriceHistory);

                assetCache[assetEntity.ExternalId] = (assetEntity, effectiveApiUpdateTimeUtc);
                priceUpdatesCount++;

                _logger.LogTrace("Adding new price history for asset '{AssetId}': Price={Price}, Date={DateUtc}", assetEntity.ExternalId, price, effectiveApiUpdateTimeUtc);
            }
            return (newAssetsCount, priceUpdatesCount);
        }

        private async Task SaveChangesToDbAsync(int page, CancellationToken cancellationToken)
        {
            try
            {
                var changes = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully saved {ChangeCount} changes from page {Page} to database.", changes, page);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to save changes to database for page {Page}. Further processing might be affected.", page);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Saving changes to database was cancelled for page {Page}.", page);
                throw;
            }
        }

        public async Task<List<CryptoAssetViewModel>> GetPricesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching latest crypto prices with trend data from the database.");

            var assetsWithPriceData = await _cacheService.GetOrCreateAsync(
                "coins",
                async (x) => await _dbContext.CryptoAssets
                .AsNoTracking()
                .Select(asset => new CryptoAssetPriceHistoryEntry
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    Symbol = asset.Symbol,
                    IconUrl = asset.IconUrl,
                    PriceHistoryEntries = asset.PriceHistory
                                            .OrderByDescending(h => h.Date)
                                            .Select(h => new PriceHistoryEntry {Date = h.Date, Price = h.Price })
                                            .Take(2)
                                            .ToList()
                })
                .ToListAsync(x),
                cancellationToken
                );

            cancellationToken.ThrowIfCancellationRequested(); 

            var viewModels = new List<CryptoAssetViewModel>();
            string currencyCode = _apiOptions.VsCurrency?.ToUpperInvariant() ?? "USD";

            foreach (var assetData in assetsWithPriceData!)
            {
                if (!PriceValidator.HasSufficientPriceHistoryForTrend(assetData.PriceHistoryEntries))
                {
                    _logger.LogDebug("Asset '{AssetName}' (ID: {AssetId}) has insufficient price history ({Count} entries). Skipping trend calculation.",
                         assetData.Name, assetData.Id, assetData.PriceHistoryEntries?.Count ?? 0);

                    if (assetData.PriceHistoryEntries == null || !assetData.PriceHistoryEntries.Any()) continue; 
                }

                var latestPriceEntry = assetData.PriceHistoryEntries.First();
                var previousPriceEntry = assetData.PriceHistoryEntries.Count > 1 ? assetData.PriceHistoryEntries.ElementAt(1) : null;

                var trend = CalculateTrendViewModel(latestPriceEntry.Price, previousPriceEntry?.Price);

                var lastUpdatedUtc = PriceValidator.EnsureUtc(latestPriceEntry.Date);

                viewModels.Add(new CryptoAssetViewModel
                {
                    Name = assetData.Name,
                    Symbol = assetData.Symbol,
                    CurrentPrice = latestPriceEntry.Price.ToString("0.################", CultureInfo.InvariantCulture),
                    Currency = currencyCode,
                    IconUrl = assetData.IconUrl,
                    LastUpdated = lastUpdatedUtc,
                    Trend = trend 
                });
            }

            _logger.LogInformation("Successfully fetched and processed {Count} crypto asset view models.", viewModels.Count);
            return viewModels;
        }
        private static TrendViewModel CalculateTrendViewModel(decimal latestPrice, decimal? previousPriceRaw)
        {
            if (previousPriceRaw == null)
            {
                return new TrendViewModel(TrendDirection.Neutral, null); 
            }

            decimal previousPrice = previousPriceRaw.Value;
            decimal percentageChange;
            string direction;

            if (previousPrice == 0m)
            {
                if (latestPrice > 0m)
                {
                    direction = TrendDirection.Up;
                    percentageChange = 100.0m; 
                }
                else if (latestPrice < 0m) 
                {
                    direction = TrendDirection.Down;
                    percentageChange = -100.0m;
                }
                else 
                {
                    direction = TrendDirection.Neutral;
                    percentageChange = 0m;
                }
            }
            else
            {
                percentageChange = ((latestPrice - previousPrice) / previousPrice) * 100m;

                if (latestPrice > previousPrice) direction = TrendDirection.Up;
                else if (latestPrice < previousPrice) direction = TrendDirection.Down;
                else direction = TrendDirection.Neutral; 
            }

            return new TrendViewModel(direction, Math.Round(percentageChange, 2));
        }

        // Tentative structure for CryptoPriceTracker.Api.Models.PriceDataPoint
        // public class PriceDataPoint
        // {
        //     public DateTime Date { get; set; }
        //     public decimal Price { get; set; }
        // }

        // Tentative structure for CryptoPriceTracker.Api.Models.CoinChartViewModel
        // public class CoinChartViewModel
        // {
        //     public string CoinName { get; set; }
        //     public string CoinSymbol { get; set; }
        //     public List<PriceDataPoint> PriceHistory { get; set; }
        // }

        public async Task<List<CoinChartViewModel>> GetTopNCoinsByPriceAsync(int count, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching top {Count} coins by latest price.", count);

            if (count <= 0)
            {
                _logger.LogWarning("Requested count for top N coins is zero or negative: {Count}. Returning empty list.", count);
                return new List<CoinChartViewModel>();
            }

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Step 1: Fetch all assets and their single latest price
            var assetsWithLatestPrice = await _dbContext.CryptoAssets
                .AsNoTracking()
                .Select(asset => new
                {
                    asset.Id,
                    asset.Name,
                    asset.Symbol,
                    LatestPriceEntry = asset.PriceHistory
                                        .OrderByDescending(ph => ph.Date)
                                        .Select(ph => new { ph.Price, ph.Date }) // Select only what's needed
                                        .FirstOrDefault()
                })
                .Where(asset => asset.LatestPriceEntry != null) // Ensure there is a price history
                .ToListAsync(cancellationToken);

            // Step 2: Sort by the latest price and take top N
            var topNAssets = assetsWithLatestPrice
                .OrderByDescending(asset => asset.LatestPriceEntry!.Price)
                .Take(count)
                .ToList();

            if (!topNAssets.Any())
            {
                _logger.LogInformation("No assets found with price history, or top N list is empty after sorting. Returning empty list.");
                return new List<CoinChartViewModel>();
            }
            
            _logger.LogInformation("Identified {TopNCount} assets to fetch detailed history for.", topNAssets.Count);

            var resultViewModels = new List<CoinChartViewModel>();

            foreach (var assetInfo in topNAssets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Step 3: Gather price history for the last 30 days for each top N coin
                // First, fetch raw data from DB
                var rawPriceHistoryForAsset = await _dbContext.CryptoPriceHistories
                    .AsNoTracking()
                    .Where(ph => ph.CryptoAssetId == assetInfo.Id && ph.Date >= thirtyDaysAgo)
                    .Select(ph => new { ph.Date, ph.Price }) // Select only necessary fields
                    .ToListAsync(cancellationToken);

                // Then, perform grouping and averaging in-memory
                var dailyPriceHistory = rawPriceHistoryForAsset
                    .GroupBy(h => h.Date.Date) // Group by the date part
                    .Select(g => new PriceDataPoint
                    {
                        Date = g.Key,
                        Price = g.Average(ph => ph.Price) // This now uses LINQ to Objects
                    })
                    .OrderBy(pdp => pdp.Date)
                    .ToList();
                
                if (!dailyPriceHistory.Any())
                {
                     _logger.LogDebug("No price history found in the last 30 days for asset {AssetName} (ID: {AssetId}). It will have an empty history list.", assetInfo.Name, assetInfo.Id);
                }

                resultViewModels.Add(new CoinChartViewModel
                {
                    CoinName = assetInfo.Name,
                    CoinSymbol = assetInfo.Symbol,
                    PriceHistory = dailyPriceHistory
                });
            }

            _logger.LogInformation("Successfully processed {Count} view models for top N coins by price.", resultViewModels.Count);
            return resultViewModels;
        }
    }
}