using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RetailPOS.Models;

namespace RetailPOS.Services;

public class DatabaseService
{
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _lock = new();
    private DatabaseSchema _currentDb;

    public DatabaseService()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _dataFilePath = Path.Combine(appDir, "retailpos_data.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _currentDb = new DatabaseSchema
        {
            Settings = new StoreSettings(),
            Categories = GetDefaultCategories(),
            Users = GetDefaultUsers(),
            Products = GetDefaultProducts(),
            Customers = GetDefaultCustomers(),
            Sales = [],
            PointTransactions = []
        };

        Load();
    }

    private void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    var loaded = JsonSerializer.Deserialize<DatabaseSchema>(json, _jsonOptions);
                    if (loaded != null)
                    {
                        if (loaded.Settings == null) loaded.Settings = new StoreSettings();
                        if (loaded.Categories == null || loaded.Categories.Count == 0) loaded.Categories = GetDefaultCategories();
                        if (loaded.Users == null || loaded.Users.Count == 0) loaded.Users = GetDefaultUsers();
                        if (loaded.Products == null || loaded.Products.Count == 0) loaded.Products = GetDefaultProducts();
                        if (loaded.Customers == null || loaded.Customers.Count == 0) loaded.Customers = GetDefaultCustomers();
                        if (loaded.Sales == null) loaded.Sales = [];
                        if (loaded.PointTransactions == null) loaded.PointTransactions = [];

                        _currentDb = loaded;
                    }
                }
                else
                {
                    Persist();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading database: {ex.Message}");
            }
        }
    }

    private void Persist()
    {
        try
        {
            var json = JsonSerializer.Serialize(_currentDb, _jsonOptions);
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving database: {ex.Message}");
        }
    }

    private static List<CategoryItem> GetDefaultCategories()
    {
        return
        [
            new CategoryItem { Id = "cat_1", Name = "Produce", Description = "Fresh vegetables, fruits, and greens" },
            new CategoryItem { Id = "cat_2", Name = "Dairy & Eggs", Description = "Fresh milk, eggs, cheeses, and yogurt" },
            new CategoryItem { Id = "cat_3", Name = "Bakery", Description = "Fresh breads, rolls, pastries, and buns" },
            new CategoryItem { Id = "cat_4", Name = "Beverages", Description = "Juices, sodas, waters, tea, and coffees" },
            new CategoryItem { Id = "cat_5", Name = "Snacks", Description = "Chips, candies, chocolates, and nuts" },
            new CategoryItem { Id = "cat_6", Name = "Pantry", Description = "Rice, pastas, olive oils, sauces, and spices" },
            new CategoryItem { Id = "cat_7", Name = "Household", Description = "Cleaning, detergents, paper towels, and soaps" }
        ];
    }

    private static List<LoyalCustomer> GetDefaultCustomers()
    {
        return
        [
            new LoyalCustomer
            {
                Id = "c1",
                PhoneNumber = "555-0101",
                FullName = "John Doe",
                Email = "john.doe@example.com",
                Tier = "Gold",
                Points = 350,
                TotalSpent = 420.50m,
                JoinedAt = DateTime.Now.AddMonths(-6)
            },
            new LoyalCustomer
            {
                Id = "c2",
                PhoneNumber = "555-0202",
                FullName = "Emily Rose",
                Email = "emily.rose@example.com",
                Tier = "Silver",
                Points = 140,
                TotalSpent = 185.00m,
                JoinedAt = DateTime.Now.AddMonths(-3)
            },
            new LoyalCustomer
            {
                Id = "c3",
                PhoneNumber = "555-0303",
                FullName = "Michael Brown",
                Email = "mbrown@example.com",
                Tier = "Platinum",
                Points = 820,
                TotalSpent = 1250.00m,
                JoinedAt = DateTime.Now.AddYears(-1)
            },
            new LoyalCustomer
            {
                Id = "c4",
                PhoneNumber = "555-0404",
                FullName = "Sophia Wilson",
                Email = "sophia.w@example.com",
                Tier = "Bronze",
                Points = 45,
                TotalSpent = 48.90m,
                JoinedAt = DateTime.Now.AddDays(-10)
            }
        ];
    }

    private static List<User> GetDefaultUsers()
    {
        return
        [
            new User
            {
                Id = "u_admin",
                Username = "admin",
                FullName = "Store Administrator",
                Pin = "1234",
                Role = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-60)
            },
            new User
            {
                Id = "u_sarah",
                Username = "sarah",
                FullName = "Sarah Jenkins",
                Pin = "1111",
                Role = "Cashier",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            new User
            {
                Id = "u_david",
                Username = "david",
                FullName = "David Miller",
                Pin = "2222",
                Role = "Cashier",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-15)
            }
        ];
    }

    private static List<Product> GetDefaultProducts()
    {
        return
        [
            // Dairy & Eggs
            new Product { Id = "p101", Barcode = "1001", Name = "Whole Fresh Milk 1L", Category = "Dairy & Eggs", Price = 2.49m, Cost = 1.60m, Stock = 42, MinStock = 10, Unit = "bottle" },
            new Product { Id = "p102", Barcode = "1002", Name = "Farm Fresh Eggs (12-pk)", Category = "Dairy & Eggs", Price = 3.99m, Cost = 2.50m, Stock = 28, MinStock = 8, Unit = "carton" },
            new Product { Id = "p103", Barcode = "1003", Name = "Cheddar Cheese Block 250g", Category = "Dairy & Eggs", Price = 4.25m, Cost = 2.80m, Stock = 15, MinStock = 5, Unit = "block" },
            new Product { Id = "p104", Barcode = "1004", Name = "Salted Butter 200g", Category = "Dairy & Eggs", Price = 2.89m, Cost = 1.90m, Stock = 20, MinStock = 6, Unit = "pack" },
            new Product { Id = "p105", Barcode = "1005", Name = "Greek Yogurt 500g", Category = "Dairy & Eggs", Price = 3.49m, Cost = 2.10m, Stock = 14, MinStock = 6, Unit = "tub" },

            // Produce
            new Product { Id = "p201", Barcode = "2001", Name = "Fresh Bananas (1kg)", Category = "Produce", Price = 1.99m, Cost = 1.10m, Stock = 35, MinStock = 12, Unit = "kg" },
            new Product { Id = "p202", Barcode = "2002", Name = "Red Gala Apples (1kg)", Category = "Produce", Price = 2.99m, Cost = 1.70m, Stock = 24, MinStock = 10, Unit = "kg" },
            new Product { Id = "p203", Barcode = "2003", Name = "Seedless Watermelon", Category = "Produce", Price = 5.49m, Cost = 3.20m, Stock = 7, MinStock = 5, Unit = "each" },
            new Product { Id = "p204", Barcode = "2004", Name = "Organic Vine Tomatoes (500g)", Category = "Produce", Price = 2.29m, Cost = 1.30m, Stock = 18, MinStock = 6, Unit = "pack" },
            new Product { Id = "p205", Barcode = "2005", Name = "Yellow Onions (1kg bag)", Category = "Produce", Price = 1.79m, Cost = 0.90m, Stock = 30, MinStock = 10, Unit = "bag" },

            // Bakery
            new Product { Id = "p301", Barcode = "3001", Name = "Whole Grain Bread 650g", Category = "Bakery", Price = 2.69m, Cost = 1.40m, Stock = 16, MinStock = 6, Unit = "loaf" },
            new Product { Id = "p302", Barcode = "3002", Name = "Butter Croissants (4-pk)", Category = "Bakery", Price = 3.49m, Cost = 2.00m, Stock = 9, MinStock = 5, Unit = "pack" },
            new Product { Id = "p303", Barcode = "3003", Name = "French Baguette", Category = "Bakery", Price = 1.89m, Cost = 0.90m, Stock = 14, MinStock = 5, Unit = "loaf" },

            // Beverages
            new Product { Id = "p401", Barcode = "4001", Name = "Coca-Cola Classic 500ml", Category = "Beverages", Price = 1.69m, Cost = 0.95m, Stock = 50, MinStock = 15, Unit = "bottle" },
            new Product { Id = "p402", Barcode = "4002", Name = "Pure Orange Juice 1L", Category = "Beverages", Price = 3.19m, Cost = 1.85m, Stock = 19, MinStock = 8, Unit = "bottle" },
            new Product { Id = "p403", Barcode = "4003", Name = "Natural Spring Water 1.5L", Category = "Beverages", Price = 0.99m, Cost = 0.45m, Stock = 65, MinStock = 20, Unit = "bottle" },
            new Product { Id = "p404", Barcode = "4004", Name = "Dark Roast Ground Coffee 250g", Category = "Beverages", Price = 5.99m, Cost = 3.80m, Stock = 12, MinStock = 5, Unit = "bag" },

            // Snacks
            new Product { Id = "p501", Barcode = "5001", Name = "Classic Sea Salt Potato Chips 150g", Category = "Snacks", Price = 2.49m, Cost = 1.20m, Stock = 30, MinStock = 10, Unit = "bag" },
            new Product { Id = "p502", Barcode = "5002", Name = "Milk Chocolate Bar 100g", Category = "Snacks", Price = 1.79m, Cost = 0.90m, Stock = 40, MinStock = 12, Unit = "bar" },
            new Product { Id = "p503", Barcode = "5003", Name = "Roasted Almonds 200g", Category = "Snacks", Price = 4.89m, Cost = 3.10m, Stock = 8, MinStock = 5, Unit = "pack" },

            // Pantry
            new Product { Id = "p601", Barcode = "6001", Name = "Basmati Rice 2kg", Category = "Pantry", Price = 6.49m, Cost = 4.00m, Stock = 22, MinStock = 8, Unit = "bag" },
            new Product { Id = "p602", Barcode = "6002", Name = "Italian Penne Pasta 500g", Category = "Pantry", Price = 1.49m, Cost = 0.75m, Stock = 32, MinStock = 10, Unit = "pack" },
            new Product { Id = "p603", Barcode = "6003", Name = "Extra Virgin Olive Oil 750ml", Category = "Pantry", Price = 8.99m, Cost = 5.80m, Stock = 14, MinStock = 6, Unit = "bottle" },
            new Product { Id = "p604", Barcode = "6004", Name = "Traditional Tomato Sauce 400g", Category = "Pantry", Price = 1.89m, Cost = 0.95m, Stock = 25, MinStock = 8, Unit = "can" },

            // Household
            new Product { Id = "p701", Barcode = "7001", Name = "Dishwashing Liquid 750ml", Category = "Household", Price = 2.99m, Cost = 1.60m, Stock = 17, MinStock = 6, Unit = "bottle" },
            new Product { Id = "p702", Barcode = "7002", Name = "Paper Kitchen Towels (2-pk)", Category = "Household", Price = 3.49m, Cost = 1.90m, Stock = 4, MinStock = 8, Unit = "pack" }
        ];
    }

    #region Settings (Master Data)
    public StoreSettings GetSettings()
    {
        lock (_lock)
        {
            return _currentDb.Settings;
        }
    }

    public void SaveSettings(StoreSettings settings)
    {
        lock (_lock)
        {
            _currentDb.Settings = settings;
            Persist();
        }
    }
    #endregion

    #region Categories (Master Data)
    public List<CategoryItem> GetCategories()
    {
        lock (_lock)
        {
            return _currentDb.Categories.OrderBy(c => c.Name).ToList();
        }
    }

    public void SaveCategory(CategoryItem category)
    {
        lock (_lock)
        {
            var idx = _currentDb.Categories.FindIndex(c => c.Id == category.Id);
            if (idx >= 0)
            {
                _currentDb.Categories[idx] = category;
            }
            else
            {
                _currentDb.Categories.Add(category);
            }
            Persist();
        }
    }

    public void DeleteCategory(string categoryId)
    {
        lock (_lock)
        {
            _currentDb.Categories.RemoveAll(c => c.Id == categoryId);
            Persist();
        }
    }
    #endregion

    #region Customer Loyalty
    public List<LoyalCustomer> GetCustomers()
    {
        lock (_lock)
        {
            return _currentDb.Customers.Where(c => c.IsActive).OrderByDescending(c => c.Points).ToList();
        }
    }

    public List<LoyalCustomer> GetAllCustomers()
    {
        lock (_lock)
        {
            return _currentDb.Customers.OrderByDescending(c => c.Points).ToList();
        }
    }

    public LoyalCustomer? GetCustomerByPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var p = phone.Trim().Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");

        lock (_lock)
        {
            return _currentDb.Customers.FirstOrDefault(c =>
                c.IsActive &&
                (c.PhoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Equals(p, StringComparison.OrdinalIgnoreCase) ||
                 c.FullName.Contains(phone.Trim(), StringComparison.OrdinalIgnoreCase)));
        }
    }

    public void SaveCustomer(LoyalCustomer customer)
    {
        lock (_lock)
        {
            var idx = _currentDb.Customers.FindIndex(c => c.Id == customer.Id);
            if (idx >= 0)
            {
                _currentDb.Customers[idx] = customer;
            }
            else
            {
                _currentDb.Customers.Add(customer);
            }
            Persist();
        }
    }

    public void DeleteCustomer(string customerId)
    {
        lock (_lock)
        {
            var customer = _currentDb.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {
                customer.IsActive = false;
                Persist();
            }
        }
    }

    public void AwardPoints(string customerId, int points, string receiptNumber, decimal saleAmount)
    {
        if (points <= 0) return;
        lock (_lock)
        {
            var customer = _currentDb.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {
                customer.Points += points;
                customer.TotalSpent += saleAmount;

                // Auto Tier Upgrade logic:
                // Bronze: < $200
                // Silver: $200 - $500
                // Gold: $500 - $1000
                // Platinum: > $1000
                if (customer.TotalSpent >= 1000m) customer.Tier = "Platinum";
                else if (customer.TotalSpent >= 500m) customer.Tier = "Gold";
                else if (customer.TotalSpent >= 200m) customer.Tier = "Silver";
                else customer.Tier = "Bronze";

                _currentDb.PointTransactions.Insert(0, new PointTransaction
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.FullName,
                    ReceiptNumber = receiptNumber,
                    Timestamp = DateTime.Now,
                    Type = "Earned",
                    PointsDelta = points,
                    Notes = $"Earned on sale #{receiptNumber} (${saleAmount:F2})"
                });

                Persist();
            }
        }
    }

    public bool RedeemPoints(string customerId, int points, string receiptNumber, decimal discountAmount)
    {
        if (points <= 0) return false;
        lock (_lock)
        {
            var customer = _currentDb.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null && customer.Points >= points)
            {
                customer.Points -= points;

                _currentDb.PointTransactions.Insert(0, new PointTransaction
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.FullName,
                    ReceiptNumber = receiptNumber,
                    Timestamp = DateTime.Now,
                    Type = "Redeemed",
                    PointsDelta = -points,
                    Notes = $"Redeemed {points} pts for ${discountAmount:F2} discount on #{receiptNumber}"
                });

                Persist();
                return true;
            }
            return false;
        }
    }

    public List<PointTransaction> GetPointTransactions(string? customerId = null)
    {
        lock (_lock)
        {
            var query = _currentDb.PointTransactions.AsEnumerable();
            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(t => t.CustomerId == customerId);
            }
            return query.OrderByDescending(t => t.Timestamp).ToList();
        }
    }
    #endregion

    #region Products
    public List<Product> GetProducts()
    {
        lock (_lock)
        {
            return _currentDb.Products.Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();
        }
    }

    public List<Product> GetAllProducts()
    {
        lock (_lock)
        {
            return _currentDb.Products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();
        }
    }

    public Product? GetProductByBarcode(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var q = query.Trim();

        lock (_lock)
        {
            return _currentDb.Products.FirstOrDefault(p =>
                p.IsActive && (
                    string.Equals(p.Barcode, q, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Id, q, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public void SaveProduct(Product product)
    {
        lock (_lock)
        {
            var index = _currentDb.Products.FindIndex(p => p.Id == product.Id);
            if (index >= 0)
            {
                _currentDb.Products[index] = product;
            }
            else
            {
                _currentDb.Products.Add(product);
            }
            Persist();
        }
    }

    public void DeleteProduct(string productId)
    {
        lock (_lock)
        {
            var product = _currentDb.Products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                product.IsActive = false;
                Persist();
            }
        }
    }

    public void AdjustStock(string productId, int delta)
    {
        lock (_lock)
        {
            var product = _currentDb.Products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                product.Stock = Math.Max(0, product.Stock + delta);
                Persist();
            }
        }
    }
    #endregion

    #region Users
    public List<User> GetUsers()
    {
        lock (_lock)
        {
            return _currentDb.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
        }
    }

    public List<User> GetAllUsers()
    {
        lock (_lock)
        {
            return _currentDb.Users.OrderBy(u => u.FullName).ToList();
        }
    }

    public User? Authenticate(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return null;
        lock (_lock)
        {
            return _currentDb.Users.FirstOrDefault(u => u.IsActive && u.Pin == pin.Trim());
        }
    }

    public User? AuthenticateUser(string username, string pin)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pin)) return null;
        lock (_lock)
        {
            return _currentDb.Users.FirstOrDefault(u =>
                u.IsActive &&
                string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.Pin == pin.Trim());
        }
    }

    public void SaveUser(User user)
    {
        lock (_lock)
        {
            var index = _currentDb.Users.FindIndex(u => u.Id == user.Id);
            if (index >= 0)
            {
                _currentDb.Users[index] = user;
            }
            else
            {
                _currentDb.Users.Add(user);
            }
            Persist();
        }
    }

    public void DeleteUser(string userId)
    {
        lock (_lock)
        {
            var user = _currentDb.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsActive = false;
                Persist();
            }
        }
    }
    #endregion

    #region Sales
    public List<SaleRecord> GetSales()
    {
        lock (_lock)
        {
            return _currentDb.Sales.OrderByDescending(s => s.Timestamp).ToList();
        }
    }

    public void RecordSale(SaleRecord sale)
    {
        lock (_lock)
        {
            // Deduct stock
            foreach (var item in sale.Items)
            {
                var prod = _currentDb.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod != null)
                {
                    prod.Stock = Math.Max(0, prod.Stock - item.Quantity);
                }
            }

            _currentDb.Sales.Insert(0, sale);
            Persist();
        }
    }

    public string GetNextReceiptNumber()
    {
        lock (_lock)
        {
            int number = _currentDb.Sales.Count + 1001;
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            return $"REC-{datePart}-{number:D4}";
        }
    }
    #endregion
}
