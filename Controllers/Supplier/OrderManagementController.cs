using DropShipProject.Models;
using DropShipProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
            return View(orders.ToList()); // Convert to List<Order>
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetails(id);
            if (order == null)
            {
                return NotFound();
            }

            // Verify the order belongs to the current supplier
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