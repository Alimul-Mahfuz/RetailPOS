using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly ReceiptService _receiptService;
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool isLoggedOut = true;

    [ObservableProperty]
    private string activeTab = "Register"; // Register, Inventory, Loyalty, Sales, MasterData, Users

    public bool IsRegisterActive => ActiveTab == "Register";
    public bool IsInventoryActive => ActiveTab == "Inventory";
    public bool IsLoyaltyActive => ActiveTab == "Loyalty";
    public bool IsSalesActive => ActiveTab == "Sales";
    public bool IsMasterDataActive => ActiveTab == "MasterData";
    public bool IsUsersActive => ActiveTab == "Users";

    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsRegisterActive));
        OnPropertyChanged(nameof(IsInventoryActive));
        OnPropertyChanged(nameof(IsLoyaltyActive));
        OnPropertyChanged(nameof(IsSalesActive));
        OnPropertyChanged(nameof(IsMasterDataActive));
        OnPropertyChanged(nameof(IsUsersActive));
    }

    [ObservableProperty]
    private string currentDateTimeString = string.Empty;

    [ObservableProperty]
    private string statusMessage = "System Ready";

    public bool IsAdmin => CurrentUser?.IsAdmin ?? false;
    public string CashierDisplayName => CurrentUser != null ? $"{CurrentUser.FullName} ({CurrentUser.Role})" : "Not Logged In";

    public LoginViewModel LoginVM { get; }
    public RegisterViewModel RegisterVM { get; }
    public InventoryViewModel InventoryVM { get; }
    public LoyaltyViewModel LoyaltyVM { get; }
    public SalesHistoryViewModel SalesVM { get; }
    public MasterDataViewModel MasterDataVM { get; }
    public UserManagementViewModel UsersVM { get; }

    public MainViewModel()
    {
        _db = new DatabaseService();
        _receiptService = new ReceiptService(_db);

        LoginVM = new LoginViewModel(_db, OnUserLoggedIn);
        RegisterVM = new RegisterViewModel(_db, _receiptService, () => CurrentUser);
        InventoryVM = new InventoryViewModel(_db);
        LoyaltyVM = new LoyaltyViewModel(_db);
        SalesVM = new SalesHistoryViewModel(_db, _receiptService);
        MasterDataVM = new MasterDataViewModel(_db);
        UsersVM = new UserManagementViewModel(_db, () => CurrentUser);

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }

    private void UpdateClock()
    {
        CurrentDateTimeString = DateTime.Now.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt");
    }

    private void OnUserLoggedIn(User user)
    {
        CurrentUser = user;
        IsLoggedOut = false;
        ActiveTab = "Register";
        StatusMessage = $"Logged in as {user.FullName} ({user.Role})";

        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(CashierDisplayName));

        RegisterVM.RefreshCategories();
        RegisterVM.RefreshCatalog();
        RegisterVM.UpdateTotals();
        InventoryVM.RefreshCategories();
        InventoryVM.RefreshProducts();
        LoyaltyVM.RefreshCustomers();
        SalesVM.RefreshSales();
        MasterDataVM.LoadSettings();
        MasterDataVM.RefreshCategories();
        UsersVM.RefreshUsers();
    }

    [RelayCommand]
    private void Navigate(string tab)
    {
        if ((tab == "Users" || tab == "MasterData") && !IsAdmin)
        {
            StatusMessage = $"Access Denied: Only Administrators can access '{tab}'.";
            return;
        }

        ActiveTab = tab;

        if (tab == "Register")
        {
            RegisterVM.RefreshCategories();
            RegisterVM.RefreshCatalog();
            RegisterVM.UpdateTotals();
        }
        else if (tab == "Inventory")
        {
            InventoryVM.RefreshCategories();
            InventoryVM.RefreshProducts();
        }
        else if (tab == "Loyalty")
        {
            LoyaltyVM.RefreshCustomers();
        }
        else if (tab == "Sales")
        {
            SalesVM.RefreshSales();
        }
        else if (tab == "MasterData")
        {
            MasterDataVM.LoadSettings();
            MasterDataVM.RefreshCategories();
        }
        else if (tab == "Users")
        {
            UsersVM.RefreshUsers();
        }

        StatusMessage = $"Switched to {tab}";
    }

    [RelayCommand]
    private void Logout()
    {
        CurrentUser = null;
        IsLoggedOut = true;
        LoginVM.Reset();
        StatusMessage = "Logged out. Please authenticate to continue.";

        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(CashierDisplayName));
    }
}
