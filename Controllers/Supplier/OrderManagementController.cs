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
        private readonly IEmailService _emailService;

        public OrderManagementController(
            OrderService orderService,
            UserManager<User> userManager,
            IEmailService emailService)
        {
            _orderService = orderService;
            _userManager = userManager;
            _emailService = emailService;
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
                }

                Console.WriteLine($"Filtered orders count: {orders.Count}");

                var result = orders.Select(o => new
                {
                    id = o.Id,
                    orderNumber = o.OrderNumber,
                    dropShipperCompanyName = o.DropShipper?.CompanyName,
                    orderDate = o.OrderDate.ToString("o"),
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

                if (order.DropShipper?.Email != null)
                {
                    var emailSubject = $"Order #{order.OrderNumber} – Status Update Notification";

                    var emailBody = $@"
                    <html>
                      <body style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                        <p>Dear <strong>{order.DropShipper.CompanyName}</strong>,</p>

                        <p>We hope this message finds you well.</p>

                        <p>
                          This is to inform you that the status of your order 
                          <strong>#{order.OrderNumber}</strong> has been updated.
                        </p>

                        <p>
                          <strong>Status:</strong> <span style='color: #2E86C1;'>{status}</span>
                        </p>

                        <h4 style='margin-top: 30px; margin-bottom: 10px;'>Order Summary</h4>
                        <table style='border-collapse: collapse; width: 100%; max-width: 500px;'>
                          <tr>
                            <td style='padding: 8px; font-weight: bold;'>Order Number:</td>
                            <td style='padding: 8px;'>{order.OrderNumber}</td>
                          </tr>
                          <tr style='background-color: #f9f9f9;'>
                            <td style='padding: 8px; font-weight: bold;'>Order Date:</td>
                            <td style='padding: 8px;'>{order.OrderDate:yyyy-MM-dd}</td>
                          </tr>
                          <tr>
                            <td style='padding: 8px; font-weight: bold;'>Total Amount:</td>
                            <td style='padding: 8px;'>{order.TotalAmount:C}</td>
                          </tr>
                        </table>

                        <p style='margin-top: 30px;'>If you have any questions or need further assistance, feel free to contact us.</p>

                        <p>Thank you for your continued partnership.</p>

                        <p style='margin-top: 30px;'>Warm regards,<br/>
                        <strong>ZAH Dropshipper Team</strong></p>
                      </body>
                    </html>";


                    try
                    {
                        await _emailService.SendEmailAsync(order.DropShipper.Email, emailSubject, emailBody,true);
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Failed to send email: {emailEx.Message}");
                        // Log the error but don't fail the status update
                    }
                }
                else
                {
                    Console.WriteLine("No dropshipper email found for order.");
                }

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