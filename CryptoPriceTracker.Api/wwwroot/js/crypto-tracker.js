// --- CONFIGURATION ---
const CONFIG = {
    API_ENDPOINTS: {
        latestPrices: "/api/crypto/latest-prices",
        updatePrices: "/api/crypto/update-prices"
    },
    TOAST_DURATION: 3500,
    TOAST_LOADING_DURATION: 2000,
    TOAST_ERROR_DURATION: 5000,
    DEBOUNCE_DELAY: 300,
    ITEMS_PER_PAGE: 12, 
    ITEMS_PER_PAGE_OPTIONS: [12, 24, 48, 96] 
};

// --- DOM ELEMENT REFERENCES ---
const updateBtn = document.getElementById("updateBtn");
const btnText = updateBtn?.querySelector(".btn-text"); 
const btnSpinner = updateBtn?.querySelector(".btn-spinner");
const cryptoContainer = document.getElementById("cryptoGridContainer");
const searchInput = document.getElementById("searchInput");
const sortSelect = document.getElementById("sortSelect");
const toggleThemeBtn = document.getElementById("toggleThemeBtn");
const toastContainer = document.getElementById("toastContainer");
const paginationContainer = document.getElementById("paginationContainer");

// --- STATE VARIABLES ---
let currentCryptoData = []; 
let searchDebounceTimer; 
let currentPage = 1; 
let itemsPerPage = CONFIG.ITEMS_PER_PAGE; 

// --- UTILITY FUNCTIONS ---
function debounce(func, delay) {
    let timeoutId;
    return function (...args) {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => {
            func.apply(this, args);
        }, delay);
    };
}

// --- UI UPDATE FUNCTIONS ---

