using eMarket.Models;
using eMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eMarket.Controllers
{
    public class ProductsController : BaseController<Product>
    {
        private readonly MongoDBService<Product> _productService;
        private readonly MongoDBService<Category> _categoryService;

        public ProductsController(IOptions<MongoDBSettings> mongoDBSettings)
            : base(mongoDBSettings, mongoDBSettings.Value.CollectionProductName)
        {
            _categoryService = new MongoDBService<Category>(
                mongoDBSettings, mongoDBSettings.Value.CollectionCategoryName);
            _productService = new MongoDBService<Product>(
                mongoDBSettings, mongoDBSettings.Value.CollectionProductName);
        }

        public override async Task<IActionResult> Create([FromBody] Product product)
        {
            var categoryExists = await _categoryService.GetAsync(product.Category);
            if (categoryExists == null)
            {
                return BadRequest($"Category with ID '{product.Category}' does not exist.");
            }
            return await base.Create(product);
        }

        [HttpPut("{id:length(24)}")]
        public override async Task<IActionResult> Update(string id, [FromBody] Product product)
        {
            var categoryExists = await _categoryService.GetAsync(product.Category);
            if (categoryExists == null)
            {
                return BadRequest($"Category with ID '{product.Category}' does not exist.");
            }
            return await base.Update(id, product);
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            try
            {
                var products = await _productService.GetAsync() ?? new List<Product>();
                var categories = await _categoryService.GetAsync() ?? new List<Category>();

                var catalog = categories
                    .Select(c => new
                    {
                        CategoryName = c.Name,
                        Products = products.Where(p => p.Category == c.Id).ToList()
                    })
                    .Where(c => c.Products.Any())
                    .ToDictionary(c => c.CategoryName, c => c.Products);

                var uncategorizedProducts = products
                    .Where(p => string.IsNullOrEmpty(p.Category) || !categories.Any(c => c.Id == p.Category))
                    .ToList();

                if (uncategorizedProducts.Any())
                {
                    catalog["Other"] = uncategorizedProducts;
                }

                return Ok(new { catalog });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }
    }
}