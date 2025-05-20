document.addEventListener('DOMContentLoaded', function () {
    const canvas = document.getElementById('topCoinsChartCanvas');
    if (!canvas) {
        console.error('Chart canvas element not found!');
        // Display a user-friendly message on the page if the canvas is missing
        const chartContainer = document.querySelector('.chart-container'); // Or any other relevant container
        if (chartContainer) {
            chartContainer.innerHTML = '<p class="text-danger text-center">Chart canvas element (topCoinsChartCanvas) is missing from the page. Cannot render the chart.</p>';
        }
        return;
    }
    const ctx = canvas.getContext('2d');
    let topCoinsChart; // To store the chart instance

    const API_URL = '/api/crypto/top-coins-by-price-chart/10';

    // Predefined colors for chart lines
    const CHART_COLORS = [
        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
        '#FF9F40', '#C9CBCF', '#7BC225', '#F24726', '#0F8B8D',
        '#E91E63', '#2196F3', '#FFEB3B', '#009688', '#673AB7' // Added more colors
    ];

    // Function to display a loading message
    function showLoadingMessage() {
        if (canvas && canvas.parentElement) {
            // Clear previous content (like error messages) but keep the canvas
            const parent = canvas.parentElement;
            // Remove existing messages if any
            const existingMessage = parent.querySelector('.chart-message');
            if (existingMessage) {
                parent.removeChild(existingMessage);
            }
            const loadingMessage = document.createElement('p');
            loadingMessage.className = 'text-info text-center chart-message';
            loadingMessage.textContent = 'Loading chart data...';
            // Insert loading message before the canvas
            parent.insertBefore(loadingMessage, canvas);
        }
    }

    // Function to clear loading/error messages
    function clearMessages() {
        if (canvas && canvas.parentElement) {
            const parent = canvas.parentElement;
            const message = parent.querySelector('.chart-message');
            if (message) {
                parent.removeChild(message);
            }
        }
    }
    
    // Function to display error message
    function showErrorMessage(message) {
        clearMessages(); // Clear any existing messages first
        if (canvas && canvas.parentElement) {
            const parent = canvas.parentElement;
            canvas.style.display = 'none'; // Hide canvas on error
            const errorMessageElement = document.createElement('p');
            errorMessageElement.className = 'text-danger text-center chart-message';
            errorMessageElement.textContent = message;
            parent.appendChild(errorMessageElement); // Append error message
        } else {
            alert(message); // Fallback for critical errors where canvas parent isn't found
        }
    }


    async function fetchDataAndRenderChart() {
        showLoadingMessage();

        try {
            const response = await fetch(API_URL);
            if (!response.ok) {
                // Try to get error message from API response body if available
                let apiErrorMsg = `API Error: ${response.status} ${response.statusText}`;
                try {
                    const errorData = await response.json();
                    if (errorData && errorData.message) {
                       apiErrorMsg = `API Error: ${errorData.message} (Status: ${response.status})`;
                    } else if (typeof errorData === 'string' && errorData.length > 0 && errorData.length < 200) {
                        apiErrorMsg = `API Error: ${errorData} (Status: ${response.status})`;
                    }
                } catch(e) { /* Ignore if error response is not JSON */ }
                throw new Error(apiErrorMsg);
            }
            const coinData = await response.json();

            clearMessages(); // Clear loading message

            if (!coinData || coinData.length === 0) {
                showErrorMessage('No chart data available for the top coins. The list might be empty or the data is still being processed.');
                return;
            }
            
            canvas.style.display = 'block'; // Ensure canvas is visible

            // Process data for Chart.js
            // 1. Collect all unique dates and sort them
            const allDates = new Set();
            coinData.forEach(coin => {
                coin.priceHistory.forEach(ph => {
                    // Standardize date to YYYY-MM-DD for reliable sorting and mapping
                    allDates.add(new Date(ph.date).toISOString().split('T')[0]);
                });
            });
            // Sort dates chronologically
            const sortedUniqueDates = Array.from(allDates).sort((a, b) => new Date(a) - new Date(b));

            // 2. Create datasets
            const datasets = coinData.map((coin, index) => {
                // Create a map of standardized date strings to prices for efficient lookup
                const priceMap = new Map(coin.priceHistory.map(ph => 
                    [new Date(ph.date).toISOString().split('T')[0], ph.price]
                ));
                
                // For each unique date, get the price or null if not available for this coin
                const dataPoints = sortedUniqueDates.map(dateStr => priceMap.get(dateStr) || null);

                return {
                    label: `${coin.coinName} (${coin.coinSymbol.toUpperCase()})`,
                    data: dataPoints,
                    borderColor: CHART_COLORS[index % CHART_COLORS.length],
                    backgroundColor: CHART_COLORS[index % CHART_COLORS.length] + 'B3', // Slightly more opaque for points
                    fill: false,
                    tension: 0.1, // For slightly curved lines
                    pointRadius: 3,
                    pointHoverRadius: 6, // Slightly larger hover
                    borderWidth: 2 // Thicker line
                };
            });

            const chartDataConfig = {
                labels: sortedUniqueDates, // These are YYYY-MM-DD strings
                datasets: datasets
            };

            const chartOptions = {
                responsive: true,
                maintainAspectRatio: true, 
                scales: {
                    x: {
                        title: { display: true, text: 'Date', font: { weight: 'bold' } },
                        ticks: { 
                            autoSkip: true, 
                            maxTicksLimit: 15, 
                            callback: function(value, index, values) {
                                // 'value' is the index here, getLabelForValue converts it to the actual label (YYYY-MM-DD)
                                const dateLabel = this.getLabelForValue(value); 
                                const date = new Date(dateLabel + 'T00:00:00Z'); // Ensure UTC interpretation if dates are YYYY-MM-DD
                                // Format to MM/DD for display
                                return `${String(date.getUTCMonth() + 1).padStart(2, '0')}/${String(date.getUTCDate()).padStart(2, '0')}`;
                            }
                        }
                    },
                    y: {
                        title: { display: true, text: 'Price (USD)', font: { weight: 'bold' } },
                        beginAtZero: false,
                        ticks: {
                            callback: function(value, index, values) {
                                return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
                            }
                        }
                    }
                },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            boxWidth: 15, // Slightly smaller box
                            padding: 15,  // Adjust padding
                            usePointStyle: true, // Use point style for legend items
                            font: { size: 13 }
                        }
                    },
                    tooltip: {
                        mode: 'index', // Show tooltips for all datasets at that index
                        intersect: false, // Don't require hover directly over point
                        backgroundColor: 'rgba(0, 0, 0, 0.8)', // Darker tooltip
                        titleFont: { size: 14, weight: 'bold' },
                        bodyFont: { size: 12 },
                        padding: 10,
                        callbacks: {
                            title: function(tooltipItems) {
                                // Format the title (date)
                                if (tooltipItems.length > 0) {
                                    const dateLabel = tooltipItems[0].label; // This is YYYY-MM-DD
                                    const date = new Date(dateLabel + 'T00:00:00Z'); // Ensure UTC interpretation
                                    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' });
                                }
                                return '';
                            },
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                if (context.parsed.y !== null) {
                                    // Format as currency
                                    label += new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(context.parsed.y);
                                } else {
                                    label += 'N/A'; // For missing data points
                                }
                                return label;
                            }
                        }
                    }
                },
                interaction: { // Settings for how chart interacts with mouse/touch
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                }
            };
            
            if (topCoinsChart) {
                topCoinsChart.destroy(); // Destroy existing chart instance before creating new one
            }

            topCoinsChart = new Chart(ctx, {
                type: 'line',
                data: chartDataConfig,
                options: chartOptions
            });

        } catch (error) {
            console.error('Failed to fetch or render chart:', error);
            showErrorMessage(`Error loading chart: ${error.message}. Please try refreshing the page or check the console for more details.`);
        }
    }

    fetchDataAndRenderChart();
});
