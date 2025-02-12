namespace eMarket.Models
{
    public class MongoDBSettings
    {
        public string ConnectionString {get; set;} = null!;
        public string DatabaseName { get; set; } = null!;
        public string CollectionCategoryName { get; set; } = null!;
        public string CollectionProductName { get; set; } = null!;
        public string CollectionUserName { get; set; } = null!;
        public string CollectionOrderName { get; set; } = null!;
    }
}
