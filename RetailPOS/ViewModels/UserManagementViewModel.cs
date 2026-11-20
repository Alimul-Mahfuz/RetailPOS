using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class UserManagementViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly Func<User?> _getCurrentUser;

    public ObservableCollection<User> Users { get; } = [];
    public ObservableCollection<string> Roles { get; } = [];

    [ObservableProperty]
    private User? selectedUser;

    // Add / Edit User Modal
    [ObservableProperty]
    private bool isEditModalOpen;

    [ObservableProperty]
    private string modalTitle = "Add New Staff Member";

    [ObservableProperty]
    private string editId = string.Empty;

    [ObservableProperty]
    private string editUsername = string.Empty;

    [ObservableProperty]
    private string editFullName = string.Empty;

    [ObservableProperty]
    private string editPin = string.Empty;

    [ObservableProperty]
    private string editRole = "Cashier";

    [ObservableProperty]
    private bool editIsActive = true;

    [ObservableProperty]
    private string formError = string.Empty;

    [ObservableProperty]
    private bool hasFormError;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    // Delete Warning Confirmation Modal
    [ObservableProperty]
    private bool isDeleteModalOpen;

    [ObservableProperty]
    private string deleteTargetName = string.Empty;

    private User? _userToDelete;

    public UserManagementViewModel(DatabaseService db, Func<User?> getCurrentUser)
    {
        _db = db;
        _getCurrentUser = getCurrentUser;

        Roles.Add("Cashier");
        Roles.Add("Administrator");

        RefreshUsers();
    }

    public void RefreshUsers()
    {
        Users.Clear();
        var all = _db.GetAllUsers();
        foreach (var u in all)
        {
            Users.Add(u);
        }

        if (SelectedUser == null || !Users.Contains(SelectedUser))
        {
            SelectedUser = Users.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void OpenAddModal()
    {
        ModalTitle = "Add New Staff Member";
        EditId = string.Empty;
        EditUsername = string.Empty;
        EditFullName = string.Empty;
        EditPin = string.Empty;
        EditRole = "Cashier";
        EditIsActive = true;
        FormError = string.Empty;
        HasFormError = false;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    private void OpenEditModal(User? user)
    {
        var target = user ?? SelectedUser;
        if (target == null) return;

        ModalTitle = $"Edit Staff Account - {target.FullName}";
        EditId = target.Id;
        EditUsername = target.Username;
        EditFullName = target.FullName;
        EditPin = target.Pin;
        EditRole = target.Role;
        EditIsActive = target.IsActive;
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
    private void SaveUserForm()
    {
        if (string.IsNullOrWhiteSpace(EditUsername))
        {
            FormError = "Username is required.";
            HasFormError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            FormError = "Full Name is required.";
            HasFormError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(EditPin) || EditPin.Trim().Length < 4)
        {
            FormError = "Security PIN must be at least 4 digits.";
            HasFormError = true;
            return;
        }

        var isNew = string.IsNullOrEmpty(EditId);

        // Check if username already exists for another user
        var existingWithUsername = _db.GetAllUsers().FirstOrDefault(u =>
            string.Equals(u.Username, EditUsername.Trim(), StringComparison.OrdinalIgnoreCase) &&
            u.Id != EditId);

        if (existingWithUsername != null)
        {
            FormError = $"The username '{EditUsername.Trim()}' is already taken.";
            HasFormError = true;
            return;
        }

        var user = new User
        {
            Id = isNew ? Guid.NewGuid().ToString("N") : EditId,
            Username = EditUsername.Trim().ToLowerInvariant(),
            FullName = EditFullName.Trim(),
            Pin = EditPin.Trim(),
            Role = EditRole,
            IsActive = EditIsActive,
            CreatedAt = isNew ? DateTime.Now : (SelectedUser?.CreatedAt ?? DateTime.Now)
        };

        _db.SaveUser(user);
        IsEditModalOpen = false;
        StatusMessage = $"User '{user.FullName}' saved successfully.";
        RefreshUsers();
    }

    #region Delete Warning Modal
    [RelayCommand]
    private void PromptDeleteUser(User? user)
    {
        var target = user ?? SelectedUser;
        if (target == null) return;

        var currentUser = _getCurrentUser();
        if (currentUser != null && target.Id == currentUser.Id)
        {
            StatusMessage = "Cannot delete the currently logged in administrator account.";
            return;
        }

        var activeAdmins = _db.GetUsers().Count(u => u.IsAdmin);
        if (target.IsAdmin && activeAdmins <= 1)
        {
            StatusMessage = "Cannot delete the only active Administrator account.";
            return;
        }

        _userToDelete = target;
        DeleteTargetName = target.FullName;
        IsDeleteModalOpen = true;
    }

    [RelayCommand]
    private void ConfirmDeleteUser()
    {
        if (_userToDelete != null)
        {
            _db.DeleteUser(_userToDelete.Id);
            StatusMessage = $"Staff user '{_userToDelete.FullName}' deactivated.";
            _userToDelete = null;
            RefreshUsers();
        }
        IsDeleteModalOpen = false;
    }

    [RelayCommand]
    private void CancelDeleteUser()
    {
        _userToDelete = null;
        IsDeleteModalOpen = false;
    }
    #endregion
}
