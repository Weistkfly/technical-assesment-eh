namespace CryptoPriceTracker.Api.Models
{
    public class TrendViewModel 
    {
        public TrendViewModel(string direction, decimal? percentageChange = null)
        {
            Direction = direction;
            PercentageChange = percentageChange;
        }

        public string Direction { get; set; }
        public decimal? PercentageChange { get; set; } 
    }
}
