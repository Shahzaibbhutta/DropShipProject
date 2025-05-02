using DropShipProject.Models;
using DropShipProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DropShipProject.Areas.DropShipper.Controllers
{
    [Area("DropShipper")]
    [Authorize(Roles = "DropShipper")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly AccountService _accountService;
        private readonly UserManager<User> _userManager;
        private readonly DatabaseContext _context;

        public OrderController(
            OrderService orderService,
            AccountService accountService,
            UserManager<User> userManager,
            DatabaseContext context)
        {
            _orderService = orderService;
            _accountService = accountService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var orders = await _orderService.GetOrdersForDropShipper(user.Id);
            return View(orders.ToList());
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderDetails(id);
                if (order == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null || order.DropShipperId != user.Id)
                {
                    return Forbid();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        public async Task<IActionResult> Create()
        {
            var suppliers = await _accountService.GetAllSuppliers();
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "UserName");
            ViewBag.CourierServices = new List<string> { "FedEx", "UPS", "DHL", "Local Courier" }; // Predefined options
            var model = new CreateOrderViewModel
            {
                Items = new List<OrderItemViewModel> { new OrderItemViewModel() }
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsBySupplier(int supplierId)
        {
            var products = await _context.Products
                .Where(p => p.SupplierId == supplierId)
                .Select(p => new { p.Id, p.SKU, p.Name, p.Price, p.ProductPicture })
                .ToListAsync();
            return Json(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var suppliers = await _accountService.GetAllSuppliers();
                ViewBag.Suppliers = new SelectList(suppliers, "Id", "UserName", model.SupplierId);
                ViewBag.CourierServices = new List<string> { "FedEx", "UPS", "DHL", "Local Courier" };
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            try
            {
                var order = await _orderService.CreateOrder(model, user.Id);
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var suppliers = await _accountService.GetAllSuppliers();
                ViewBag.Suppliers = new SelectList(suppliers, "Id", "UserName", model.SupplierId);
                ViewBag.CourierServices = new List<string> { "FedEx", "UPS", "DHL", "Local Courier" };
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> FilterByStatus(string[] statuses)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var orders = await _orderService.GetOrdersForDropShipper(user.Id);

            if (statuses != null && statuses.Length > 0)
            {
                orders = orders.Where(o => statuses.Contains(o.Status, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            var filteredOrders = orders.Select(o => new
            {
                id = o.Id,
                orderNumber = o.OrderNumber,
                supplierCompanyName = o.Supplier?.CompanyName ?? "N/A",
                orderDate = o.OrderDate.ToString("o"),
                totalAmount = o.TotalAmount,
                status = o.Status
            });

            return Json(filteredOrders);
        }
    }
}