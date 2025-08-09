
window.AnalyticsModule = window.AnalyticsModule || {};

// Define all functions immediately at global scope for compatibility
window.renderChart = function (canvasId, config) {
    console.log('renderChart called for:', canvasId);

    // Ensure Chart.js is loaded
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded');
        return;
    }

    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        console.error(`Canvas element ${canvasId} not found`);
        return;
    }

    // Destroy existing chart if present
    if (window.AnalyticsModule.charts && window.AnalyticsModule.charts[canvasId]) {
        window.AnalyticsModule.charts[canvasId].destroy();
    }

    // Initialize charts object if needed
    window.AnalyticsModule.charts = window.AnalyticsModule.charts || {};

    // Create the chart
    const chartConfig = {
        type: config.type || 'line',
        data: {
            labels: config.labels || [],
            datasets: [{
                label: config.label || 'Data',
                data: config.data || [],
                borderColor: 'rgb(59, 130, 246)',
                backgroundColor: 'rgba(59, 130, 246, 0.1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    };

    window.AnalyticsModule.charts[canvasId] = new Chart(ctx.getContext('2d'), chartConfig);
};

window.renderPieChart = function (canvasId, config) {
    console.log('renderPieChart called for:', canvasId);

    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded');
        return;
    }

    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        console.error(`Canvas element ${canvasId} not found`);
        return;
    }

    // Destroy existing chart if present
    if (window.AnalyticsModule.charts && window.AnalyticsModule.charts[canvasId]) {
        window.AnalyticsModule.charts[canvasId].destroy();
    }

    window.AnalyticsModule.charts = window.AnalyticsModule.charts || {};

    const chartConfig = {
        type: 'doughnut',
        data: {
            labels: config.labels || [],
            datasets: [{
                data: config.data || [],
                backgroundColor: config.colors || [
                    '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444',
                    '#06B6D4', '#84CC16', '#F97316', '#A855F7', '#EF4444'
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        font: {
                            size: 12
                        }
                    }
                }
            }
        }
    };

    window.AnalyticsModule.charts[canvasId] = new Chart(ctx.getContext('2d'), chartConfig);
};

window.initializeLiveChart = function (chartId, config) {
    console.log('initializeLiveChart called for:', chartId);

    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded');
        return null;
    }

    const ctx = document.getElementById(chartId);
    if (!ctx) {
        console.error(`Canvas element ${chartId} not found`);
        return null;
    }

    window.AnalyticsModule.charts = window.AnalyticsModule.charts || {};

    if (window.AnalyticsModule.charts[chartId]) {
        window.AnalyticsModule.charts[chartId].destroy();
    }

    window.AnalyticsModule.charts[chartId] = new Chart(ctx.getContext('2d'), config);
    return window.AnalyticsModule.charts[chartId];
};

window.updateLiveChart = function (chartId, data) {
    if (!window.AnalyticsModule.charts || !window.AnalyticsModule.charts[chartId]) {
        console.warn(`Chart ${chartId} not found`);
        return;
    }

    const chart = window.AnalyticsModule.charts[chartId];
    chart.data.labels = data.labels;

    if (data.datasets) {
        data.datasets.forEach((dataset, index) => {
            if (chart.data.datasets[index]) {
                chart.data.datasets[index].data = dataset.data;
            }
        });
    }

    chart.update('none');
};

window.destroyChart = function (chartId) {
    if (window.AnalyticsModule.charts && window.AnalyticsModule.charts[chartId]) {
        window.AnalyticsModule.charts[chartId].destroy();
        delete window.AnalyticsModule.charts[chartId];
    }
};

window.resetChartZoom = function (chartId) {
    if (window.AnalyticsModule.charts && window.AnalyticsModule.charts[chartId]) {
        const chart = window.AnalyticsModule.charts[chartId];
        if (chart.resetZoom) {
            chart.resetZoom();
        }
    }
};

window.playSound = function (type) {
    console.log('Playing sound:', type);
    // Implementation for sound playing
};

window.showToast = function (message, type) {
    console.log(`Toast [${type}]: ${message}`);

    const toast = document.createElement('div');
    toast.className = `toast toast-${type || 'info'}`;
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: white;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        padding: 16px;
        z-index: 9999;
        animation: slideIn 0.3s ease;
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 300px;
    `;

    toast.innerHTML = `
        <div style="font-size: 20px; color: ${type === 'success' ? '#10b981' : '#3b82f6'};">
            ${type === 'success' ? '✓' : 'ℹ'}
        </div>
        <div>${message}</div>
    `;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
};

// Initialize the module
(function () {
    console.log('Analytics Module JavaScript loaded and ready');
    window.AnalyticsModule.isReady = true;
})();
