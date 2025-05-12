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
    }
}