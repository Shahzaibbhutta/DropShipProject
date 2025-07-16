using DropShipProject.DTOs;
using DropShipProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DropShipProject.Controllers.Supplier
{
    [Route("api/supplier/products")]
    [ApiController]
    [Authorize(Roles = "Supplier")]
    public class SupplierProductApiController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public SupplierProductApiController(DatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products
                .Select(p => new ProductWithSupplierDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock,
                    SKU = p.SKU,
                    ProductPicture = p.ProductPicture,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.CompanyName // Or ContactPerson or UserName
                })
                .ToListAsync();

            return Ok(products);
        }

    }

}
