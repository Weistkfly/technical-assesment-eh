using CryptoPriceTracker.Api.Models;
using CryptoPriceTracker.Api.Validators;

namespace CryptoPriceTracker.Tests
{
    public class CryptoPriceServiceTest
    {
        private CoinGeckoApiOptions CreateValidApiOptions() => new CoinGeckoApiOptions
        {
            UserAgent = "TestAgent/1.0",
            BaseUrl = "https://api.coingecko.com/api/v3",
            MarketDataEndpoint = "coins/markets",
            VsCurrency = "usd",
            PerPage = 100,
            MaxPage = 10
        };

        #region ValidateApiOptions Tests

        [Fact]
        public void ValidateApiOptions_ValidOptions_DoesNotThrow()
        {
            // Arrange
            var options = CreateValidApiOptions();

            // Act
            var exception = Record.Exception(() => PriceValidator.ValidateApiOptions(options));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateApiOptions_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            CoinGeckoApiOptions? options = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PriceValidator.ValidateApiOptions(options!)); 
            Assert.Equal("apiOptions", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ValidateApiOptions_InvalidUserAgent_ThrowsArgumentException(string? userAgent)
        {
            // Arrange
            var options = CreateValidApiOptions();
            options.UserAgent = userAgent;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => PriceValidator.ValidateApiOptions(options));
            Assert.Contains("UserAgent must be configured", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ValidateApiOptions_InvalidBaseUrl_ThrowsArgumentException(string? baseUrl)
        {
            // Arrange
            var options = CreateValidApiOptions();
            options.BaseUrl = baseUrl;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => PriceValidator.ValidateApiOptions(options));
            Assert.Contains("BaseUrl must be configured", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ValidateApiOptions_InvalidMarketDataEndpoint_ThrowsArgumentException(string? endpoint)
        {
            // Arrange
            var options = CreateValidApiOptions();
            options.MarketDataEndpoint = endpoint;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => PriceValidator.ValidateApiOptions(options));
            Assert.Contains("MarketDataEndpoint must be configured", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ValidateApiOptions_InvalidVsCurrency_ThrowsArgumentException(string? currency)
        {
            // Arrange
            var options = CreateValidApiOptions();
            options.VsCurrency = currency;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => PriceValidator.ValidateApiOptions(options));
            Assert.Contains("VsCurrency must be configured", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidateApiOptions_InvalidPerPage_ThrowsArgumentOutOfRangeException(int perPage)
        {
            // Arrange
            var options = CreateValidApiOptions();
            options.PerPage = perPage;

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => PriceValidator.ValidateApiOptions(options));
            Assert.Equal("PerPage", ex.ParamName); 
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidateApiOptions_InvalidMaxPage_ThrowsArgumentOutOfRangeException(int maxPage)
        {
            // Arrange
            var options = CreateValidApiOptions();
            options.MaxPage = maxPage;

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => PriceValidator.ValidateApiOptions(options));
            Assert.Equal("MaxPage", ex.ParamName);
        }

        #endregion

        #region AreApiAssetsRetrieved Tests

        [Fact]
        public void AreApiAssetsRetrieved_NullArray_ReturnsFalse()
        {
            // Arrange
            CryptoAssetResponse[]? assets = null;

            // Act
            var result = PriceValidator.AreApiAssetsRetrieved(assets);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreApiAssetsRetrieved_EmptyArray_ReturnsFalse()
        {
            // Arrange
            var assets = Array.Empty<CryptoAssetResponse>(); 

            // Act
            var result = PriceValidator.AreApiAssetsRetrieved(assets);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreApiAssetsRetrieved_ArrayWithItems_ReturnsTrue()
        {
            // Arrange
            var assets = new CryptoAssetResponse[]
            {
                new CryptoAssetResponse { id = "bitcoin" }
            };

            // Act
            var result = PriceValidator.AreApiAssetsRetrieved(assets);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region IsValidCryptoAssetResponse Tests

        [Fact]
        public void IsValidCryptoAssetResponse_NullResponse_ReturnsFalse()
        {
            // Arrange
            CryptoAssetResponse? response = null;

            // Act
            var result = PriceValidator.IsValidCryptoAssetResponse(response);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValidCryptoAssetResponse_InvalidId_ReturnsFalse(string? id)
        {
            // Arrange
            var response = new CryptoAssetResponse { id = id };

            // Act
            var result = PriceValidator.IsValidCryptoAssetResponse(response);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCryptoAssetResponse_ValidResponse_ReturnsTrue()
        {
            // Arrange
            var response = new CryptoAssetResponse { id = "ethereum" };

            // Act
            var result = PriceValidator.IsValidCryptoAssetResponse(response);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region EnsureUtc Tests

        [Fact]
        public void EnsureUtc_Nullable_NullInput_ReturnsDefaultValue()
        {
            // Arrange
            DateTime? inputDate = null;
            var defaultValue = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var result = PriceValidator.EnsureUtc(inputDate, defaultValue);

            // Assert
            Assert.Equal(defaultValue, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void EnsureUtc_Nullable_UtcInput_ReturnsSameUtcValue()
        {
            // Arrange
            var inputDate = new DateTime(2024, 5, 12, 10, 0, 0, DateTimeKind.Utc);
            var defaultValue = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);


            // Act
            var result = PriceValidator.EnsureUtc(inputDate, defaultValue);

            // Assert
            Assert.Equal(inputDate, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void EnsureUtc_Nullable_LocalInput_ReturnsConvertedUtcValue()
        {
            // Arrange
            var localTime = new DateTime(2024, 5, 12, 10, 0, 0, DateTimeKind.Local);
            var expectedUtcTime = localTime.ToUniversalTime();
            var defaultValue = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = PriceValidator.EnsureUtc(localTime, defaultValue);

            // Assert
            Assert.Equal(expectedUtcTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void EnsureUtc_Nullable_UnspecifiedInput_ReturnsSpecifiedAsUtcValue()
        {
            // Arrange
            var unspecifiedTime = new DateTime(2024, 5, 12, 10, 0, 0, DateTimeKind.Unspecified);
            var expectedUtcTime = DateTime.SpecifyKind(unspecifiedTime, DateTimeKind.Utc);
            var defaultValue = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);


            // Act
            var result = PriceValidator.EnsureUtc(unspecifiedTime, defaultValue);

            // Assert
            Assert.Equal(expectedUtcTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }


        [Fact]
        public void EnsureUtc_NonNull_UtcInput_ReturnsSameUtcValue()
        {
            // Arrange
            var inputDate = new DateTime(2024, 5, 12, 10, 0, 0, DateTimeKind.Utc);

            // Act
            var result = PriceValidator.EnsureUtc(inputDate);

            // Assert
            Assert.Equal(inputDate, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void EnsureUtc_NonNull_LocalInput_ReturnsConvertedUtcValue()
        {
            // Arrange
            var localTime = new DateTime(2024, 5, 12, 10, 0, 0, DateTimeKind.Local);
            var expectedUtcTime = localTime.ToUniversalTime();

            // Act
            var result = PriceValidator.EnsureUtc(localTime);

            // Assert
            Assert.Equal(expectedUtcTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void EnsureUtc_NonNull_UnspecifiedInput_ReturnsSpecifiedAsUtcValue()
        {
            // Arrange
            var unspecifiedTime = new DateTime(2024, 5, 12, 10, 0, 0, DateTimeKind.Unspecified);
            var expectedUtcTime = DateTime.SpecifyKind(unspecifiedTime, DateTimeKind.Utc);

            // Act
            var result = PriceValidator.EnsureUtc(unspecifiedTime);

            // Assert
            Assert.Equal(expectedUtcTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        #endregion

        #region IsPriceUpdateNeeded Tests

        [Fact]
        public void IsPriceUpdateNeeded_NullLastKnownDate_ReturnsTrue()
        {
            // Arrange
            DateTime? lastKnownDate = null;
            var effectiveApiTime = DateTime.UtcNow;

            // Act
            var result = PriceValidator.IsPriceUpdateNeeded(lastKnownDate, effectiveApiTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPriceUpdateNeeded_ApiTimeAfterLastKnown_ReturnsTrue()
        {
            // Arrange
            var lastKnownDate = new DateTime(2024, 5, 12, 12, 0, 0, DateTimeKind.Utc);
            var effectiveApiTime = lastKnownDate.AddSeconds(1); 

            // Act
            var result = PriceValidator.IsPriceUpdateNeeded(lastKnownDate, effectiveApiTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPriceUpdateNeeded_ApiTimeAfterLastKnown_DifferentKinds_ReturnsTrue()
        {
            // Arrange
            var lastKnownDateLocal = new DateTime(2024, 5, 12, 8, 0, 0, DateTimeKind.Local); 
            var lastKnownDateUtc = lastKnownDateLocal.ToUniversalTime(); 
            var effectiveApiTimeUnspecified = new DateTime(2024, 5, 12, 12, 0, 1, DateTimeKind.Unspecified); 

            // Act
            var result = PriceValidator.IsPriceUpdateNeeded(lastKnownDateLocal, effectiveApiTimeUnspecified);

            // Assert
            Assert.True(result);
        }


        [Fact]
        public void IsPriceUpdateNeeded_ApiTimeEqualsLastKnown_ReturnsFalse()
        {
            // Arrange
            var lastKnownDate = new DateTime(2024, 5, 12, 12, 0, 0, DateTimeKind.Utc);
            var effectiveApiTime = lastKnownDate;

            // Act
            var result = PriceValidator.IsPriceUpdateNeeded(lastKnownDate, effectiveApiTime);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPriceUpdateNeeded_ApiTimeBeforeLastKnown_ReturnsFalse()
        {
            // Arrange
            var lastKnownDate = new DateTime(2024, 5, 12, 12, 0, 0, DateTimeKind.Utc);
            var effectiveApiTime = lastKnownDate.AddSeconds(-1); 

            // Act
            var result = PriceValidator.IsPriceUpdateNeeded(lastKnownDate, effectiveApiTime);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPriceUpdateNeeded_ApiTimeBeforeLastKnown_DifferentKinds_ReturnsFalse()
        {
            // Arrange
            // Test EnsureUtc is working correctly within the method
            var lastKnownDateUnspecified = new DateTime(2024, 5, 12, 13, 0, 0, DateTimeKind.Unspecified); 
            var effectiveApiTimeLocal = new DateTime(2024, 5, 12, 8, 59, 59, DateTimeKind.Local); 

            // Act
            var result = PriceValidator.IsPriceUpdateNeeded(lastKnownDateUnspecified, effectiveApiTimeLocal);

            // Assert
            Assert.False(result);
        }


        #endregion

        #region HasSufficientPriceHistoryForTrend Tests

        [Fact]
        public void HasSufficientPriceHistoryForTrend_NullEntries_ReturnsFalse()
        {
            // Arrange
            List<PriceHistoryEntry>? entries = null;

            // Act
            var result = PriceValidator.HasSufficientPriceHistoryForTrend(entries);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasSufficientPriceHistoryForTrend_EmptyEntries_ReturnsFalse()
        {
            // Arrange
            var entries = Enumerable.Empty<PriceHistoryEntry>().ToList();

            // Act
            var result = PriceValidator.HasSufficientPriceHistoryForTrend(entries);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasSufficientPriceHistoryForTrend_EntriesWithItems_ReturnsTrue()
        {
            // Arrange
            var entries = new List<PriceHistoryEntry>
            {
                new() { Date = DateTime.UtcNow, Price = 5000 }
            };

            // Act
            var result = PriceValidator.HasSufficientPriceHistoryForTrend(entries);

            // Assert
            Assert.True(result);
        }

        #endregion

        // --- Tests for GetTopNCoinsByPriceAsync ---

        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }

        private CryptoPriceService CreateService(ApplicationDbContext context, IOptions<CoinGeckoApiOptions>? apiOptions = null, ILogger<CryptoPriceService>? logger = null, CacheService? cacheService = null, HttpClient? httpClient = null)
        {
            var options = apiOptions ?? Options.Create(new CoinGeckoApiOptions 
            {
                UserAgent = "TestAgent/1.0", BaseUrl = "http://dummy.com", MarketDataEndpoint = "dummy", 
                VsCurrency = "usd", PerPage = 10, MaxPage = 1 
            });
            var log = logger ?? new Mock<ILogger<CryptoPriceService>>().Object;
            
            var cacheOpts = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache distributedCache = new MemoryDistributedCache(cacheOpts);
            var cache = cacheService ?? new CacheService(distributedCache);
            
            var client = httpClient ?? new HttpClient(new Mock<HttpMessageHandler>().Object);

            return new CryptoPriceService(context, client, options, log, cache);
        }

        [Fact]
        public async Task Test_GetTopNCoins_ReturnsCorrectlySortedCoins()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var service = CreateService(context);
            var now = DateTime.UtcNow;

            var assets = new List<CryptoAsset>
            {
                new CryptoAsset { ExternalId = "coin1", Name = "Coin One", Symbol = "CN1", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now.AddHours(-1), Price = 100 } } },
                new CryptoAsset { ExternalId = "coin2", Name = "Coin Two", Symbol = "CN2", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now.AddHours(-1), Price = 300 } } },
                new CryptoAsset { ExternalId = "coin3", Name = "Coin Three", Symbol = "CN3", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now.AddHours(-1), Price = 200 } } },
                new CryptoAsset { ExternalId = "coin4", Name = "Coin Four", Symbol = "CN4", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now.AddHours(-1), Price = 50 } } }
            };
            await context.CryptoAssets.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTopNCoinsByPriceAsync(3);

            // Assert
            result.Should().HaveCount(3);
            result.Select(c => c.CoinName).Should().ContainInOrder("Coin Two", "Coin Three", "Coin One");
            result.ForEach(r =>
            {
                r.CoinName.Should().NotBeNullOrEmpty();
                r.CoinSymbol.Should().NotBeNullOrEmpty();
                var originalAsset = assets.First(a => a.Name == r.CoinName);
                r.CoinSymbol.Should().Be(originalAsset.Symbol);
            });
        }

        [Fact]
        public async Task Test_GetTopNCoins_ReturnsAllCoinsIfCountIsLarger()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var service = CreateService(context);
            var now = DateTime.UtcNow;

            var assets = new List<CryptoAsset>
            {
                new CryptoAsset { ExternalId = "coin1", Name = "Coin One", Symbol = "CN1", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now, Price = 100 } } },
                new CryptoAsset { ExternalId = "coin2", Name = "Coin Two", Symbol = "CN2", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now, Price = 300 } } }
            };
            await context.CryptoAssets.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTopNCoinsByPriceAsync(5);

            // Assert
            result.Should().HaveCount(2);
            result.Select(c => c.CoinName).Should().ContainInOrder("Coin Two", "Coin One");
        }

        [Fact]
        public async Task Test_GetTopNCoins_ReturnsEmptyListIfNoAssetsHavePrices()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var service = CreateService(context);

            var assets = new List<CryptoAsset>
            {
                new CryptoAsset { ExternalId = "coin1", Name = "Coin One", Symbol = "CN1", PriceHistory = new List<CryptoPriceHistory>() }, // No history
                new CryptoAsset { ExternalId = "coin2", Name = "Coin Two", Symbol = "CN2" } // Null history
            };
            await context.CryptoAssets.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTopNCoinsByPriceAsync(3);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Test_GetTopNCoins_ReturnsEmptyListForInvalidCount(int count)
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var service = CreateService(context);
             var now = DateTime.UtcNow;
            // Add some data to ensure it's the count check that's working, not lack of data
            var assets = new List<CryptoAsset> { new CryptoAsset { ExternalId = "c1", Name = "C1", Symbol="S1", PriceHistory = new List<CryptoPriceHistory> { new CryptoPriceHistory { Date = now, Price = 100 } } } };
            await context.CryptoAssets.AddRangeAsync(assets);
            await context.SaveChangesAsync();


            // Act
            var result = await service.GetTopNCoinsByPriceAsync(count);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Test_GetTopNCoins_PriceHistoryIsLimitedTo30DaysAndAggregatedDaily()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var service = CreateService(context);
            var now = DateTime.UtcNow; // Use a fixed "now" for predictable date calculations

            var asset = new CryptoAsset 
            { 
                ExternalId = "coin1", Name = "Coin One", Symbol = "CN1", 
                PriceHistory = new List<CryptoPriceHistory>
                {
                    // Within 30 days - Day 1 (2 entries, should be averaged)
                    new CryptoPriceHistory { Date = now.AddDays(-1).Date.AddHours(10), Price = 100 },
                    new CryptoPriceHistory { Date = now.AddDays(-1).Date.AddHours(12), Price = 110 }, // Avg = 105
                    // Within 30 days - Day 2
                    new CryptoPriceHistory { Date = now.AddDays(-2).Date.AddHours(10), Price = 90 },
                    // Within 30 days - Day 28
                    new CryptoPriceHistory { Date = now.AddDays(-28).Date.AddHours(10), Price = 80 },
                    // Outside 30 days (should be excluded)
                    new CryptoPriceHistory { Date = now.AddDays(-31).Date.AddHours(10), Price = 70 },
                    new CryptoPriceHistory { Date = now.AddDays(-35).Date.AddHours(10), Price = 60 },
                     // Today's price (will be included in top N selection)
                    new CryptoPriceHistory { Date = now.AddHours(-2), Price = 120 } // This is the latest price for sorting
                }
            };
            await context.CryptoAssets.AddAsync(asset);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTopNCoinsByPriceAsync(1);

            // Assert
            result.Should().HaveCount(1);
            var coinChart = result.First();
            coinChart.CoinName.Should().Be("Coin One");

            // Expected dates: -28, -2, -1, today (relative to now.Date, if 'now' is early in the day, today might not be fully passed)
            // The latest price is used for sorting, then history is fetched.
            // The history for chart should be from -28 days ago, -2 days ago, -1 day ago. Today's price history might be tricky if it's same day as 'now'.
            // The service logic groups by Date.Date.
            // Price points: (now.AddDays(-28), 80), (now.AddDays(-2), 90), (now.AddDays(-1), 105)
            // The latest price used for sorting is 120 (from now.AddHours(-2))
            
            // Filter history to be within the last 30 days from 'now'
            var thirtyDaysAgo = now.AddDays(-30).Date;
            var expectedHistoryPoints = asset.PriceHistory
                .Where(ph => ph.Date.Date >= thirtyDaysAgo && ph.Date.Date <= now.Date) // Consider up to current date
                .GroupBy(ph => ph.Date.Date)
                .Select(g => new { Date = g.Key, Price = g.Average(p => p.Price) })
                .OrderBy(p => p.Date)
                .ToList();

            coinChart.PriceHistory.Should().HaveCount(expectedHistoryPoints.Count);
            
            // Check specific points, especially the aggregated one
            var day1Aggregated = coinChart.PriceHistory.FirstOrDefault(ph => ph.Date.Date == now.AddDays(-1).Date);
            day1Aggregated.Should().NotBeNull();
            day1Aggregated!.Price.Should().BeApproximately(105, 0.001m);

            var day28 = coinChart.PriceHistory.FirstOrDefault(ph => ph.Date.Date == now.AddDays(-28).Date);
            day28.Should().NotBeNull();
            day28!.Price.Should().Be(80);

            // Ensure dates are sorted
            coinChart.PriceHistory.Select(ph => ph.Date).Should().BeInAscendingOrder();

            // Ensure no data older than 30 days
            coinChart.PriceHistory.Should().OnlyContain(ph => ph.Date >= thirtyDaysAgo);
        }

        [Fact]
        public async Task Test_GetTopNCoins_HandlesAssetsWithNoHistoryInLast30Days()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var service = CreateService(context);
            var now = DateTime.UtcNow;

            var assets = new List<CryptoAsset>
            {
                // Asset 1: High latest price, but all history is old
                new CryptoAsset 
                { 
                    ExternalId = "oldHistoryCoin", Name = "Old History Coin", Symbol = "OHC", 
                    PriceHistory = new List<CryptoPriceHistory> 
                    { 
                        new CryptoPriceHistory { Date = now.AddDays(-40), Price = 1000 }, // Latest price, but old
                        new CryptoPriceHistory { Date = now.AddDays(-45), Price = 900 } 
                    } 
                },
                // Asset 2: Lower latest price, but recent history
                new CryptoAsset 
                { 
                    ExternalId = "recentHistoryCoin", Name = "Recent History Coin", Symbol = "RHC", 
                    PriceHistory = new List<CryptoPriceHistory> 
                    { 
                        new CryptoPriceHistory { Date = now.AddDays(-1), Price = 500 }, // Latest price
                        new CryptoPriceHistory { Date = now.AddDays(-2), Price = 490 }  
                    } 
                }
            };
            await context.CryptoAssets.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTopNCoinsByPriceAsync(2); // Get top 2

            // Assert
            result.Should().HaveCount(2); // Both coins should be returned based on their latest prices
            
            // Coins should be sorted by their *absolute* latest price regardless of its age
            result.Select(c => c.CoinName).Should().ContainInOrder("Old History Coin", "Recent History Coin");

            var oldHistoryCoinChart = result.FirstOrDefault(c => c.CoinName == "Old History Coin");
            oldHistoryCoinChart.Should().NotBeNull();
            oldHistoryCoinChart!.PriceHistory.Should().BeEmpty(); // Because its history is older than 30 days

            var recentHistoryCoinChart = result.FirstOrDefault(c => c.CoinName == "Recent History Coin");
            recentHistoryCoinChart.Should().NotBeNull();
            recentHistoryCoinChart!.PriceHistory.Should().HaveCount(2); // It has recent history
            recentHistoryCoinChart!.PriceHistory.First().Price.Should().Be(490); // oldest of recent
            recentHistoryCoinChart!.PriceHistory.Last().Price.Should().Be(500); // newest of recent
        }
    }
}