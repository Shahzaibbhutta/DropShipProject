using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DropShipProject.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int DropShipperId { get; set; }
        public User DropShipper { get; set; }
        public int SupplierId { get; set; }
        public User Supplier { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        [Required]
        public string PaymentMethod { get; set; } = "COD";
        [Required]
        public string CustomerName { get; set; } // New field
        [Required]
        public string CustomerMobile { get; set; } // New field
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string CourierService { get; set; } // New field
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CreateOrderViewModel
    {
        public int SupplierId { get; set; }
        public string Notes { get; set; }
        [Required]
        public string CustomerName { get; set; } // New field
        [Required]
        public string CustomerMobile { get; set; } // New field
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string CourierService { get; set; } // New field
        public string PaymentMethod { get; set; } = "COD";
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } // New field for SKU input
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        [Required]
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }
        [Required]
        public string SKU { get; set; }
        [Required]
        public string ProductPicture { get; set; }
        [Required] // Explicitly mark as required (already implied by int)
        public int SupplierId { get; set; }
        public User Supplier { get; set; } // Navigation property, optional
    }
}