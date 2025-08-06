// wwwroot/dynamicRouting.js
window.dynamicRouting = {
    initializeDynamicRouting: function () {
        console.log('Dynamic routing initialized');

        // Store original pushState and replaceState
        const originalPushState = history.pushState;
        const originalReplaceState = history.replaceState;

        // Override to intercept navigation
        history.pushState = function () {
            originalPushState.apply(history, arguments);
            window.dispatchEvent(new Event('blazor-route-changed'));
        };

        history.replaceState = function () {
            originalReplaceState.apply(history, arguments);
            window.dispatchEvent(new Event('blazor-route-changed'));
        };
    },

    softRefreshRoutes: function () {
        // Trigger a soft refresh without losing state
        const event = new CustomEvent('blazor-routes-refresh', {
            detail: { timestamp: new Date().toISOString() }
        });
        window.dispatchEvent(event);

        // Force Blazor to re-evaluate routes
        if (window.Blazor) {
            const currentPath = window.location.pathname;
            history.replaceState(null, '', currentPath);
        }
    },

    navigateToRoute: function (path, forceReload = false) {
        if (forceReload) {
            window.location.href = path;
        } else {
            // Use Blazor's navigation
            window.Blazor.navigateTo(path);
        }
    }
};

export function initializeDynamicRouting() {
    return window.dynamicRouting.initializeDynamicRouting();
}

export function softRefreshRoutes() {
    return window.dynamicRouting.softRefreshRoutes();
}

export function navigateToRoute(path, forceReload) {
    return window.dynamicRouting.navigateToRoute(path, forceReload);
}