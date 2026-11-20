using System.Collections.Generic;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Service> FeaturedServices { get; set; } = new();
        public int TotalPetsServed { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalServices { get; set; }
    }
}
