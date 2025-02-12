using eMarket.Models;
using eMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eMarket.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController<T> : ControllerBase where T : BaseObject
    {
        protected readonly MongoDBService<T> _mongoDBService;

        protected BaseController(IOptions<MongoDBSettings> mongoDBSettings, string collectionName)
        {
            _mongoDBService = new MongoDBService<T>(mongoDBSettings, collectionName);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var items = await _mongoDBService.GetAsync();
                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                return HandleServerError(ex);
            }
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var item = await _mongoDBService.GetAsync(id);
                if (item == null)
                {
                    return NotFound(new { error = $"{typeof(T).Name} with ID {id} not found." });
                }
                return Ok(new {data = item });
            }
            catch (Exception ex)
            {
                return HandleServerError(ex);
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] T item)
        {
            try
            {
                await _mongoDBService.PostAsync(item);
                return Ok(new {message = $"{typeof(T).Name} created successfully." });
            }
            catch (Exception ex)
            {
                return HandleServerError(ex);
            }
        }

        [HttpPut("{id:length(24)}")]
        public virtual async Task<IActionResult> Update(string id, [FromBody] T item)
        {
            try
            {
                var originalItem = await _mongoDBService.GetAsync(id);
                if (originalItem == null)
                {
                    return NotFound(new { message = $"{typeof(T).Name} with ID {id} not found." });
                }

                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name == "Id") continue;

                    var newValue = prop.GetValue(item);
                    var oldValue = prop.GetValue(originalItem);

                    if (newValue != null && prop.CanWrite)
                    {
                        if (!object.Equals(newValue, oldValue))
                        {
                            prop.SetValue(originalItem, newValue);
                        }
                    }
                }

                await _mongoDBService.UpdateAsync(id, originalItem);
                return Ok(new { message = $"{typeof(T).Name} updated successfully." });
            }
            catch (Exception ex)
            {
                return HandleServerError(ex);
            }
        }


        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var existingItem = await _mongoDBService.GetAsync(id);
                if (existingItem == null)
                {
                    return NotFound(new {error = $"{typeof(T).Name} with ID {id} not found." });
                }

                await _mongoDBService.DeleteAsync(id);
                return Ok(new {message = $"{typeof(T).Name} deleted successfully." });
            }
            catch (Exception ex)
            {
                return HandleServerError(ex);
            }
        }

        private IActionResult HandleServerError(Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error." + ex.Message });
        }
    }
}
