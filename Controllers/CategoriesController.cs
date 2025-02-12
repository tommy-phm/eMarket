using eMarket.Models;
using eMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eMarket.Controllers
{
    public class CategoriesController : BaseController<Category>
    {
        public CategoriesController(IOptions<MongoDBSettings> mongoDBSettings)
            : base(mongoDBSettings, mongoDBSettings.Value.CollectionCategoryName)
        {
        }
    }
}