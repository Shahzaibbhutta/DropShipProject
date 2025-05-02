using DropShipProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DropShipProject.Models; // Make sure to include your User model namespace

namespace DropShipProject.Controllers.DropShipper
{
    [Area("DropShipper")]
    [Authorize(Roles = "DropShipper")]
    public class DashboardController : Controller
    {
        private readonly OrderService _orderService;
        private readonly UserManager<User> _userManager;

        public DashboardController(OrderService orderService, UserManager<User> userManager)
        {
            _orderService = orderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var orders = await _orderService.GetOrdersForDropShipper(user.Id);
            return View(orders.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByStatus()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                var orders = await _orderService.GetOrdersForDropShipper(user.Id);
                var statusCounts = new
                {
                    pending = orders.Count(o => o.Status == "Pending"),
                    processing = orders.Count(o => o.Status == "Processing"),
                    shipped = orders.Count(o => o.Status == "Shipped"),
                    delivered = orders.Count(o => o.Status == "Delivered"),
                    @return = orders.Count(o => o.Status == "Return"),
                    cancelled = orders.Count(o => o.Status == "Cancelled")
                };

                return Json(statusCounts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrdersByStatus: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch orders by status" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderAmountOverTime()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                var orders = await _orderService.GetOrdersForDropShipper(user.Id);
                var groupedOrders = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        totalAmount = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(g => g.date)
                    .ToList();

                return Json(groupedOrders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrderAmountOverTime: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch order amount over time" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatusDistribution()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                var orders = await _orderService.GetOrdersForDropShipper(user.Id);
                var statusCounts = new
                {
                    pending = orders.Count(o => o.Status == "Pending"),
                    processing = orders.Count(o => o.Status == "Processing"),
                    shipped = orders.Count(o => o.Status == "Shipped"),
                    delivered = orders.Count(o => o.Status == "Delivered"),
                    @return = orders.Count(o => o.Status == "Return"),
                    cancelled = orders.Count(o => o.Status == "Cancelled")
                };

                return Json(statusCounts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrderStatusDistribution: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch order status distribution" });
            }
        }
    }
}