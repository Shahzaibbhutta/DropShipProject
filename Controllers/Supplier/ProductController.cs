using DropShipProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DropShipProject.Areas.Supplier.Controllers
{
    [Area("Supplier")]
    [Authorize(Roles = "Supplier")]
    public class ProductController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public ProductController(DatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var products = _context.Products.Where(p => p.SupplierId == user.Id).ToList();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(product);
            //}

            var user = await _userManager.GetUserAsync(User);
            product.SupplierId = user.Id;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (product.SupplierId != user.Id)
            {
                return Forbid();
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (existingProduct.SupplierId != user.Id)
            {
                return Forbid();
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}