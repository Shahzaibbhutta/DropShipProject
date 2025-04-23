using DropShipProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DropShipProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseContext _context;
        public HomeController(ILogger<HomeController> logger, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch products with their suppliers and group by supplier
            var productsBySupplier = await _context.Products
                .Include(p => p.Supplier)
                .GroupBy(p => new
                {
                    p.SupplierId,
                    SupplierName = p.Supplier != null ? p.Supplier.UserName : "Unknown Supplier"
                })
                .Select(g => new SupplierProductsViewModel
                {
                    SupplierName = g.Key.SupplierName,
                    Products = g.ToList()
                })
                .ToListAsync();

            return View(productsBySupplier);
        }
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
