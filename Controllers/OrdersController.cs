using System.IdentityModel.Tokens.Jwt;
using eMarket.Models;
using eMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eMarket.Controllers
{
    public class OrdersController : BaseController<Order>
    {
        private readonly MongoDBService<Order> _orderService;
        private readonly MongoDBService<User> _userService;
        private readonly MongoDBService<Product> _productService;

        public OrdersController(IOptions<MongoDBSettings> mongoDBSettings)
            : base(mongoDBSettings, mongoDBSettings.Value.CollectionOrderName)
        {
            _orderService = new MongoDBService<Order>(
                mongoDBSettings, mongoDBSettings.Value.CollectionOrderName);
            _userService = new MongoDBService<User>(
                mongoDBSettings, mongoDBSettings.Value.CollectionUserName);
            _productService = new MongoDBService<Product>(
                mongoDBSettings, mongoDBSettings.Value.CollectionProductName);
        }

        public override async Task<IActionResult> Create([FromBody] Order order)
        {
            order.User = "";

            var token = Request.Cookies.TryGetValue("AuthToken", out var authToken) ? authToken : null;

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    order.User = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value
                                 ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
                }
                catch (Exception)
                {
                }
            }

            order.Date = order.Date == default ? DateTime.UtcNow : order.Date;

            for (int i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                if (await _productService.GetAsync(item.ProductId) == null)
                    return BadRequest($"Product at index {i} with ID '{item.ProductId}' does not exist.");
            }
            return await base.Create(order);
        }

        [HttpPut("{id:length(24)}")]
        public override async Task<IActionResult> Update(string id, [FromBody] Order order)
        {
            for (int i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                if (await _productService.GetAsync(item.ProductId) == null)
                    return BadRequest($"Product at index {i} with ID '{item.ProductId}' does not exist.");
            }

            return await base.Update(id, order);
        }

        [HttpGet("report")]
        [HttpGet("report/{id:length(24)}")]
        public async Task<IActionResult> GetOrders(string? id = null)
        {
            try
            {
                var orders = await _orderService.GetAsync();
                var users = await _userService.GetAsync();
                var products = await _productService.GetAsync();

                var userMap = users.ToDictionary(u => u.Id ?? "", u => u.Username ?? "Guest");
                var productMap = products.ToDictionary(p => p.Id ?? "", p => (decimal)p.Price);

                if (string.IsNullOrEmpty(id))
                {
                    var orderList = orders
                        .OrderByDescending(o => o.Date)
                        .Take(100)
                        .Select(o => new
                        {
                            o.Id,
                            o.Date,
                            UserName = userMap.GetValueOrDefault(o.User, "Guest"),
                            TotalPrice = o.Items.Sum(i => productMap.GetValueOrDefault(i.ProductId, 0) * i.Quantity),
                            o.IsProcessed
                        })
                        .ToList();

                    return Ok(new { data = orderList });
                }
                else
                {
                    var order = orders.FirstOrDefault(o => o.Id == id);
                    if (order == null)
                    {
                        return NotFound(new { error = $"Order with ID {id} not found." });
                    }

                    var orderDetail = new
                    {
                        order.Id,
                        order.Date,
                        UserName = userMap.GetValueOrDefault(order.User, "Guest"),
                        TotalPrice = order.Items.Sum(i => productMap.GetValueOrDefault(i.ProductId, 0) * i.Quantity),
                        order.IsProcessed,
                        Items = order.Items.Select(i => new
                        {
                            i.ProductId,
                            ProductName = products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? "Unknown",
                            Description = products.FirstOrDefault(p => p.Id == i.ProductId)?.Description ?? "Unknown",
                            i.Quantity,
                            Price = productMap.GetValueOrDefault(i.ProductId, 0),
                            Total = productMap.GetValueOrDefault(i.ProductId, 0) * i.Quantity
                        }).ToList()
                    };

                    return Ok(new { data = orderDetail });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }
    }
}
