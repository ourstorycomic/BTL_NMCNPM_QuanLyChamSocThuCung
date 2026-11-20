using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Data;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;
using BTL_NMCNPM_QuanLyChamSocThuCung.ViewModels;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _db.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();

            var totalPets = await _db.Pets.CountAsync();
            var totalCusts = await _db.Customers.CountAsync(c => c.IsActive);
            var completedAppts = await _db.Appointments.CountAsync(a => a.Status == "Completed");

            var model = new HomeIndexViewModel
            {
                FeaturedServices = services,
                TotalPetsServed = completedAppts > 0 ? completedAppts : totalPets,
                TotalCustomers = totalCusts,
                TotalServices = services.Count
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
