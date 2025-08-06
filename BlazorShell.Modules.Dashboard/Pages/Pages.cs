using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorShell.Modules.Dashboard.Pages
{
    // Base Dashboard Page Component
    [Route("/dashboard")]
    public class DashboardPage : ComponentBase
    {
        [Inject] private IDashboardService DashboardService { get; set; } = default!;
        [Inject] private IWidgetService WidgetService { get; set; } = default!;

        private DashboardData? _dashboardData;
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            _isLoading = true;
            _dashboardData = await DashboardService.GetDashboardDataAsync();
            _isLoading = false;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "container-fluid");

            // Header
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "class", "d-flex justify-content-between align-items-center mb-4");

            builder.OpenElement(4, "h1");
            builder.OpenElement(5, "i");
            builder.AddAttribute(6, "class", "bi bi-speedometer2");
            builder.CloseElement();
            builder.AddContent(7, " Dashboard");
            builder.CloseElement();

            builder.OpenElement(8, "button");
            builder.AddAttribute(9, "class", "btn btn-primary");
            builder.AddAttribute(10, "onclick", EventCallback.Factory.Create(this, LoadData));
            builder.OpenElement(11, "i");
            builder.AddAttribute(12, "class", "bi bi-arrow-clockwise");
            builder.CloseElement();
            builder.AddContent(13, " Refresh");
            builder.CloseElement();

            builder.CloseElement(); // Header div

            if (_isLoading)
            {
                builder.OpenElement(14, "div");
                builder.AddAttribute(15, "class", "text-center py-5");
                builder.OpenElement(16, "div");
                builder.AddAttribute(17, "class", "spinner-border text-primary");
                builder.CloseElement();
                builder.OpenElement(18, "p");
                builder.AddAttribute(19, "class", "mt-2");
                builder.AddContent(20, "Loading dashboard...");
                builder.CloseElement();
                builder.CloseElement();
            }
            else if (_dashboardData != null)
            {
                // Stats Cards
                builder.OpenElement(21, "div");
                builder.AddAttribute(22, "class", "row mb-4");

                RenderStatCard(builder, 23, "Total Users", _dashboardData.TotalUsers.ToString("N0"),
                    "bi-people", "primary");
                RenderStatCard(builder, 30, "Active Sessions", _dashboardData.ActiveSessions.ToString(),
                    "bi-activity", "success");
                RenderStatCard(builder, 37, "Revenue", _dashboardData.TotalRevenue.ToString("C"),
                    "bi-currency-dollar", "info");
                RenderStatCard(builder, 44, "Growth Rate",
                    $"{(_dashboardData.GrowthRate > 0 ? "+" : "")}{_dashboardData.GrowthRate:F1}%",
                    "bi-graph-up-arrow", "warning");

                builder.CloseElement(); // Row

                // Activities
                if (_dashboardData.RecentActivities?.Any() == true)
                {
                    builder.OpenElement(51, "div");
                    builder.AddAttribute(52, "class", "card");
                    builder.OpenElement(53, "div");
                    builder.AddAttribute(54, "class", "card-header");
                    builder.AddContent(55, "Recent Activities");
                    builder.CloseElement();
                    builder.OpenElement(56, "div");
                    builder.AddAttribute(57, "class", "card-body");

                    builder.OpenElement(58, "ul");
                    builder.AddAttribute(59, "class", "list-group list-group-flush");

                    var activityIndex = 60;
                    foreach (var activity in _dashboardData.RecentActivities)
                    {
                        builder.OpenElement(activityIndex++, "li");
                        builder.AddAttribute(activityIndex++, "class", "list-group-item");
                        builder.AddContent(activityIndex++, activity);
                        builder.CloseElement();
                    }

                    builder.CloseElement(); // ul
                    builder.CloseElement(); // card-body
                    builder.CloseElement(); // card
                }
            }

            builder.CloseElement(); // Container
        }

        private void RenderStatCard(RenderTreeBuilder builder, int sequence, string title,
            string value, string icon, string color)
        {
            builder.OpenElement(sequence, "div");
            builder.AddAttribute(sequence + 1, "class", "col-xl-3 col-md-6 mb-3");

            builder.OpenElement(sequence + 2, "div");
            builder.AddAttribute(sequence + 3, "class", $"card border-start border-{color} border-4");

            builder.OpenElement(sequence + 4, "div");
            builder.AddAttribute(sequence + 5, "class", "card-body");

            builder.OpenElement(sequence + 6, "div");
            builder.AddAttribute(sequence + 7, "class", "text-muted small");
            builder.AddContent(sequence + 8, title);
            builder.CloseElement();

            builder.OpenElement(sequence + 9, "div");
            builder.AddAttribute(sequence + 10, "class", "h4 mb-0");
            builder.AddContent(sequence + 11, value);
            builder.CloseElement();

            builder.CloseElement(); // card-body
            builder.CloseElement(); // card
            builder.CloseElement(); // col
        }
    }

    // Analytics Page Component
    [Route("/dashboard/analytics")]
    public class DashboardAnalyticsPage : ComponentBase
    {
        [Inject] private IDashboardService DashboardService { get; set; } = default!;

        private DashboardStats? _stats;

        protected override async Task OnInitializedAsync()
        {
            _stats = await DashboardService.GetStatsAsync();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "container-fluid");

            builder.OpenElement(2, "h1");
            builder.OpenElement(3, "i");
            builder.AddAttribute(4, "class", "bi bi-graph-up");
            builder.CloseElement();
            builder.AddContent(5, " Analytics");
            builder.CloseElement();

            if (_stats != null)
            {
                builder.OpenElement(6, "div");
                builder.AddAttribute(7, "class", "row mt-4");

                RenderMetricCard(builder, 8, "Total Visits", _stats.TotalVisits.ToString("N0"));
                RenderMetricCard(builder, 12, "Unique Visitors", _stats.UniqueVisitors.ToString("N0"));
                RenderMetricCard(builder, 16, "Page Views", _stats.PageViews.ToString("N0"));
                RenderMetricCard(builder, 20, "Bounce Rate", $"{_stats.BounceRate:F1}%");

                builder.CloseElement(); // row
            }

            builder.CloseElement(); // container
        }

        private void RenderMetricCard(RenderTreeBuilder builder, int sequence, string label, string value)
        {
            builder.OpenElement(sequence, "div");
            builder.AddAttribute(sequence + 1, "class", "col-md-3 mb-3");

            builder.OpenElement(sequence + 2, "div");
            builder.AddAttribute(sequence + 3, "class", "card");

            builder.OpenElement(sequence + 4, "div");
            builder.AddAttribute(sequence + 5, "class", "card-body text-center");

            builder.OpenElement(sequence + 6, "h6");
            builder.AddAttribute(sequence + 7, "class", "text-muted");
            builder.AddContent(sequence + 8, label);
            builder.CloseElement();

            builder.OpenElement(sequence + 9, "h3");
            builder.AddContent(sequence + 10, value);
            builder.CloseElement();

            builder.CloseElement(); // card-body
            builder.CloseElement(); // card
            builder.CloseElement(); // col
        }
    }

    // Widgets Page Component
    [Route("/dashboard/widgets")]
    public class DashboardWidgetsPage : ComponentBase
    {
        [Inject] private IWidgetService WidgetService { get; set; } = default!;

        private IEnumerable<Widget>? _availableWidgets;

        protected override async Task OnInitializedAsync()
        {
            _availableWidgets = await WidgetService.GetAvailableWidgetsAsync();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "container-fluid");

            builder.OpenElement(2, "h1");
            builder.OpenElement(3, "i");
            builder.AddAttribute(4, "class", "bi bi-grid-3x3");
            builder.CloseElement();
            builder.AddContent(5, " Widget Manager");
            builder.CloseElement();

            if (_availableWidgets != null)
            {
                builder.OpenElement(6, "div");
                builder.AddAttribute(7, "class", "row mt-4");

                var widgetIndex = 8;
                foreach (var widget in _availableWidgets)
                {
                    builder.OpenElement(widgetIndex++, "div");
                    builder.AddAttribute(widgetIndex++, "class", "col-lg-3 col-md-4 col-sm-6 mb-3");

                    builder.OpenElement(widgetIndex++, "div");
                    builder.AddAttribute(widgetIndex++, "class", "card h-100");

                    builder.OpenElement(widgetIndex++, "div");
                    builder.AddAttribute(widgetIndex++, "class", "card-body text-center");

                    builder.OpenElement(widgetIndex++, "i");
                    builder.AddAttribute(widgetIndex++, "class", $"{widget.Icon ?? "bi bi-puzzle"} fs-1 text-primary mb-3");
                    builder.CloseElement();

                    builder.OpenElement(widgetIndex++, "h5");
                    builder.AddContent(widgetIndex++, widget.Name);
                    builder.CloseElement();

                    builder.OpenElement(widgetIndex++, "p");
                    builder.AddAttribute(widgetIndex++, "class", "small text-muted");
                    builder.AddContent(widgetIndex++, $"Type: {widget.Type}");
                    builder.CloseElement();

                    builder.CloseElement(); // card-body
                    builder.CloseElement(); // card
                    builder.CloseElement(); // col
                }

                builder.CloseElement(); // row
            }

            builder.CloseElement(); // container
        }
    }

    // Reports Page Component
    [Route("/dashboard/reports")]
    public class DashboardReportsPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "container-fluid");

            builder.OpenElement(2, "h1");
            builder.OpenElement(3, "i");
            builder.AddAttribute(4, "class", "bi bi-file-earmark-bar-graph");
            builder.CloseElement();
            builder.AddContent(5, " Reports");
            builder.CloseElement();

            builder.OpenElement(6, "div");
            builder.AddAttribute(7, "class", "alert alert-info mt-4");
            builder.AddContent(8, "Report generation feature coming soon!");
            builder.CloseElement();

            builder.CloseElement(); // container
        }
    }
}