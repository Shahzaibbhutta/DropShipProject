using DropShipProject.Models;
using DropShipProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DropShipProject.Controllers.DropShipper
{
    [Area("DropShipper")]
    [Authorize(Roles = "DropShipper")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly AccountService _accountService;
        private readonly UserManager<User> _userManager;

        public OrderController(
            OrderService orderService,
            AccountService accountService,
            UserManager<User> userManager)
        {
            _orderService = orderService;
            _accountService = accountService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var orders = await _orderService.GetOrdersForDropShipper(user.Id);
            return View(orders.ToList()); // Convert to List<Order>
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetails(id);
            if (order == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (order.DropShipperId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            var suppliers = await _accountService.GetAllSuppliers(); // Now works with both versions
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "CompanyName");

            var model = new CreateOrderViewModel
            {
                Items = new List<OrderItemViewModel> { new OrderItemViewModel() }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var suppliers = await _accountService.GetAllSuppliers(); // Now works with both versions
                ViewBag.Suppliers = new SelectList(suppliers, "Id", "CompanyName");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            var order = await _orderService.CreateOrder(model, user.Id);
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}