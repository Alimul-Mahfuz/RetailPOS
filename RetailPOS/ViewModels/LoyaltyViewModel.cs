using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class LoyaltyViewModel : ViewModelBase
{
    private readonly DatabaseService _db;

    public ObservableCollection<LoyalCustomer> Customers { get; } = [];
    public ObservableCollection<PointTransaction> Transactions { get; } = [];
    public ObservableCollection<string> Tiers { get; } = [];

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private LoyalCustomer? selectedCustomer;

    // Metrics
    [ObservableProperty]
    private int totalLoyalMembers;

    [ObservableProperty]
    private int totalPointsInCirculation;

    [ObservableProperty]
    private decimal totalMemberSpending;

    // Status / Alerts
    [ObservableProperty]
    private string statusMessage = string.Empty;

    // Add / Edit Modal
    [ObservableProperty]
    private bool isEditModalOpen;

    [ObservableProperty]
    private string modalTitle = "Enroll New Loyal Customer";

    [ObservableProperty]
    private string editId = string.Empty;

    [ObservableProperty]
    private string editPhone = string.Empty;

    [ObservableProperty]
    private string editFullName = string.Empty;

    [ObservableProperty]
    private string editEmail = string.Empty;

    [ObservableProperty]
    private string editTier = "Bronze";

    [ObservableProperty]
    private string formError = string.Empty;

    [ObservableProperty]
    private bool hasFormError;

    // Manual Adjust Points Modal
    [ObservableProperty]
    private bool isPointsModalOpen;

    [ObservableProperty]
    private string adjustPointsAmount = "50";

    [ObservableProperty]
    private string adjustPointsReason = "Store Promotion Bonus";

    // Delete Warning Modal
    [ObservableProperty]
    private bool isDeleteModalOpen;

    [ObservableProperty]
    private string deleteTargetName = string.Empty;

    private LoyalCustomer? _customerToDelete;

    public LoyaltyViewModel(DatabaseService db)
    {
        _db = db;

        Tiers.Add("Bronze");
        Tiers.Add("Silver");
        Tiers.Add("Gold");
        Tiers.Add("Platinum");

        RefreshCustomers();
    }

    public void RefreshCustomers()
    {
        Customers.Clear();
        var all = _db.GetCustomers();

        TotalLoyalMembers = all.Count;
        TotalPointsInCirculation = all.Sum(c => c.Points);
        TotalMemberSpending = all.Sum(c => c.TotalSpent);

        var query = SearchQuery?.Trim() ?? string.Empty;
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(c =>
                c.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.PhoneNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var c in filtered)
        {
            Customers.Add(c);
        }

        if (SelectedCustomer == null || !Customers.Contains(SelectedCustomer))
        {
            SelectedCustomer = Customers.FirstOrDefault();
        }

        RefreshTransactions();
    }

    partial void OnSearchQueryChanged(string value) => RefreshCustomers();

    partial void OnSelectedCustomerChanged(LoyalCustomer? value) => RefreshTransactions();

    private void RefreshTransactions()
    {
        Transactions.Clear();
        var txs = _db.GetPointTransactions(SelectedCustomer?.Id);
        foreach (var t in txs)
        {
            Transactions.Add(t);
        }
    }

    [RelayCommand]
    private void OpenAddModal()
    {
        ModalTitle = "Enroll New Loyal Customer";
        EditId = string.Empty;
        EditPhone = string.Empty;
        EditFullName = string.Empty;
        EditEmail = string.Empty;
        EditTier = "Bronze";
        FormError = string.Empty;
        HasFormError = false;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    private void OpenEditModal(LoyalCustomer? customer)
    {
        var target = customer ?? SelectedCustomer;
        if (target == null) return;

        ModalTitle = $"Edit Customer - {target.FullName}";
        EditId = target.Id;
        EditPhone = target.PhoneNumber;
        EditFullName = target.FullName;
        EditEmail = target.Email;
        EditTier = target.Tier;
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
    private void SaveCustomerForm()
    {
        if (string.IsNullOrWhiteSpace(EditPhone))
        {
            FormError = "Customer Phone Number is required.";
            HasFormError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            FormError = "Full Name is required.";
            HasFormError = true;
            return;
        }

        var isNew = string.IsNullOrEmpty(EditId);

        // Check if phone number already exists
        var existing = _db.GetCustomers().FirstOrDefault(c =>
            c.PhoneNumber.Replace("-", "").Equals(EditPhone.Replace("-", "").Trim(), StringComparison.OrdinalIgnoreCase) &&
            c.Id != EditId);

        if (existing != null)
        {
            FormError = $"Phone number '{EditPhone}' is already registered to {existing.FullName}.";
            HasFormError = true;
            return;
        }

        var customer = new LoyalCustomer
        {
            Id = isNew ? Guid.NewGuid().ToString("N") : EditId,
            PhoneNumber = EditPhone.Trim(),
            FullName = EditFullName.Trim(),
            Email = EditEmail.Trim(),
            Tier = EditTier,
            Points = isNew ? 25 : (SelectedCustomer?.Points ?? 0), // Welcome bonus 25 pts for new members!
            TotalSpent = isNew ? 0m : (SelectedCustomer?.TotalSpent ?? 0m),
            JoinedAt = isNew ? DateTime.Now : (SelectedCustomer?.JoinedAt ?? DateTime.Now),
            IsActive = true
        };

        _db.SaveCustomer(customer);
        IsEditModalOpen = false;
        StatusMessage = $"Customer '{customer.FullName}' successfully saved.";
        RefreshCustomers();
    }

    [RelayCommand]
    private void PromptDeleteCustomer(LoyalCustomer? customer)
    {
        var target = customer ?? SelectedCustomer;
        if (target == null) return;

        _customerToDelete = target;
        DeleteTargetName = target.FullName;
        IsDeleteModalOpen = true;
    }

    [RelayCommand]
    private void ConfirmDeleteCustomer()
    {
        if (_customerToDelete != null)
        {
            _db.DeleteCustomer(_customerToDelete.Id);
            StatusMessage = $"Customer '{_customerToDelete.FullName}' removed from loyalty program.";
            _customerToDelete = null;
            RefreshCustomers();
        }
        IsDeleteModalOpen = false;
    }

    [RelayCommand]
    private void CancelDeleteCustomer()
    {
        _customerToDelete = null;
        IsDeleteModalOpen = false;
    }

    [RelayCommand]
    private void OpenPointsModal(LoyalCustomer? customer)
    {
        var target = customer ?? SelectedCustomer;
        if (target == null) return;

        SelectedCustomer = target;
        AdjustPointsAmount = "50";
        AdjustPointsReason = "Store Promotion / Birthday Bonus";
        IsPointsModalOpen = true;
    }

    [RelayCommand]
    private void ClosePointsModal()
    {
        IsPointsModalOpen = false;
    }

    [RelayCommand]
    private void ApplyPointsAdjustment()
    {
        if (SelectedCustomer == null) return;
        if (int.TryParse(AdjustPointsAmount, out var delta))
        {
            if (delta > 0)
            {
                _db.AwardPoints(SelectedCustomer.Id, delta, "ADJUST", 0m);
                StatusMessage = $"Awarded {delta} points to {SelectedCustomer.FullName}.";
            }
            else if (delta < 0)
            {
                _db.RedeemPoints(SelectedCustomer.Id, Math.Abs(delta), "ADJUST", 0m);
                StatusMessage = $"Deducted {Math.Abs(delta)} points from {SelectedCustomer.FullName}.";
            }
            IsPointsModalOpen = false;
            RefreshCustomers();
        }
    }
}