function showToast(message, type = 'info', duration = CONFIG.TOAST_DURATION) {
    if (!toastContainer) {
        console.error("Toast container element not found!");
        return;
    }
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`; 
    toast.setAttribute('role', 'alert'); 
    toast.setAttribute('aria-live', type === 'danger' ? 'assertive' : 'polite');
    toast.textContent = message;

    toastContainer.appendChild(toast);
    void toast.offsetWidth;
    toast.classList.add('show');

    setTimeout(() => {
        toast.classList.remove('show');
        toast.addEventListener('transitionend', () => {
            if (toast.parentNode === toastContainer) {
                toastContainer.removeChild(toast);
            }
        }, { once: true }); 

    }, duration);
}

function formatTrend(trendData) {
    let trendHtml = `<span class="trend-neutral">N/A</span>`;
    let trendPercentageForSort = null; 

    if (trendData) {
        const { direction, percentageChange } = trendData;
        let trendClass = "trend-neutral";
        let trendSymbol = ""; 
        let sign = "";

        if (direction === "up") {
            trendClass = "trend-up";
            trendSymbol = "▲ "; 
            sign = "+";
        } else if (direction === "down") {
            trendClass = "trend-down";
            trendSymbol = "▼ "; 
        }

        if (typeof percentageChange === 'number' && !isNaN(percentageChange)) {
            trendHtml = `<span class="${trendClass}">${trendSymbol}${sign}${percentageChange.toFixed(2)}%</span>`;
            trendPercentageForSort = percentageChange;
        } else if (direction === "up" || direction === "down") {
            trendHtml = `<span class="${trendClass}">${trendSymbol}</span>`;
            trendPercentageForSort = direction === "up" ? Infinity : -Infinity;
        }
    }
    return { html: trendHtml, percentage: trendPercentageForSort };
}

function renderCryptoCards(dataToRender) {
    if (!cryptoContainer) {
        console.error("Crypto container element not found!");
        return;
    }
    cryptoContainer.innerHTML = "";
    const fragment = document.createDocumentFragment();

    if (dataToRender && dataToRender.length > 0) {
        dataToRender.forEach((asset, index) => {
            const cardWrapper = document.createElement("article"); 
            cardWrapper.className = "crypto-card-wrapper";
            const cardId = `crypto-card-${asset.symbol?.toLowerCase() || index}`; 
            cardWrapper.setAttribute('aria-labelledby', `${cardId}-name`); 

            const { html: trendHtml, percentage: trendPercentageValue } = formatTrend(asset.trend);

            const lastUpdatedLocal = asset.lastUpdated
                ? new Date(asset.lastUpdated).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
                : "N/A";

            const priceStringFromAPI = (asset.currentPrice !== null && asset.currentPrice !== undefined)
                ? String(asset.currentPrice) 
                : "N/A";

            const priceForSort = parseFloat(String(asset.currentPrice || '').replace(/,/g, ''));

            let formattedDisplayPrice;
            if (priceStringFromAPI === "N/A" || isNaN(priceForSort)) {
                formattedDisplayPrice = "N/A";
            } else {
                const numberToFormat = priceForSort;
                let minDigits = 2;
                let maxDigits = 2;
                if (numberToFormat > 0 && numberToFormat < 1) {
                    const decimalPart = priceStringFromAPI.split('.')[1] || '';
                    minDigits = Math.min(8, decimalPart.length || 2); 
                    maxDigits = 8;
                } else if (numberToFormat >= 10000) {
                    minDigits = 0;
                    maxDigits = 0;
                }

                formattedDisplayPrice = numberToFormat.toLocaleString(undefined, {
                    style: 'currency',
                    currency: asset.currency || 'USD',
                    minimumFractionDigits: minDigits,
                    maximumFractionDigits: maxDigits
                });
            }
            const currencyCode = asset.currency ? asset.currency.toUpperCase() : "";
            const currencyCodeHtml = currencyCode ? `<span class="crypto-price-currency-code">(${currencyCode})</span>` : "";

            cardWrapper.innerHTML = `
                <div class="crypto-card">
                    <div class="crypto-header">
                        <img src="${asset.iconUrl || 'https://placehold.co/40x40/eeeeee/777777?text=?'}"
                             alt=""
                             class="crypto-icon"
                             onerror="this.onerror=null; this.src='https://placehold.co/40x40/eeeeee/777777?text=?';">
                        <div class="crypto-name-symbol">
                            <h3 class="crypto-name" id="${cardId}-name">${asset.name || 'Unknown'}</h3>
                            <span class="crypto-symbol">(${(asset.symbol ? asset.symbol.toUpperCase() : 'N/A')})</span>
                        </div>
                    </div>
                    <div class="crypto-main-details">
                        <p class="crypto-price">${formattedDisplayPrice} ${currencyCodeHtml}</p>
                        <p class="crypto-change"><strong>Trend:</strong> ${trendHtml}</p>
                    </div>
                    <div class="crypto-footer-details">
                         <p><small><strong>Last Updated:</strong> ${lastUpdatedLocal}</small></p>
                    </div>
                </div>
            `;
            fragment.appendChild(cardWrapper);
        });
        cryptoContainer.appendChild(fragment); 
    } else {
        const message = searchInput.value.trim()
            ? `No results found for "${searchInput.value}".`
            : "No cryptocurrency data available to display.";
        cryptoContainer.innerHTML = `<p class="col-12 text-center p-5">${message}</p>`;
        if (searchInput.value.trim()) {
            showToast(message, "info");
        }
    }
}

function renderPaginationControls(totalItems) {
    if (!paginationContainer) {
        console.error("Pagination container element not found!");
        return;
    }
    paginationContainer.innerHTML = ""; 

    const totalPages = Math.ceil(totalItems / itemsPerPage);
    const startItem = totalItems === 0 ? 0 : (currentPage - 1) * itemsPerPage + 1;
    const endItem = Math.min(currentPage * itemsPerPage, totalItems);

    const itemsPerPageDiv = document.createElement("div");
    itemsPerPageDiv.classList.add("pagination-items-per-page");

    const ippLabel = document.createElement("label");
    ippLabel.htmlFor = "itemsPerPageSelect";
    ippLabel.textContent = "Show:";
    itemsPerPageDiv.appendChild(ippLabel);

    const ippSelect = document.createElement("select");
    ippSelect.id = "itemsPerPageSelect";
    ippSelect.classList.add("form-select"); 
    ippSelect.setAttribute("aria-label", "Select number of items per page");
    CONFIG.ITEMS_PER_PAGE_OPTIONS.forEach(option => {
        const opt = document.createElement("option");
        opt.value = option;
        opt.textContent = option;
        if (option === itemsPerPage) opt.selected = true;
        ippSelect.appendChild(opt);
    });
    ippSelect.addEventListener("change", (e) => {
        itemsPerPage = parseInt(e.target.value, 10);
        currentPage = 1; 
        filterAndSortAndRender(); 
    });
    itemsPerPageDiv.appendChild(ippSelect);

    const paginationInfo = document.createElement("div");
    paginationInfo.classList.add("pagination-info");
    paginationInfo.textContent = totalItems > 0
        ? `Showing ${startItem} - ${endItem} of ${totalItems}`
        : "No items to show";
    paginationContainer.appendChild(paginationInfo); 

    if (totalPages <= 1) {
        paginationContainer.appendChild(itemsPerPageDiv); 
        return; 
    }

    const paginationNavButtons = document.createElement("div");
    paginationNavButtons.classList.add("pagination-nav-buttons");

    const createPageButton = (pageNumber, text = pageNumber, ariaLabel = `Go to page ${pageNumber}`, isDisabled = false, isActive = false, isEllipsis = false) => {
        const button = document.createElement(isEllipsis ? "span" : "button");
        button.classList.add(isEllipsis ? "page-ellipsis" : "page-btn");
        button.innerHTML = text; 
        if (!isEllipsis) {
            button.setAttribute("aria-label", ariaLabel);
            button.disabled = isDisabled;
            if (isActive) {
                button.classList.add("active");
                button.setAttribute("aria-current", "page");
            }
            button.addEventListener("click", () => {
                currentPage = pageNumber;
                filterAndSortAndRender();
            });
        } else {
            button.setAttribute("aria-hidden", "true"); 
        }
        return button;
    };

    paginationNavButtons.appendChild(
        createPageButton(currentPage - 1, "&laquo;", "Go to previous page", currentPage === 1)
    );

    const MAX_VISIBLE_PAGES = 5; 
    let startPage, endPage;

    if (totalPages <= MAX_VISIBLE_PAGES) {
        startPage = 1;
        endPage = totalPages;
    } else {
        const maxPagesBeforeCurrentPage = Math.floor((MAX_VISIBLE_PAGES - 2) / 2); 
        const maxPagesAfterCurrentPage = Math.ceil((MAX_VISIBLE_PAGES - 2) / 2) - 1;

        if (currentPage <= maxPagesBeforeCurrentPage + 1) { 
            startPage = 1;
            endPage = MAX_VISIBLE_PAGES - 1; 
            paginationNavButtons.appendChild(createPageButton(1, 1, `Go to page 1`, false, currentPage === 1));
            for (let i = 2; i <= endPage; i++) {
                paginationNavButtons.appendChild(createPageButton(i, i, `Go to page ${i}`, false, currentPage === i));
            }
            paginationNavButtons.appendChild(createPageButton(0, '...', '', false, false, true)); 
            paginationNavButtons.appendChild(createPageButton(totalPages, totalPages, `Go to page ${totalPages}`));

        } else if (currentPage >= totalPages - maxPagesAfterCurrentPage - 1) { 
            startPage = totalPages - (MAX_VISIBLE_PAGES - 2);
            endPage = totalPages;
            paginationNavButtons.appendChild(createPageButton(1, 1, `Go to page 1`));
            paginationNavButtons.appendChild(createPageButton(0, '...', '', false, false, true)); 
            for (let i = startPage; i <= endPage; i++) {
                paginationNavButtons.appendChild(createPageButton(i, i, `Go to page ${i}`, false, currentPage === i));
            }
        } else { 
            startPage = currentPage - maxPagesBeforeCurrentPage;
            endPage = currentPage + maxPagesAfterCurrentPage + 1; 
            paginationNavButtons.appendChild(createPageButton(1, 1, `Go to page 1`));
            paginationNavButtons.appendChild(createPageButton(0, '...', '', false, false, true)); 
            for (let i = startPage; i <= endPage; i++) {
                paginationNavButtons.appendChild(createPageButton(i, i, `Go to page ${i}`, false, currentPage === i));
            }
            paginationNavButtons.appendChild(createPageButton(0, '...', '', false, false, true)); 
            paginationNavButtons.appendChild(createPageButton(totalPages, totalPages, `Go to page ${totalPages}`));
        }
    }

    if (!startPage) { 
        for (let i = 1; i <= totalPages; i++) {
            paginationNavButtons.appendChild(createPageButton(i, i, `Go to page ${i}`, false, i === currentPage));
        }
    }

    paginationNavButtons.appendChild(
        createPageButton(currentPage + 1, "&raquo;", "Go to next page", currentPage === totalPages)
    );

    paginationContainer.appendChild(paginationNavButtons); 

    paginationContainer.appendChild(itemsPerPageDiv);
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('cryptoTrackerTheme', theme); 
    if (toggleThemeBtn) {
        toggleThemeBtn.textContent = theme === 'dark' ? 'Light Mode 🌞' : 'Dark Mode 🌙';
        toggleThemeBtn.setAttribute('aria-pressed', theme === 'dark'); 
    }
}

// --- DATA FETCHING & PROCESSING ---

/**
 * Fetches the latest crypto prices from the API and updates the UI.
 */
async function fetchAndDisplayPrices() {
    if (updateBtn && btnSpinner && btnText) {
        showToast("Fetching latest prices...", "info", CONFIG.TOAST_LOADING_DURATION);
        updateBtn.classList.add("btn-loading");
        btnSpinner.style.display = "inline-block";
        btnText.textContent = "Loading..."; 
        updateBtn.disabled = true;
    } else if (cryptoContainer) {
        cryptoContainer.innerHTML = "<p class='col-12 text-center p-5'>Loading crypto data...</p>";
    }

    try {
        const response = await fetch(CONFIG.API_ENDPOINTS.latestPrices);
        if (!response.ok) {
            let errorBody = '';
            try { errorBody = await response.text(); } catch (_) { /* ignore */ }
            throw new Error(`API Error: ${response.status} ${response.statusText}. ${errorBody}`);
        }
        currentCryptoData = await response.json(); 

        currentPage = 1;
        filterAndSortAndRender(); 

        if (currentCryptoData.length > 0 && cryptoContainer?.children.length > 0 && cryptoContainer.children[0].tagName !== 'P') {
            showToast("Latest prices displayed.", "success");
        } else if (!searchInput?.value.trim() && currentCryptoData.length === 0) {
            showToast("No cryptocurrency data found from API.", "info");
        }

    } catch (error) {
        console.error("Error fetching or displaying prices:", error);
        showToast(`❌ Error loading prices: ${error.message}`, "danger", CONFIG.TOAST_ERROR_DURATION);
        if (cryptoContainer) {
            cryptoContainer.innerHTML = `<p style='color:var(--trend-down-color);' class='col-12 text-center p-5'>Could not load cryptocurrency data. Please try updating or check back later.</p>`;
        }
        renderPaginationControls(0); 
    } finally {
        if (updateBtn && btnSpinner && btnText) {
            updateBtn.classList.remove("btn-loading", "btn-success", "btn-danger"); 
            btnSpinner.style.display = "none";
            btnText.textContent = "Update Prices"; 
            updateBtn.disabled = false; 
        }
    }
}

/**
 * Filters, sorts, paginates, and renders the crypto data based on current state.
 */
function filterAndSortAndRender() {
    let dataToProcess = [...currentCryptoData]; 

    const searchTerm = searchInput?.value.toLowerCase().trim() || '';
    if (searchTerm) {
        dataToProcess = dataToProcess.filter(asset =>
            asset.name?.toLowerCase().includes(searchTerm) ||
            asset.symbol?.toLowerCase().includes(searchTerm)
        );
    }

    const sortBy = sortSelect?.value || 'default';

    const compareFunction = (a, b, key, asc = true) => {
        let valA, valB;

        if (key === 'name') {
            valA = String(a.name || "").toLowerCase();
            valB = String(b.name || "").toLowerCase();
            return asc ? valA.localeCompare(valB) : valB.localeCompare(valA);
        }
        if (key === 'currentPrice') {
            valA = parseFloat(String(a.currentPrice || '').replace(/,/g, ''));
            valB = parseFloat(String(b.currentPrice || '').replace(/,/g, ''));
            if (isNaN(valA)) valA = asc ? Infinity : -Infinity;
            if (isNaN(valB)) valB = asc ? Infinity : -Infinity;
        }
        if (key === 'percentageChange') {
            valA = a.trend?.percentageChange;
            valB = b.trend?.percentageChange;
            if (valA == null || isNaN(valA)) valA = asc ? Infinity : -Infinity;
            if (valB == null || isNaN(valB)) valB = asc ? Infinity : -Infinity;
        }

        return asc ? valA - valB : valB - valA;
    };

    switch (sortBy) {
        case 'name_asc': dataToProcess.sort((a, b) => compareFunction(a, b, 'name', true)); break;
        case 'name_desc': dataToProcess.sort((a, b) => compareFunction(a, b, 'name', false)); break;
        case 'price_asc': dataToProcess.sort((a, b) => compareFunction(a, b, 'currentPrice', true)); break;
        case 'price_desc': dataToProcess.sort((a, b) => compareFunction(a, b, 'currentPrice', false)); break;
        case 'change_asc': dataToProcess.sort((a, b) => compareFunction(a, b, 'percentageChange', true)); break;
        case 'change_desc': dataToProcess.sort((a, b) => compareFunction(a, b, 'percentageChange', false)); break;
        case 'default': 
            break;
    }


    const totalItems = dataToProcess.length;
    const totalPages = Math.ceil(totalItems / itemsPerPage);
    if (currentPage > totalPages) {
        currentPage = totalPages || 1; 
    }

    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const paginatedData = dataToProcess.slice(startIndex, endIndex);

    renderCryptoCards(paginatedData);
    renderPaginationControls(totalItems); 
}


// --- EVENT HANDLERS ---

async function handleUpdatePricesClick() {
    if (!updateBtn || !btnText || !btnSpinner) return;

    updateBtn.disabled = true;
    btnText.textContent = "Updating...";
    btnSpinner.style.display = "inline-block";
    updateBtn.classList.add("btn-loading");
    updateBtn.classList.remove("btn-success", "btn-danger");
    showToast("Requesting price update from server...", "info", CONFIG.TOAST_LOADING_DURATION);

    try {
        const response = await fetch(CONFIG.API_ENDPOINTS.updatePrices, { method: "POST" });

        if (response.ok) {
            updateBtn.classList.remove("btn-loading");
            updateBtn.classList.add("btn-success"); 
            btnText.textContent = "Updated ✓";
            btnSpinner.style.display = "none";
            showToast("Update request successful. Fetching new data...", "success");

            setTimeout(async () => {
                await fetchAndDisplayPrices(); 
            }, 1000); 

        } else {
            const errorText = await response.text();
            throw new Error(`Server error: ${response.status} ${errorText || response.statusText}`);
        }
    } catch (error) {
        console.error("Error updating prices:", error);
        showToast(`❌ Failed to update prices: ${error.message}`, "danger", CONFIG.TOAST_ERROR_DURATION);
        updateBtn.classList.remove("btn-loading", "btn-success");
        updateBtn.classList.add("btn-danger"); 
        btnText.textContent = "Update Failed ✗";
        btnSpinner.style.display = "none";

        setTimeout(() => {
            if (updateBtn && btnText) { 
                btnText.textContent = "Update Prices";
                updateBtn.classList.remove("btn-danger");
                updateBtn.disabled = false;
            }
        }, 3000);
    }
   
}

function handleToggleThemeClick() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    applyTheme(newTheme);
}

function handleSearchOrSortChange() {
    currentPage = 1; 
    filterAndSortAndRender();
}

// --- INITIALIZATION ---

/**
 * Initializes the application: applies theme, sets up event listeners, and fetches initial data.
 */
function init() {
    const savedTheme = localStorage.getItem('cryptoTrackerTheme');
    const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    applyTheme(savedTheme || (prefersDark ? 'dark' : 'light')); 

    updateBtn?.addEventListener("click", handleUpdatePricesClick);
    searchInput?.addEventListener("input", debounce(handleSearchOrSortChange, CONFIG.DEBOUNCE_DELAY));
    sortSelect?.addEventListener("change", handleSearchOrSortChange);
    toggleThemeBtn?.addEventListener('click', handleToggleThemeClick);

    fetchAndDisplayPrices();
}

// --- Run Initialization ---
// Redefine DOM element references within init or ensure they are checked before use if accessed globally.
// For functions like showToast that use toastContainer, it's assumed to be globally available if used.
function init() {
    // Global theme application
    const savedTheme = localStorage.getItem('cryptoTrackerTheme');
    const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    applyTheme(savedTheme || (prefersDark ? 'dark' : 'light')); 

    // Listener for theme toggle button (if it exists on any page)
    // Re-fetch toggleThemeBtn here as its existence might be page-dependent.
    const pageToggleThemeBtn = document.getElementById("toggleThemeBtn");
    pageToggleThemeBtn?.addEventListener('click', handleToggleThemeClick);

    // Check if we are on a page that includes the main crypto tracking elements (Index.cshtml)
    // Re-fetch cryptoContainer here to gate Index.cshtml specific logic.
    const pageCryptoContainer = document.getElementById('cryptoGridContainer');
    if (pageCryptoContainer) {
        // References to elements specific to Index.cshtml
        // Re-fetch these elements or ensure global consts are only used if pageCryptoContainer exists.
        const pageUpdateBtn = document.getElementById("updateBtn");
        const pageSearchInput = document.getElementById("searchInput");
        const pageSortSelect = document.getElementById("sortSelect");
        // paginationContainer is already checked internally by renderPaginationControls
        // btnText and btnSpinner are derived from updateBtn, ensure handleUpdatePricesClick checks them or updateBtn itself.

        // Event listeners for Index.cshtml elements
        pageUpdateBtn?.addEventListener("click", handleUpdatePricesClick);
        pageSearchInput?.addEventListener("input", debounce(handleSearchOrSortChange, CONFIG.DEBOUNCE_DELAY));
        pageSortSelect?.addEventListener("change", handleSearchOrSortChange);

        // Initial data load for Index.cshtml
        // fetchAndDisplayPrices internally checks for cryptoContainer (which is pageCryptoContainer here)
        // and other elements like updateBtn.
        fetchAndDisplayPrices(); 
    }
}

document.addEventListener("DOMContentLoaded", init);