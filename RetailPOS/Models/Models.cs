using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RetailPOS.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier"; // Administrator or Cashier
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsAdmin => string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase);
}

public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; } = 5;
    public string Unit { get; set; } = "pcs";
    public bool IsActive { get; set; } = true;

    public bool IsOutOfStock => Stock <= 0;
    public bool IsLowStock => Stock > 0 && Stock <= MinStock;
    public string StockStatus => IsOutOfStock ? "Out of Stock" : (IsLowStock ? "Low Stock" : "In Stock");
    public string FormattedPrice => $"${Price:F2}";
}

public partial class CartItem : ObservableObject
{
    public Product Product { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(FormattedLineTotal))]
    private int quantity = 1;

    public decimal UnitPrice => Product.Price;
    public decimal LineTotal => Quantity * UnitPrice;
    public string FormattedUnitPrice => $"${UnitPrice:F2}";
    public string FormattedLineTotal => $"${LineTotal:F2}";

    public CartItem(Product product, int quantity = 1)
    {
        Product = product;
        Quantity = quantity;
    }
}

public class SaleItem
{
    public string ProductId { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public string FormattedPrice => $"${UnitPrice:F2}";
    public string FormattedTotal => $"${LineTotal:F2}";
}

public class SaleRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public List<SaleItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal PointsDiscount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public decimal ChangeDue { get; set; }

    // Customer Loyalty info
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int PointsEarned { get; set; }
    public int PointsRedeemed { get; set; }

    public int TotalItemsCount
    {
        get
        {
            int count = 0;
            foreach (var item in Items) count += item.Quantity;
            return count;
        }
    }

    public string FormattedTotal => $"${Total:F2}";
    public string FormattedDate => Timestamp.ToString("yyyy-MM-dd hh:mm tt");
}

public class StoreSettings
{
    public string StoreName { get; set; } = "GREEN VALLEY GROCERY & MART";
    public string StoreAddress { get; set; } = "104 Pinecrest Blvd, Suite 100";
    public string StorePhone { get; set; } = "Tel: (555) 019-2834";
    public string TaxName { get; set; } = "VAT";
    public decimal TaxRatePercent { get; set; } = 5.0m;
    public string TaxId { get; set; } = "TAX ID: 88-3419024";
    public string CurrencySymbol { get; set; } = "$";
    public string CurrencyCode { get; set; } = "USD";
    public string CurrencyPosition { get; set; } = "Before"; // Before or After

    // Loyalty point settings
    public decimal PointsPerDollarSpent { get; set; } = 1.0m; // 1 point per $1
    public decimal PointRedeemValue { get; set; } = 0.05m; // 1 point = $0.05 (20 points = $1)
    public int MinPointsToRedeem { get; set; } = 20;

    public string ReceiptFooter { get; set; } = "Thank you for shopping with us!\nPlease keep receipt for returns within 14 days.";
}

public class CategoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class LoyalCustomer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tier { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum
    public int Points { get; set; } = 0;
    public decimal TotalSpent { get; set; } = 0m;
    public DateTime JoinedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public string FormattedPoints => $"{Points} pts";
    public string FormattedSpent => $"${TotalSpent:F2}";
    public decimal RedeemableValue(decimal pointValue) => Points * pointValue;
}

public class PointTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Type { get; set; } = "Earned"; // Earned or Redeemed
    public int PointsDelta { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class DatabaseSchema
{
    public StoreSettings Settings { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<SaleRecord> Sales { get; set; } = [];
    public List<LoyalCustomer> Customers { get; set; } = [];
    public List<PointTransaction> PointTransactions { get; set; } = [];
}
