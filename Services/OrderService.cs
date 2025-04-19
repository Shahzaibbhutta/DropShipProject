using DropShipProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                OrderDate = DateTime.UtcNow,
                PaymentMethod = model.PaymentMethod,
                ShippingAddress = model.ShippingAddress,
                City = model.City
            };

            foreach (var item in model.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.SupplierId != model.SupplierId)
                {
                    throw new InvalidOperationException("Invalid product or supplier mismatch.");
                }
                if (product.Stock < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}.");
                }
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
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
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetOrdersForDropShipper(int dropShipperId)
        {
            return await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.DropShipperId == dropShipperId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersForSupplier(int supplierId)
        {
            return await _context.Orders
                .Include(o => o.DropShipper)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
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