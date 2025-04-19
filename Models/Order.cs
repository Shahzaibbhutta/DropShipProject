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
        public string PaymentMethod { get; set; } = "COD"; // Default to Cash on Delivery
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public string City { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; } // Reference Product instead of ProductName
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Still stored for order history
    }

    public class CreateOrderViewModel
    {
        public int SupplierId { get; set; }
        public string Notes { get; set; }
        [Required]
        public string ShippingAddress { get; set; }
        [Required]
        public string City { get; set; }
        public string PaymentMethod { get; set; } = "COD"; // Default to COD
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; } // Select Product instead of entering name
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } // Populated from Product
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
        public int SupplierId { get; set; }
        public User Supplier { get; set; }
    }
}