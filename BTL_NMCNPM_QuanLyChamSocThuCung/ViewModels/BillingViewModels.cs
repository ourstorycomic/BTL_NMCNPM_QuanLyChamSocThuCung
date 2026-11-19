using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels
{
    public class BillingIndexViewModel
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; } // All, Unpaid, Paid
        public string? PaymentMethod { get; set; } // All, Cash, QRCode

        // Thống kê nhanh
        public decimal TodayRevenue { get; set; }
        public int TodayPaidCount { get; set; }
        public int PendingCheckoutCount { get; set; }

        // Danh sách các cuộc hẹn chờ lập hóa đơn / thanh toán
        public List<Appointment> PendingAppointments { get; set; } = new();

        // Danh sách các hóa đơn đã tạo
        public List<Invoice> Invoices { get; set; } = new();
    }

    public class BillingCheckoutViewModel
    {
        public Guid AppointmentId { get; set; }
        public Guid? InvoiceId { get; set; }

        // Thông tin khách hàng & thú cưng
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }

        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public decimal ActualWeight { get; set; }
        public string? AllergyNote { get; set; }

        // Ngày giờ hẹn
        public DateTime AppointmentDate { get; set; }

        // Danh sách dịch vụ thực hiện
        public List<BillingServiceItemViewModel> Services { get; set; } = new();

        // Phụ phí phát sinh (từ bước tiếp nhận/chăm sóc hoặc bổ sung tại quầy)
        public decimal Surcharge { get; set; }
        public string? SurchargeReason { get; set; }

        // Khuyến mãi & Giảm giá (UC12 / FR-21)
        public string? DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; }

        // Phương thức thanh toán (Cash, QRCode)
        public string PaymentMethod { get; set; } = "Cash";

        // Tổng tiền
        public decimal ServiceTotal => Services.Sum(s => s.Amount);
        public decimal FinalTotal => Math.Max(0, ServiceTotal + Surcharge - DiscountAmount);
    }

    public class BillingServiceItemViewModel
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Amount => Quantity * UnitPrice;
    }

    public class BillingProcessPaymentModel
    {
        [Required]
        public Guid AppointmentId { get; set; }

        public decimal Surcharge { get; set; }
        public string? SurchargeReason { get; set; }

        public string? DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, QRCode

        public string? CashierNote { get; set; }
    }
}
