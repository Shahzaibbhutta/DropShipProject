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

        public async Task<IActionResult> Index(string search, string sortBy, string sortOrder, decimal? minPrice, decimal? maxPrice)
        {
            // Store filter parameters in ViewData for form persistence
            ViewData["Search"] = search;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            ViewData["MinPrice"] = minPrice;
            ViewData["MaxPrice"] = maxPrice;

            // Base query
            var query = _context.Products
                .Include(p => p.Supplier)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) || p.SKU.ToLower().Contains(search));
            }

            // Apply price range filter
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                bool isAscending = sortOrder != "desc";
                switch (sortBy.ToLower())
                {
                    case "price":
                        query = isAscending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price);
                        break;
                    case "name":
                        query = isAscending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name);
                        break;
                    case "stock":
                        query = isAscending ? query.OrderBy(p => p.Stock) : query.OrderByDescending(p => p.Stock);
                        break;
                    case "sku":
                        query = isAscending ? query.OrderBy(p => p.SKU) : query.OrderByDescending(p => p.SKU);
                        break;
                }
            }

            // Group products by supplier
            var productsBySupplier = await query
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

            // Get recommended products
            var recommendedProducts = await _context.OrderItems
                .Where(oi => new[] { "Shipped", "Delivered" }.Contains(oi.Order.Status))
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(4)
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
                    .Take(4)
                    .ToListAsync();
            }

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

        [HttpPost]
        public async Task<IActionResult> GetOrderStatus([FromBody] OrderStatusRequest request)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber);
            if (order == null)
            {
                return Json(new { response = "Order not found." });
            }
            return Json(new { response = $"Order {request.OrderNumber} is {order.Status}." });
        }

        [HttpGet]
        public async Task<IActionResult> GetTopProducts()
        {
            var topProducts = await _context.OrderItems
                .Where(oi => new[] { "Shipped", "Delivered" }.Contains(oi.Order.Status))
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(3)
                .Join(_context.Products,
                    sale => sale.ProductId,
                    product => product.Id,
                    (sale, product) => new
                    {
                        product.Name,
                        product.Price
                    })
                .ToListAsync();

            if (!topProducts.Any())
            {
                topProducts = await _context.Products
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(3)
                    .Select(p => new
                    {
                        p.Name,
                        p.Price
                    })
                    .ToListAsync();
            }

            return Json(topProducts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class OrderStatusRequest
    {
        public string OrderNumber { get; set; }
    }
}