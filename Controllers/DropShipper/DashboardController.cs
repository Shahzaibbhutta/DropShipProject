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

        public DashboardController(OrderService orderService,UserManager<User> userManager)
        {
            _orderService = orderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Get orders for the current dropshipper
            var orders = await _orderService.GetOrdersForDropShipper(user.Id);
            return View(orders);
        }
    }
}