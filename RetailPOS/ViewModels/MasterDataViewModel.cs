using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class MasterDataViewModel : ViewModelBase
{
    private readonly DatabaseService _db;

    // Store profile
    [ObservableProperty]
    private string storeName = string.Empty;

    [ObservableProperty]
    private string storeAddress = string.Empty;

    [ObservableProperty]
    private string storePhone = string.Empty;

    [ObservableProperty]
    private string taxId = string.Empty;

    // VAT / Tax
    [ObservableProperty]
    private string taxName = "VAT";

    [ObservableProperty]
    private string taxRatePercent = "5.0";

    // Currency
    [ObservableProperty]
    private string currencySymbol = "$";

    [ObservableProperty]
    private string currencyCode = "USD";

    [ObservableProperty]
    private string currencyPosition = "Before";

    // Loyalty Settings
    [ObservableProperty]
    private string pointsPerDollarSpent = "1.0";

    [ObservableProperty]
    private string pointRedeemValue = "0.05";

    [ObservableProperty]
    private string minPointsToRedeem = "20";

    // Receipt Footer
    [ObservableProperty]
    private string receiptFooter = string.Empty;

    // Status Message
    [ObservableProperty]
    private string statusMessage = string.Empty;

    // Category Management
    public ObservableCollection<CategoryItem> Categories { get; } = [];

    [ObservableProperty]
    private string newCategoryName = string.Empty;

    [ObservableProperty]
    private string newCategoryDescription = string.Empty;

    [ObservableProperty]
    private CategoryItem? selectedCategory;

    // Delete Warning Modal for Master Data
    [ObservableProperty]
    private bool isDeleteModalOpen;

    [ObservableProperty]
    private string deleteTargetName = string.Empty;

    private CategoryItem? _categoryToDelete;

    public MasterDataViewModel(DatabaseService db)
    {
        _db = db;
        LoadSettings();
        RefreshCategories();
    }

    public void LoadSettings()
    {
        var s = _db.GetSettings();
        StoreName = s.StoreName;
        StoreAddress = s.StoreAddress;
        StorePhone = s.StorePhone;
        TaxId = s.TaxId;
        TaxName = s.TaxName;
        TaxRatePercent = s.TaxRatePercent.ToString("F1");
        CurrencySymbol = s.CurrencySymbol;
        CurrencyCode = s.CurrencyCode;
        CurrencyPosition = s.CurrencyPosition;
        PointsPerDollarSpent = s.PointsPerDollarSpent.ToString("F1");
        PointRedeemValue = s.PointRedeemValue.ToString("F2");
        MinPointsToRedeem = s.MinPointsToRedeem.ToString();
        ReceiptFooter = s.ReceiptFooter;
        StatusMessage = "Configuration loaded.";
    }

    public void RefreshCategories()
    {
        Categories.Clear();
        foreach (var c in _db.GetCategories())
        {
            Categories.Add(c);
        }
    }

    [RelayCommand]
    private void SaveAllSettings()
    {
        if (!decimal.TryParse(TaxRatePercent, out var taxRate) || taxRate < 0)
        {
            StatusMessage = "Invalid Tax Rate! Please enter a valid non-negative number.";
            return;
        }

        if (!decimal.TryParse(PointsPerDollarSpent, out var ptsPerDollar) || ptsPerDollar < 0)
        {
            ptsPerDollar = 1.0m;
        }

        if (!decimal.TryParse(PointRedeemValue, out var ptValue) || ptValue <= 0)
        {
            ptValue = 0.05m;
        }

        if (!int.TryParse(MinPointsToRedeem, out var minPts) || minPts < 0)
        {
            minPts = 20;
        }

        var s = new StoreSettings
        {
            StoreName = string.IsNullOrWhiteSpace(StoreName) ? "GREEN VALLEY GROCERY" : StoreName.Trim(),
            StoreAddress = StoreAddress.Trim(),
            StorePhone = StorePhone.Trim(),
            TaxId = TaxId.Trim(),
            TaxName = string.IsNullOrWhiteSpace(TaxName) ? "VAT" : TaxName.Trim(),
            TaxRatePercent = taxRate,
            CurrencySymbol = string.IsNullOrWhiteSpace(CurrencySymbol) ? "$" : CurrencySymbol.Trim(),
            CurrencyCode = string.IsNullOrWhiteSpace(CurrencyCode) ? "USD" : CurrencyCode.Trim().ToUpperInvariant(),
            CurrencyPosition = CurrencyPosition,
            PointsPerDollarSpent = ptsPerDollar,
            PointRedeemValue = ptValue,
            MinPointsToRedeem = minPts,
            ReceiptFooter = ReceiptFooter.Trim()
        };

        _db.SaveSettings(s);
        StatusMessage = "Store configuration and tax settings successfully saved!";
    }

    [RelayCommand]
    private void AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            StatusMessage = "Category name cannot be empty.";
            return;
        }

        var item = new CategoryItem
        {
            Name = NewCategoryName.Trim(),
            Description = NewCategoryDescription?.Trim() ?? string.Empty
        };

        _db.SaveCategory(item);
        NewCategoryName = string.Empty;
        NewCategoryDescription = string.Empty;
        RefreshCategories();
        StatusMessage = $"Category '{item.Name}' added successfully.";
    }

    [RelayCommand]
    private void PromptDeleteCategory(CategoryItem? item)
    {
        var target = item ?? SelectedCategory;
        if (target == null) return;

        _categoryToDelete = target;
        DeleteTargetName = target.Name;
        IsDeleteModalOpen = true;
    }

    [RelayCommand]
    private void ConfirmDeleteCategory()
    {
        if (_categoryToDelete != null)
        {
            _db.DeleteCategory(_categoryToDelete.Id);
            StatusMessage = $"Category '{_categoryToDelete.Name}' removed.";
            _categoryToDelete = null;
            RefreshCategories();
        }
        IsDeleteModalOpen = false;
    }

    [RelayCommand]
    private void CancelDeleteCategory()
    {
        _categoryToDelete = null;
        IsDeleteModalOpen = false;
    }
}
