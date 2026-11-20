using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class InventoryViewModel : ViewModelBase
{
    private readonly DatabaseService _db;

    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<string> FilterCategories { get; } = [];
    public ObservableCollection<string> ProductCategories { get; } = [];

    // Backward compatibility alias
    public ObservableCollection<string> Categories => FilterCategories;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private bool showLowStockOnly;

    [ObservableProperty]
    private Product? selectedProduct;

    // Stats
    [ObservableProperty]
    private int totalProductsCount;

    [ObservableProperty]
    private int totalStockUnits;

    [ObservableProperty]
    private int lowStockCount;

    [ObservableProperty]
    private int outOfStockCount;

    // Add / Edit Modal
    [ObservableProperty]
    private bool isEditModalOpen;

    [ObservableProperty]
    private string modalTitle = "Add New Product";

    [ObservableProperty]
    private string editId = string.Empty;

    [ObservableProperty]
    private string editBarcode = string.Empty;

    [ObservableProperty]
    private string editName = string.Empty;

    [ObservableProperty]
    private string editCategory = "Produce";

    [ObservableProperty]
    private string editPrice = string.Empty;

    [ObservableProperty]
    private string editCost = string.Empty;

    [ObservableProperty]
    private string editStock = string.Empty;

    [ObservableProperty]
    private string editMinStock = "5";

    [ObservableProperty]
    private string editUnit = string.Empty;

    [ObservableProperty]
    private string formError = string.Empty;

    [ObservableProperty]
    private bool hasFormError;

    // Stock Adjust Modal
    [ObservableProperty]
    private bool isAdjustModalOpen;

    [ObservableProperty]
    private string adjustDelta = "10";

    [ObservableProperty]
    private string adjustReason = "Supplier Delivery Restock";

    // Delete Warning Confirmation Modal
    [ObservableProperty]
    private bool isDeleteModalOpen;

    [ObservableProperty]
    private string deleteTargetName = string.Empty;

    private Product? _productToDelete;

    public InventoryViewModel(DatabaseService db)
    {
        _db = db;
        RefreshCategories();
        RefreshProducts();
    }

    public void RefreshCategories()
    {
        var prevFilter = SelectedCategory;

        FilterCategories.Clear();
        FilterCategories.Add("All");

        ProductCategories.Clear();

        foreach (var c in _db.GetCategories())
        {
            FilterCategories.Add(c.Name);
            ProductCategories.Add(c.Name);
        }

        if (!string.IsNullOrEmpty(prevFilter) && FilterCategories.Contains(prevFilter))
        {
            SelectedCategory = prevFilter;
        }
        else
        {
            SelectedCategory = "All";
        }
    }

    public void RefreshProducts()
    {
        Products.Clear();
        var all = _db.GetProducts();

        TotalProductsCount = all.Count;
        TotalStockUnits = all.Sum(p => p.Stock);
        LowStockCount = all.Count(p => p.IsLowStock);
        OutOfStockCount = all.Count(p => p.IsOutOfStock);

        var query = SearchQuery?.Trim() ?? string.Empty;
        var filtered = all.AsEnumerable();

        // If SelectedCategory is null or "All", show all products
        if (!string.IsNullOrWhiteSpace(SelectedCategory) && !string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(p => string.Equals(p.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (ShowLowStockOnly)
        {
            filtered = filtered.Where(p => p.IsLowStock || p.IsOutOfStock);
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
            Products.Add(p);
        }

        if (SelectedProduct != null && !Products.Contains(SelectedProduct))
        {
            SelectedProduct = Products.FirstOrDefault();
        }
    }

    partial void OnSearchQueryChanged(string value) => RefreshProducts();
    partial void OnSelectedCategoryChanged(string value) => RefreshProducts();
    partial void OnShowLowStockOnlyChanged(bool value) => RefreshProducts();

    [RelayCommand]
    private void OpenAddModal()
    {
        // Make sure categories are available without resetting filter
        if (ProductCategories.Count == 0)
        {
            RefreshCategories();
        }

        ModalTitle = "Add New Grocery Product";
        EditId = string.Empty;
        EditBarcode = string.Empty;
        EditName = string.Empty;
        EditCategory = ProductCategories.FirstOrDefault() ?? "Produce";
        EditPrice = string.Empty;
        EditCost = string.Empty;
        EditStock = string.Empty;
        EditMinStock = "5";
        EditUnit = string.Empty;
        FormError = string.Empty;
        HasFormError = false;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    private void OpenEditModal(Product? product)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        if (ProductCategories.Count == 0)
        {
            RefreshCategories();
        }

        ModalTitle = $"Edit Product - {target.Name}";
        EditId = target.Id;
        EditBarcode = target.Barcode;
        EditName = target.Name;
        EditCategory = target.Category;
        EditPrice = target.Price.ToString("F2");
        EditCost = target.Cost.ToString("F2");
        EditStock = target.Stock.ToString();
        EditMinStock = target.MinStock.ToString();
        EditUnit = target.Unit;
        FormError = string.Empty;
        HasFormError = false;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    private void CloseEditModal()
    {
        IsEditModalOpen = false;
        HasFormError = false;
    }

    [RelayCommand]
    private void SaveProductForm()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            FormError = "Product Name is required.";
            HasFormError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(EditBarcode))
        {
            FormError = "Barcode / SKU is required.";
            HasFormError = true;
            return;
        }

        if (!decimal.TryParse(EditPrice, out var price) || price < 0)
        {
            FormError = "Selling Price must be a valid positive number.";
            HasFormError = true;
            return;
        }

        if (!decimal.TryParse(EditCost, out var cost) || cost < 0)
        {
            cost = 0m;
        }

        if (!int.TryParse(EditStock, out var stock) || stock < 0)
        {
            FormError = "Stock must be a positive integer.";
            HasFormError = true;
            return;
        }

        if (!int.TryParse(EditMinStock, out var minStock) || minStock < 0)
        {
            minStock = 5;
        }

        var isNew = string.IsNullOrEmpty(EditId);
        var category = string.IsNullOrWhiteSpace(EditCategory)
            ? (ProductCategories.FirstOrDefault() ?? "Produce")
            : EditCategory.Trim();

        var product = new Product
        {
            Id = isNew ? Guid.NewGuid().ToString("N") : EditId,
            Barcode = EditBarcode.Trim(),
            Name = EditName.Trim(),
            Category = category,
            Price = price,
            Cost = cost,
            Stock = stock,
            MinStock = minStock,
            Unit = string.IsNullOrWhiteSpace(EditUnit) ? "pcs" : EditUnit.Trim(),
            IsActive = true
        };

        _db.SaveProduct(product);
        IsEditModalOpen = false;
        RefreshProducts();
    }

    #region Delete Warning Modal
    [RelayCommand]
    private void PromptDeleteProduct(Product? product)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        _productToDelete = target;
        DeleteTargetName = target.Name;
        IsDeleteModalOpen = true;
    }

    [RelayCommand]
    private void ConfirmDeleteProduct()
    {
        if (_productToDelete != null)
        {
            _db.DeleteProduct(_productToDelete.Id);
            _productToDelete = null;
            RefreshProducts();
        }
        IsDeleteModalOpen = false;
    }

    [RelayCommand]
    private void CancelDeleteProduct()
    {
        _productToDelete = null;
        IsDeleteModalOpen = false;
    }
    #endregion

    #region Stock Adjust
    [RelayCommand]
    private void OpenAdjustStockModal(Product? product)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        SelectedProduct = target;
        AdjustDelta = "10";
        AdjustReason = "Supplier Restock Delivery";
        IsAdjustModalOpen = true;
    }

    [RelayCommand]
    private void CloseAdjustModal()
    {
        IsAdjustModalOpen = false;
    }

    [RelayCommand]
    private void QuickAddStock(string deltaStr)
    {
        if (SelectedProduct == null) return;
        if (int.TryParse(deltaStr, out var delta))
        {
            _db.AdjustStock(SelectedProduct.Id, delta);
            RefreshProducts();
        }
    }

    [RelayCommand]
    private void ApplyAdjustStock()
    {
        if (SelectedProduct == null) return;
        if (int.TryParse(AdjustDelta, out var delta))
        {
            _db.AdjustStock(SelectedProduct.Id, delta);
            IsAdjustModalOpen = false;
            RefreshProducts();
        }
    }
    #endregion
}
