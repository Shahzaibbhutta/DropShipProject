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
    public class DashboardController : Controller
    {
        private readonly OrderService _orderService;
        private readonly UserManager<User> _userManager;

        public DashboardController(
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

            // Directly use the string ID (default Identity behavior)
            var orders = await _orderService.GetOrdersForSupplier(user.Id);
            return View(orders);
        }
    }
}