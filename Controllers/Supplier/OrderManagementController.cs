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
            if (user == null)
            {
                return NotFound("User not found");
            }

            // If using string IDs (default Identity behavior):
            var orders = await _orderService.GetOrdersForSupplier(user.Id);

            // OR if using integer IDs:
            // var orders = await _orderService.GetOrdersForSupplier(int.Parse(user.Id));

            return View(orders);
        }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                // Verify the order belongs to the current supplier
                var order = await _orderService.GetOrderDetails(orderId);
                var user = await _userManager.GetUserAsync(User);

                if (order == null || order.SupplierId != user.Id)
                {
                    return Forbid();
                }

                await _orderService.UpdateOrderStatus(orderId, status);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Log the error
                return RedirectToAction(nameof(Index));
            }
        }
    }
}