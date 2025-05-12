using Microsoft.AspNetCore.Mvc;
using CryptoPriceTracker.Api.Services;
using CryptoPriceTracker.Api.Data;
using System.Threading;
using CryptoPriceTracker.Api.Models;

namespace CryptoPriceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/crypto")]
    public class CryptoController : ControllerBase
    {
        private readonly CryptoPriceService _service;
        private readonly ILogger<CryptoController> _logger;

        // Constructor with dependency injection of the service
        public CryptoController(CryptoPriceService service, ILogger<CryptoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// TODO: Implement logic to call the UpdatePricesAsync method from the service
        /// This endpoint should trigger a price update by fetching prices from the CoinGecko API
        /// and saving them in the database through the service logic.
        /// </summary>
        /// <returns>200 OK with a confirmation message once done</returns>
        [HttpPost("update-prices")]
        public async Task<IActionResult> UpdatePrices(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to initiate cryptocurrency price update.");

            try
            {
                await _service.UpdatePricesAsync(cancellationToken);

                string result = "Cryptocurrency price update process initiated successfully.";
                return Ok(result);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Price update operation was cancelled by the client.");
                return NoContent(); 
            }
            catch (Exception ex)
            {
                var errorMessage = "An unexpected error occurred while updating cryptocurrency prices.";
                _logger.LogError(ex, "{errorMessage}", errorMessage);
                return StatusCode(500, errorMessage);
            }
        }

        /// <summary>
        /// TODO: Implement an endpoint to return the latest prices per crypto asset.
        /// This will allow the frontend to display the most recent data saved in the database.
        /// </summary>
        /// <returns>A list of assets and their latest recorded price</returns>
        [HttpGet("latest-prices")]
        public async Task<IActionResult> GetLatestPrices(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Request received for latest crypto prices.");

                List<CryptoAssetViewModel> latestPrices = await _service.GetPricesAsync(cancellationToken);

                _logger.LogInformation("Successfully retrieved {Count} latest crypto prices.", latestPrices.Count);
                return Ok(latestPrices);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Operation to get latest prices was canceled.");
                return NoContent(); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching latest crypto prices.");
                return StatusCode(500, "An unexpected error occurred while processing your request.");
            }
        }
    }
}