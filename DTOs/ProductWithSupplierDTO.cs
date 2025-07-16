namespace DropShipProject.DTOs
{
    public class ProductWithSupplierDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string SKU { get; set; }
        public string ProductPicture { get; set; }

        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
    }
}
