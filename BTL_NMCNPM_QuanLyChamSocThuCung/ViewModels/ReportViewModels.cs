using System;
using System.Collections.Generic;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels
{
    public class ReportDashboardViewModel
    {
        public string TimeRange { get; set; } = "month"; // today, 7days, month, custom
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // 4 KPI chính
        public decimal TotalRevenue { get; set; }
        public int TotalPaidInvoices { get; set; }
        public decimal TotalSurcharge { get; set; }
        public decimal AverageOrderValue => TotalPaidInvoices > 0 ? Math.Round(TotalRevenue / TotalPaidInvoices, 0) : 0;

        // Tỷ lệ phương thức thanh toán
        public decimal CashRevenue { get; set; }
        public decimal QrRevenue { get; set; }

        // Top dịch vụ mang lại doanh thu cao nhất
        public List<ServiceRevenueItemViewModel> TopServices { get; set; } = new();

        // Dữ liệu vẽ biểu đồ theo ngày (Chart.js)
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartRevenueData { get; set; } = new();
        public List<int> ChartOrderCountData { get; set; } = new();

        // Danh sách hóa đơn chi tiết trong kỳ báo cáo
        public List<ReportInvoiceItemViewModel> RecentInvoices { get; set; } = new();
    }

    public class ServiceRevenueItemViewModel
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public double Percentage { get; set; }
    }

    public class ReportInvoiceItemViewModel
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceCode => $"#HD-{InvoiceId.ToString().Substring(0, 6).ToUpper()}";
        public DateTime PaymentTime { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
        public decimal Surcharge { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }
}
