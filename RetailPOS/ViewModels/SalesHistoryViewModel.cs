using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class SalesHistoryViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly ReceiptService _receiptService;

    public ObservableCollection<SaleRecord> Sales { get; } = [];

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private SaleRecord? selectedSale;

    [ObservableProperty]
    private string selectedSaleReceiptText = string.Empty;

    // Analytics Dashboard
    [ObservableProperty]
    private decimal todayRevenue;

    [ObservableProperty]
    private int todayOrdersCount;

    [ObservableProperty]
    private int todayItemsSold;

    [ObservableProperty]
    private decimal totalCashSales;

    [ObservableProperty]
    private decimal totalCardSales;

    [ObservableProperty]
    private string actionNotification = string.Empty;

    public SalesHistoryViewModel(DatabaseService db, ReceiptService receiptService)
    {
        _db = db;
        _receiptService = receiptService;
        RefreshSales();
    }

    public void RefreshSales()
    {
        Sales.Clear();
        var allSales = _db.GetSales();

        var today = DateTime.Today;
        var todaySales = allSales.Where(s => s.Timestamp.Date == today).ToList();

        TodayRevenue = todaySales.Sum(s => s.Total);
        TodayOrdersCount = todaySales.Count;
        TodayItemsSold = todaySales.Sum(s => s.TotalItemsCount);
        TotalCashSales = todaySales.Where(s => s.PaymentMethod == "Cash").Sum(s => s.Total);
        TotalCardSales = todaySales.Where(s => s.PaymentMethod != "Cash").Sum(s => s.Total);

        var query = SearchQuery?.Trim() ?? string.Empty;
        var filtered = allSales.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(s =>
                s.ReceiptNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.CashierName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.PaymentMethod.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var s in filtered)
        {
            Sales.Add(s);
        }

        if (SelectedSale == null || !Sales.Contains(SelectedSale))
        {
            SelectedSale = Sales.FirstOrDefault();
        }
        else
        {
            UpdateReceiptView();
        }
    }

    partial void OnSearchQueryChanged(string value) => RefreshSales();

    partial void OnSelectedSaleChanged(SaleRecord? value) => UpdateReceiptView();

    private void UpdateReceiptView()
    {
        if (SelectedSale != null)
        {
            SelectedSaleReceiptText = _receiptService.GenerateReceiptText(SelectedSale);
        }
        else
        {
            SelectedSaleReceiptText = "Select a sale transaction on the left to preview receipt.";
        }
    }

    [RelayCommand]
    private void ReprintReceipt()
    {
        if (SelectedSale == null) return;
        var path = _receiptService.SaveReceiptToFile(SelectedSale);

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
                ActionNotification = $"Receipt opened for re-printing: {SelectedSale.ReceiptNumber}";
            }
            else
            {
                ActionNotification = $"Receipt saved to {path}";
            }
        }
        catch (Exception ex)
        {
            ActionNotification = $"Receipt error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveReceiptFile()
    {
        if (SelectedSale == null) return;
        var path = _receiptService.SaveReceiptToFile(SelectedSale);
        ActionNotification = $"Receipt file saved: {path}";
    }
}
