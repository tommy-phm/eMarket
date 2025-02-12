using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eMarket.Models
{
    public abstract class BaseObject
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyOrder(-1)]
        public string? Id { get; set; }
    }

    public class Category : BaseObject
    {
        [BsonElement("name")]
        public string Name { get; set; }

        public Category(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class Product : BaseObject
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("category")]
        public string Category { get; set; }

        public Product(string id, string name, string description, decimal price, string category)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
            Category = category;
        }
    }

    public class User : BaseObject
    {
        [BsonElement("username")]
        public string Username { get; set; }
        [BsonElement("password")]
        public string Password { get; set; }
        [BsonElement("isAdmin")]
        public bool IsAdmin { get; set; }

        public User(string username, string password, bool isAdmin)
        {
            Username = username;
            Password = password;
            IsAdmin = isAdmin;
        }
    }

    public class OrderItem
    {
        [BsonElement("productId")]
        public required string ProductId { get; set; } 

        [BsonElement("quantity")]
        public int Quantity { get; set; }
    }

    public class Order : BaseObject
    {
        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("user")]
        public String User { get; set; }

        [BsonElement("items")]
        public List<OrderItem> Items { get; set; }

        [BsonElement("isProcessed")]
        public bool IsProcessed { get; set; } = false;

        public Order(string id, DateTime date, String user, List<OrderItem> items, bool isProcessed)
        {
            Id = id;
            Date = date;
            User = user;
            Items = items;
            IsProcessed = isProcessed;
        }
    }

    public class OrderDetail : Order
    {
        public string Name { get; set; }
        public decimal TotalPrice { get; set; }

        public OrderDetail(string id, DateTime date, string user, List<OrderItem> items, bool isProcessed, string name, decimal totalPrice)
            : base(id, date, user, items, isProcessed)
        {
            Name = name;
            TotalPrice = totalPrice;
        }
    }
}
