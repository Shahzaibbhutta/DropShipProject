using DropShipProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DropShipProject.Areas.Supplier.Controllers
{
    [Area("Supplier")]
    [Authorize(Roles = "Supplier")]
    public class ProductController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(DatabaseContext context, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var products = await _context.Products
                .Where(p => p.SupplierId == user.Id)
                .ToListAsync();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile ProductPictureFile)
        {
            ModelState.Remove("SupplierId");
            ModelState.Remove("ProductPicture");

            if (ProductPictureFile != null && ProductPictureFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(ProductPictureFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ProductPictureFile", "Only JPG, JPEG, PNG, and GIF files are allowed.");
                    return View(product);
                }
                if (ProductPictureFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProductPictureFile", "File size must be less than 5MB.");
                    return View(product);
                }

                var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                try
                {
                    Directory.CreateDirectory(uploadsDir);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to create directory: {ex.Message}");
                    return View(product);
                }

                var fileName = $"product-{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProductPictureFile.CopyToAsync(stream);
                    }
                    product.ProductPicture = $"/images/products/{fileName}";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to save file: {ex.Message}");
                    return View(product);
                }
            }
            else
            {
                ModelState.AddModelError("ProductPictureFile", "Please upload a product picture.");
                return View(product);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(product);
            }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile ProductPictureFile)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            ModelState.Remove("SupplierId");
            ModelState.Remove("ProductPicture");

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

            if (ProductPictureFile != null && ProductPictureFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(ProductPictureFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ProductPictureFile", "Only JPG, JPEG, PNG, and GIF files are allowed.");
                    return View(product);
                }
                if (ProductPictureFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProductPictureFile", "File size must be less than 5MB.");
                    return View(product);
                }

                var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                try
                {
                    Directory.CreateDirectory(uploadsDir);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to create directory: {ex.Message}");
                    return View(product);
                }

                if (!string.IsNullOrEmpty(existingProduct.ProductPicture))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ProductPicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new file
                var fileName = $"product-{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProductPictureFile.CopyToAsync(stream);
                    }
                    existingProduct.ProductPicture = $"/images/products/{fileName}";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to save file: {ex.Message}");
                    return View(product);
                }
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.SKU = product.SKU;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManageStock(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products
                .Include(p => p.StockTransactions)
                .FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == user.Id);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new StockManagementViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                CurrentStock = product.Stock,
                StockTransactions = product.StockTransactions.Select(st => new StockTransactionViewModel
                {
                    Id = st.Id,
                    Quantity = st.Quantity,
                    TransactionType = st.TransactionType,
                    TransactionDate = st.TransactionDate,
                    Notes = st.Notes,
                    OrderId = st.OrderId
                }).OrderByDescending(st => st.TransactionDate).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(StockManagementViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == model.ProductId && p.SupplierId == user.Id);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            if (!ModelState.IsValid)
            {
                product.Stock += model.QuantityToAdd;

                var stockTransaction = new StockTransaction
                {
                    ProductId = product.Id,
                    Quantity = model.QuantityToAdd,
                    TransactionType = "StockIn",
                    TransactionDate = DateTime.UtcNow,
                    Notes = model.Notes
                };

                _context.StockTransactions.Add(stockTransaction);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    currentStock = product.Stock,
                    transaction = new
                    {
                        transactionDate = DateTime.UtcNow.ToString("g"),
                        transactionType = "Add",
                        quantity = model.QuantityToAdd,
                        orderId = "-",
                        notes = model.Notes ?? "-"
                    }
                });
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = "Validation failed", errors });
        }

        public async Task<IActionResult> StockOverview()
        {
            var user = await _userManager.GetUserAsync(User);
            var products = await _context.Products
                .Where(p => p.SupplierId == user.Id)
                .Select(p => new StockManagementViewModel
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    SKU = p.SKU,
                    CurrentStock = p.Stock
                })
                .ToListAsync();

            return View(products);
        }
    }
}