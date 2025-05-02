using DropShipProject.Models;
using DropShipProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DropShipProject.Controllers.Supplier
{
    [Area("Supplier")]
    [Authorize(Roles = "Supplier")]
    public class OrderManagementController : Controller
    {
        private readonly OrderService _orderService;
        private readonly UserManager<User> _userManager;

        public OrderManagementController(
      OrderService orderService,
      UserManager<User> userManager)
        {
            _orderService = orderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var orders = await _orderService.GetOrdersForSupplier(user.Id);
            return View(orders.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredOrders(string[] statuses)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                Console.WriteLine("Received statuses: " + (statuses != null ? string.Join(", ", statuses) : "none"));

                var orders = await _orderService.GetOrdersForSupplier(user.Id);

                if (statuses != null && statuses.Any())
                {
                    orders = orders.Where(o => statuses.Contains(o.Status, StringComparer.OrdinalIgnoreCase)).ToList();
                } // If statuses is empty, return all orders

                Console.WriteLine($"Filtered orders count: {orders.Count}");

                var result = orders.Select(o => new
                {
                    id = o.Id,
                    orderNumber = o.OrderNumber,
                    dropShipperCompanyName = o.DropShipper?.CompanyName,
                    orderDate = o.OrderDate.ToString("o"), // ISO format for JavaScript
                    totalAmount = o.TotalAmount,
                    status = o.Status
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFilteredOrders: {ex.Message}");
                return StatusCode(500, new { error = "Failed to filter orders" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetails(id);
            if (order == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (order.SupplierId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                var order = await _orderService.GetOrderDetails(orderId);
                var user = await _userManager.GetUserAsync(User);

                if (order == null || order.SupplierId != user.Id)
                {
                    return Json(new { success = false, message = "Unauthorized or order not found." });
                }

                await _orderService.UpdateOrderStatus(orderId, status);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateStatus Error: {ex.Message}");
                return Json(new { success = false, message = "Failed to update status." });
            }
        }

    }
}