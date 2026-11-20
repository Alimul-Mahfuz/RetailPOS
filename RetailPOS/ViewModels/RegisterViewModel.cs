using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly ReceiptService _receiptService;
    private readonly Func<User?> _getCurrentUser;

    public ObservableCollection<CartItem> Cart { get; } = [];
    public ObservableCollection<Product> FilteredProducts { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string searchQuery = string.Empty;

    // Totals
    [ObservableProperty]
    private decimal subtotal;

    [ObservableProperty]
    private decimal tax;

    [ObservableProperty]
    private decimal discount;

    [ObservableProperty]
    private decimal pointsDiscount;

    [ObservableProperty]
    private decimal total;

    [ObservableProperty]
    private int totalItemCount;

    [ObservableProperty]
    private string paymentMethod = "Cash";

    [ObservableProperty]
    private decimal tenderedAmount;

    [ObservableProperty]
    private decimal changeDue;

    [ObservableProperty]
    private string checkoutError = string.Empty;

    [ObservableProperty]
    private bool hasCheckoutError;

    // Dynamic Store & Tax Labels
    [ObservableProperty]
    private string taxLabel = "Tax (5%):";

    [ObservableProperty]
    private string currencySymbol = "$";

    // Customer Loyalty at Register
    [ObservableProperty]
    private string customerSearchQuery = string.Empty;

    [ObservableProperty]
    private LoyalCustomer? attachedCustomer;

    [ObservableProperty]
    private bool hasAttachedCustomer;

    [ObservableProperty]
    private int pointsToRedeem;

    [ObservableProperty]
    private int estimatedPointsToEarn;

    [ObservableProperty]
    private string loyaltyNotice = string.Empty;

    // Delete / Clear Cart Warning Modal
    [ObservableProperty]
    private bool isClearCartModalOpen;

    // Receipt Modal / Overlay
    [ObservableProperty]
    private bool isReceiptModalOpen;

    [ObservableProperty]
    private string receiptText = string.Empty;

    [ObservableProperty]
    private string receiptStatusMessage = string.Empty;

    [ObservableProperty]
    private SaleRecord? lastSale;

    public RegisterViewModel(DatabaseService db, ReceiptService receiptService, Func<User?> getCurrentUser)
    {
        _db = db;
        _receiptService = receiptService;
        _getCurrentUser = getCurrentUser;

        RefreshCategories();
        RefreshCatalog();
        UpdateTotals();
    }

    public void RefreshCategories()
    {
        Categories.Clear();
        Categories.Add("All");
        foreach (var c in _db.GetCategories())
        {
            Categories.Add(c.Name);
        }
    }

    public void RefreshCatalog()
    {
        FilteredProducts.Clear();
        var allProducts = _db.GetProducts();

        var query = SearchQuery?.Trim() ?? string.Empty;
        var cat = SelectedCategory;

        var filtered = allProducts.AsEnumerable();

        if (!string.Equals(cat, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(p => string.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var p in filtered)
        {
            FilteredProducts.Add(p);
        }
    }

    partial void OnSearchQueryChanged(string value) => RefreshCatalog();
    partial void OnSelectedCategoryChanged(string value) => RefreshCatalog();
    partial void OnTenderedAmountChanged(decimal value) => CalculateChange();
    partial void OnTotalChanged(decimal value) => CalculateChange();

    private void CalculateChange()
    {
        if (PaymentMethod == "Cash")
        {
            ChangeDue = Math.Max(0m, TenderedAmount - Total);
        }
        else
        {
            ChangeDue = 0m;
        }
    }

    public void UpdateTotals()
    {
        var settings = _db.GetSettings();
        CurrencySymbol = settings.CurrencySymbol;

        decimal sum = 0m;
        int count = 0;
        foreach (var item in Cart)
        {
            sum += item.LineTotal;
            count += item.Quantity;
        }

        Subtotal = sum;
        decimal taxRate = settings.TaxRatePercent / 100m;
        Tax = Math.Round(Subtotal * taxRate, 2);
        TaxLabel = $"{settings.TaxName} ({settings.TaxRatePercent}%):";

        decimal afterDiscount = Math.Max(0m, Subtotal + Tax - Discount - PointsDiscount);
        Total = afterDiscount;
        TotalItemCount = count;

        // Calculate points to earn for this sale
        if (AttachedCustomer != null)
        {
            EstimatedPointsToEarn = (int)Math.Floor(Total * settings.PointsPerDollarSpent);
        }
        else
        {
            EstimatedPointsToEarn = 0;
        }

        if (PaymentMethod != "Cash")
        {
            TenderedAmount = Total;
        }
        CalculateChange();
    }

    #region Loyalty Operations at Checkout
    [RelayCommand]
    private void SearchAndAttachCustomer()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearchQuery))
        {
            LoyaltyNotice = "Enter customer phone or name to search.";
            return;
        }

        var customer = _db.GetCustomerByPhone(CustomerSearchQuery);
        if (customer != null)
        {
            AttachedCustomer = customer;
            HasAttachedCustomer = true;
            LoyaltyNotice = $"Attached: {customer.FullName} ({customer.Tier} • {customer.Points} pts)";
            PointsDiscount = 0m;
            PointsToRedeem = 0;
            UpdateTotals();
        }
        else
        {
            LoyaltyNotice = $"No customer found matching '{CustomerSearchQuery}'.";
        }
    }

    [RelayCommand]
    private void DetachCustomer()
    {
        AttachedCustomer = null;
        HasAttachedCustomer = false;
        PointsDiscount = 0m;
        PointsToRedeem = 0;
        LoyaltyNotice = string.Empty;
        UpdateTotals();
    }

    [RelayCommand]
    private void RedeemCustomerPoints(string pointsStr)
    {
        if (AttachedCustomer == null) return;
        var settings = _db.GetSettings();

        int pts;
        if (pointsStr == "Max")
        {
            pts = AttachedCustomer.Points;
        }
        else if (!int.TryParse(pointsStr, out pts))
        {
            return;
        }

        if (pts > AttachedCustomer.Points)
        {
            pts = AttachedCustomer.Points;
        }

        if (pts < settings.MinPointsToRedeem && pts > 0)
        {
            LoyaltyNotice = $"Minimum {settings.MinPointsToRedeem} points required to redeem.";
            return;
        }

        decimal discountValue = Math.Round(pts * settings.PointRedeemValue, 2);
        // Cannot discount more than Subtotal + Tax - Standard Discount
        decimal maxDiscount = Math.Max(0m, Subtotal + Tax - Discount);
        if (discountValue > maxDiscount)
        {
            discountValue = maxDiscount;
            pts = (int)Math.Ceiling(discountValue / settings.PointRedeemValue);
        }

        PointsToRedeem = pts;
        PointsDiscount = discountValue;
        LoyaltyNotice = $"Redeeming {pts} pts for {settings.CurrencySymbol}{discountValue:F2} discount!";
        UpdateTotals();
    }
    #endregion

    #region Cart Operations
    [RelayCommand]
    private void SearchOrAddBarcode()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        var code = SearchQuery.Trim();
        var exactProduct = _db.GetProductByBarcode(code);

        if (exactProduct != null)
        {
            AddToCart(exactProduct);
            SearchQuery = string.Empty;
        }
        else
        {
            RefreshCatalog();
        }
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        if (product.IsOutOfStock)
        {
            CheckoutError = $"Cannot add {product.Name}: Item is out of stock!";
            HasCheckoutError = true;
            return;
        }

        HasCheckoutError = false;
        CheckoutError = string.Empty;

        var existing = Cart.FirstOrDefault(c => c.Product.Id == product.Id);
        if (existing != null)
        {
            if (existing.Quantity >= product.Stock)
            {
                CheckoutError = $"Cannot add more {product.Name}: Only {product.Stock} in stock!";
                HasCheckoutError = true;
                return;
            }
            existing.Quantity++;
        }
        else
        {
            Cart.Add(new CartItem(product, 1));
        }

        UpdateTotals();
    }

    [RelayCommand]
    private void IncrementQuantity(CartItem item)
    {
        if (item.Quantity >= item.Product.Stock)
        {
            CheckoutError = $"Maximum stock ({item.Product.Stock}) reached for {item.Product.Name}.";
            HasCheckoutError = true;
            return;
        }

        HasCheckoutError = false;
        item.Quantity++;
        UpdateTotals();
    }

    [RelayCommand]
    private void DecrementQuantity(CartItem item)
    {
        HasCheckoutError = false;
        if (item.Quantity > 1)
        {
            item.Quantity--;
        }
        else
        {
            Cart.Remove(item);
        }
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveFromCart(CartItem item)
    {
        Cart.Remove(item);
        UpdateTotals();
    }

    // Delete Warning Modal for Cart Clearing
    [RelayCommand]
    private void PromptClearCart()
    {
        if (Cart.Count > 0)
        {
            IsClearCartModalOpen = true;
        }
    }

    [RelayCommand]
    private void ConfirmClearCart()
    {
        Cart.Clear();
        Discount = 0m;
        PointsDiscount = 0m;
        PointsToRedeem = 0;
        TenderedAmount = 0m;
        ChangeDue = 0m;
        HasCheckoutError = false;
        CheckoutError = string.Empty;
        IsClearCartModalOpen = false;
        UpdateTotals();
    }

    [RelayCommand]
    private void CancelClearCart()
    {
        IsClearCartModalOpen = false;
    }

    [RelayCommand]
    private void SetCategory(string category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    private void SetPaymentMethod(string method)
    {
        PaymentMethod = method;
        if (method != "Cash")
        {
            TenderedAmount = Total;
            ChangeDue = 0m;
        }
        else
        {
            if (TenderedAmount < Total)
            {
                TenderedAmount = Total;
            }
            CalculateChange();
        }
    }

    [RelayCommand]
    private void SetQuickTender(string amountStr)
    {
        if (amountStr == "Exact")
        {
            TenderedAmount = Total;
        }
        else if (decimal.TryParse(amountStr, out var val))
        {
            TenderedAmount = val;
        }
        CalculateChange();
    }

    [RelayCommand]
    private void ApplyDiscount(string percentStr)
    {
        if (decimal.TryParse(percentStr, out var pct))
        {
            Discount = Math.Round(Subtotal * (pct / 100m), 2);
            UpdateTotals();
        }
    }
    #endregion

    #region Checkout & Printing
    [RelayCommand]
    private void ProcessCheckout()
    {
        if (Cart.Count == 0)
        {
            CheckoutError = "Cart is empty. Add products before completing sale.";
            HasCheckoutError = true;
            return;
        }

        if (PaymentMethod == "Cash" && TenderedAmount < Total)
        {
            CheckoutError = $"Insufficient cash tendered! Total due is {CurrencySymbol}{Total:F2}, but received only {CurrencySymbol}{TenderedAmount:F2}.";
            HasCheckoutError = true;
            return;
        }

        var currentUser = _getCurrentUser();
        var cashierId = currentUser?.Id ?? "u_unknown";
        var cashierName = currentUser?.FullName ?? "Cashier";
        var receiptNo = _db.GetNextReceiptNumber();

        var sale = new SaleRecord
        {
            ReceiptNumber = receiptNo,
            Timestamp = DateTime.Now,
            CashierId = cashierId,
            CashierName = cashierName,
            Subtotal = Subtotal,
            Tax = Tax,
            Discount = Discount,
            PointsDiscount = PointsDiscount,
            Total = Total,
            PaymentMethod = PaymentMethod,
            AmountPaid = TenderedAmount,
            ChangeDue = ChangeDue,
            CustomerId = AttachedCustomer?.Id,
            CustomerName = AttachedCustomer?.FullName,
            CustomerPhone = AttachedCustomer?.PhoneNumber,
            PointsEarned = EstimatedPointsToEarn,
            PointsRedeemed = PointsToRedeem,
            Items = Cart.Select(c => new SaleItem
            {
                ProductId = c.Product.Id,
                Barcode = c.Product.Barcode,
                ProductName = c.Product.Name,
                UnitPrice = c.UnitPrice,
                Quantity = c.Quantity,
                LineTotal = c.LineTotal
            }).ToList()
        };

        // 1. Record sale and deduct stock
        _db.RecordSale(sale);

        // 2. Process customer loyalty points
        if (AttachedCustomer != null)
        {
            if (PointsToRedeem > 0)
            {
                _db.RedeemPoints(AttachedCustomer.Id, PointsToRedeem, receiptNo, PointsDiscount);
            }
            if (EstimatedPointsToEarn > 0)
            {
                _db.AwardPoints(AttachedCustomer.Id, EstimatedPointsToEarn, receiptNo, Total);
            }
        }

        // 3. Generate receipt
        LastSale = sale;
        ReceiptText = _receiptService.GenerateReceiptText(sale);
        ReceiptStatusMessage = $"Sale completed successfully! Receipt #{sale.ReceiptNumber} recorded.";
        IsReceiptModalOpen = true;

        // Auto-save receipt to disk
        _receiptService.SaveReceiptToFile(sale);

        // Reset cart and checkout state
        Cart.Clear();
        Discount = 0m;
        PointsDiscount = 0m;
        PointsToRedeem = 0;
        AttachedCustomer = null;
        HasAttachedCustomer = false;
        CustomerSearchQuery = string.Empty;
        LoyaltyNotice = string.Empty;
        TenderedAmount = 0m;
        ChangeDue = 0m;
        HasCheckoutError = false;
        CheckoutError = string.Empty;

        UpdateTotals();
        RefreshCatalog();
    }

    [RelayCommand]
    private void CloseReceiptModal()
    {
        IsReceiptModalOpen = false;
        ReceiptStatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SaveReceiptFile()
    {
        if (LastSale == null) return;
        var path = _receiptService.SaveReceiptToFile(LastSale);
        if (!string.IsNullOrEmpty(path))
        {
            ReceiptStatusMessage = $"Receipt saved to:\n{path}";
        }
    }

    [RelayCommand]
    private void PrintReceipt()
    {
        if (LastSale == null) return;
        var path = _receiptService.SaveReceiptToFile(LastSale);
        try
        {
            if (OperatingSystem.IsWindows() && File.Exists(path))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                ReceiptStatusMessage = $"Opened receipt in Notepad for printing ({path})";
            }
            else
            {
                ReceiptStatusMessage = $"Receipt saved to {path}";
            }
        }
        catch (Exception ex)
        {
            ReceiptStatusMessage = $"Receipt saved ({path}): {ex.Message}";
        }
    }
    #endregion
}
