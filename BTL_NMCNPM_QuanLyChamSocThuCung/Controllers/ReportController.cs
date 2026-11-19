using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;
using BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    /// <summary>
    /// Module Báo cáo Thống kê Doanh thu (UC13 / FR-24)
    /// Dành cho Quản trị viên (Admin) / Quản lý cơ sở
    /// Phụ trách: Nguyễn Hà Phương
    /// </summary>
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReportController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. DASHBOARD BÁO CÁO DOANH THU & BIỂU ĐỒ (UC13)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string? range, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.Now;
            DateTime from;
            DateTime to;

            range = string.IsNullOrWhiteSpace(range) ? "month" : range.ToLowerInvariant();

            switch (range)
            {
                case "today":
                    from = DateTime.Today;
                    to = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "7days":
                    from = DateTime.Today.AddDays(-6);
                    to = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "custom":
                    from = fromDate?.Date ?? DateTime.Today.AddDays(-30);
                    to = (toDate?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
                    break;
                case "month":
                default:
                    range = "month";
                    from = new DateTime(now.Year, now.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                    break;
            }

            // Truy vấn các hóa đơn đã thanh toán trong kỳ
            var paidInvoices = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Pet)
                        .ThenInclude(p => p.Customer)
                .Where(i => i.PaymentStatus == "Paid" &&
                            i.PaymentTime >= from &&
                            i.PaymentTime <= to)
                .OrderByDescending(i => i.PaymentTime)
                .ToListAsync();

            // Tính 4 chỉ số KPI chính
            var totalRevenue = paidInvoices.Sum(i => i.TotalAmount);
            var totalCount = paidInvoices.Count;
            var totalSurcharge = paidInvoices.Sum(i => i.Surcharge);

            var cashRevenue = paidInvoices.Where(i => i.PaymentMethod == "Cash").Sum(i => i.TotalAmount);
            var qrRevenue = paidInvoices.Where(i => i.PaymentMethod == "QRCode").Sum(i => i.TotalAmount);

            // Thống kê Top dịch vụ
            var serviceStats = new Dictionary<string, (string Category, int Count, decimal Revenue)>();
            foreach (var inv in paidInvoices)
            {
                if (inv.InvoiceDetails != null && inv.InvoiceDetails.Any())
                {
                    foreach (var d in inv.InvoiceDetails)
                    {
                        var name = d.Service?.ServiceName ?? "Dịch vụ khác";
                        var cat = d.Service?.Category ?? "Chung";
                        if (!serviceStats.ContainsKey(name))
                        {
                            serviceStats[name] = (cat, 0, 0m);
                        }
                        var current = serviceStats[name];
                        serviceStats[name] = (current.Category, current.Count + d.Quantity, current.Revenue + d.Amount);
                    }
                }
            }

            var topServices = serviceStats
                .OrderByDescending(kv => kv.Value.Revenue)
                .Take(5)
                .Select(kv => new ServiceRevenueItemViewModel
                {
                    ServiceName = kv.Key,
                    Category = kv.Value.Category,
                    UsageCount = kv.Value.Count,
                    TotalRevenue = kv.Value.Revenue,
                    Percentage = totalRevenue > 0 ? (double)Math.Round((kv.Value.Revenue / totalRevenue) * 100, 1) : 0
                })
                .ToList();

            // Dữ liệu vẽ biểu đồ Chart.js theo mốc thời gian
            var chartLabels = new List<string>();
            var chartRevenue = new List<decimal>();
            var chartOrderCount = new List<int>();

            var daySpan = (to.Date - from.Date).Days + 1;
            if (daySpan > 0 && daySpan <= 31)
            {
                // Chia theo từng ngày
                for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
                {
                    var label = day.ToString("dd/MM");
                    var dayInvoices = paidInvoices.Where(i => i.PaymentTime.HasValue && i.PaymentTime.Value.Date == day).ToList();

                    chartLabels.Add(label);
                    chartRevenue.Add(dayInvoices.Sum(i => i.TotalAmount));
                    chartOrderCount.Add(dayInvoices.Count);
                }
            }
            else
            {
                // Chia theo tuần hoặc tháng nếu khoảng thời gian quá dài
                for (var start = from.Date; start <= to.Date; start = start.AddDays(7))
                {
                    var end = start.AddDays(6) > to.Date ? to.Date : start.AddDays(6);
                    var label = $"{start:dd/MM}-{end:dd/MM}";
                    var weekInvoices = paidInvoices.Where(i => i.PaymentTime.HasValue &&
                                                               i.PaymentTime.Value.Date >= start &&
                                                               i.PaymentTime.Value.Date <= end).ToList();

                    chartLabels.Add(label);
                    chartRevenue.Add(weekInvoices.Sum(i => i.TotalAmount));
                    chartOrderCount.Add(weekInvoices.Count);
                }
            }

            // Danh sách hóa đơn chi tiết hiển thị trong bảng
            var recentInvoices = paidInvoices.Select(i => new ReportInvoiceItemViewModel
            {
                InvoiceId = i.InvoiceId,
                PaymentTime = i.PaymentTime ?? i.CreatedAt,
                CustomerName = i.Appointment?.Pet?.Customer?.CustomerName ?? "Khách lẻ",
                PhoneNumber = i.Appointment?.Pet?.Customer?.PhoneNumber ?? "—",
                PetName = i.Appointment?.Pet?.PetName ?? "—",
                Surcharge = i.Surcharge,
                DiscountAmount = i.DiscountAmount,
                TotalAmount = i.TotalAmount,
                PaymentMethod = i.PaymentMethod
            }).ToList();

            var model = new ReportDashboardViewModel
            {
                TimeRange = range,
                FromDate = from,
                ToDate = to,
                TotalRevenue = totalRevenue,
                TotalPaidInvoices = totalCount,
                TotalSurcharge = totalSurcharge,
                CashRevenue = cashRevenue,
                QrRevenue = qrRevenue,
                TopServices = topServices,
                ChartLabels = chartLabels,
                ChartRevenueData = chartRevenue,
                ChartOrderCountData = chartOrderCount,
                RecentInvoices = recentInvoices
            };

            return View(model);
        }

        // ==========================================
        // 2. XUẤT BÁO CÁO EXCEL / CSV (UC13: Xuất Excel)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportCsv(string? range, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.Now;
            DateTime from;
            DateTime to;

            range = string.IsNullOrWhiteSpace(range) ? "month" : range.ToLowerInvariant();

            switch (range)
            {
                case "today":
                    from = DateTime.Today;
                    to = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "7days":
                    from = DateTime.Today.AddDays(-6);
                    to = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "custom":
                    from = fromDate?.Date ?? DateTime.Today.AddDays(-30);
                    to = (toDate?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
                    break;
                case "month":
                default:
                    from = new DateTime(now.Year, now.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                    break;
            }

            var paidInvoices = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Pet)
                        .ThenInclude(p => p.Customer)
                .Where(i => i.PaymentStatus == "Paid" &&
                            i.PaymentTime >= from &&
                            i.PaymentTime <= to)
                .OrderBy(i => i.PaymentTime)
                .ToListAsync();

            var sb = new StringBuilder();

            // UTF-8 BOM để Excel hiển thị đúng tiếng Việt không bị lỗi font
            sb.Append('\uFEFF');

            // Header báo cáo
            sb.AppendLine("BÁO CÁO DOANH THU PAWHAVEN");
            sb.AppendLine($"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Kỳ báo cáo: Từ {from:dd/MM/yyyy} Đến {to:dd/MM/yyyy}");
            sb.AppendLine($"Tổng số hóa đơn: {paidInvoices.Count}");
            sb.AppendLine($"Tổng doanh thu thực thu: {paidInvoices.Sum(x => x.TotalAmount):N0} VNĐ");
            sb.AppendLine();

            // Tiêu đề cột
            sb.AppendLine("Mã hóa đơn,Thời gian thanh toán,Khách hàng,Số điện thoại,Thú cưng,Hình thức,Phụ phí (VNĐ),Giảm giá (VNĐ),Tổng tiền (VNĐ)");

            foreach (var inv in paidInvoices)
            {
                var code = $"HD-{inv.InvoiceId.ToString().Substring(0, 6).ToUpper()}";
                var time = (inv.PaymentTime ?? inv.CreatedAt).ToString("dd/MM/yyyy HH:mm");
                var custName = EscapeCsv(inv.Appointment?.Pet?.Customer?.CustomerName ?? "");
                var phone = EscapeCsv(inv.Appointment?.Pet?.Customer?.PhoneNumber ?? "");
                var petName = EscapeCsv(inv.Appointment?.Pet?.PetName ?? "");
                var method = inv.PaymentMethod == "QRCode" ? "Chuyển khoản QR" : "Tiền mặt";
                var surcharge = inv.Surcharge.ToString("F0", CultureInfo.InvariantCulture);
                var discount = inv.DiscountAmount.ToString("F0", CultureInfo.InvariantCulture);
                var total = inv.TotalAmount.ToString("F0", CultureInfo.InvariantCulture);

                sb.AppendLine($"{code},{time},{custName},{phone},{petName},{method},{surcharge},{discount},{total}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"BaoCaoDoanhThu_PawHaven_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains(',') || text.Contains('"') || text.Contains('\n'))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }
            return text;
        }
    }
}
