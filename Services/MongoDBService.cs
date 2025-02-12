using eMarket.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace eMarket.Services
{
    public class MongoDBService<T> where T : BaseObject
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings, string collectionName)
        {
            var client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _collection = database.GetCollection<T>(collectionName);
        }

        public async Task<List<T>> GetAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
        public async Task<T?> GetAsync(string id)
        {
            return await _collection.Find(obj => obj.Id == id).FirstOrDefaultAsync();
        }

        public async Task PostAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(string id, T entity)
        {
            await _collection.ReplaceOneAsync(obj => obj.Id == id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(obj => obj.Id == id);
        }
    }
}