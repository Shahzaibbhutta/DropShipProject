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

            var recommendedProducts = await _context.OrderItems
                .Where(oi => oi.Order.Status == "Delivered")
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .Join(_context.Products.Include(p => p.Supplier),
                    sale => sale.ProductId,
                    product => product.Id,
                    (sale, product) => product)
                .ToListAsync();

            if (!recommendedProducts.Any())
            {
                recommendedProducts = await _context.Products
                    .Include(p => p.Supplier)
                    .OrderBy(_ => Guid.NewGuid()) 
                    .Take(10)
                    .ToListAsync();
            }

            // Pass both to the view
            ViewData["RecommendedProducts"] = recommendedProducts;

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
