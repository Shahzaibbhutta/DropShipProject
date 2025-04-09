using DropShipProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DropShipProject.Services
{
    public class OrderService
    {
        private readonly DatabaseContext _context;

        public OrderService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrder(CreateOrderViewModel model, int dropShipperId)
        {
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                DropShipperId = dropShipperId,
                SupplierId = model.SupplierId,
                Notes = model.Notes,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            };

            foreach (var item in model.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            order.TotalAmount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> GetOrderDetails(int id)
        {
            return await _context.Orders
                .Include(o => o.DropShipper)
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetOrdersForDropShipper(int dropShipperId)
        {
            return await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                .Where(o => o.DropShipperId == dropShipperId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersForSupplier(int supplierId)
        {
            return await _context.Orders
                .Include(o => o.DropShipper)
                .Include(o => o.OrderItems)
                .Where(o => o.SupplierId == supplierId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        private string GenerateOrderNumber()
        {
            return "ORD-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" +
                   Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
        }
    }
}