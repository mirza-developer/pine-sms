// Chart rendering functionality
let orderChartInstance = null;

window.renderOrderChart = function (config) {
    const canvas = document.getElementById('orderChart');
    if (!canvas) {
        console.error('Canvas element not found');
        return;
    }

    // Destroy existing chart instance if it exists
    if (orderChartInstance) {
        orderChartInstance.destroy();
    }

    // Wait for Chart.js to be available
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded');
        setTimeout(() => window.renderOrderChart(config), 500);
        return;
    }

    try {
        const ctx = canvas.getContext('2d');
        orderChartInstance = new Chart(ctx, config);
    } catch (error) {
        console.error('Error creating chart:', error);
    }
};
