using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetailPOS.Models;
using RetailPOS.Services;

namespace RetailPOS.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly Action<User> _onLoginSuccess;

    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    public LoginViewModel(DatabaseService db, Action<User> onLoginSuccess)
    {
        _db = db;
        _onLoginSuccess = onLoginSuccess;
    }

    public void Reset()
    {
        UserId = string.Empty;
        Password = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
    }

    [RelayCommand]
    private void SubmitLogin()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            ErrorMessage = "Please enter your User ID.";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your password.";
            HasError = true;
            return;
        }

        var auth = _db.AuthenticateUser(UserId.Trim(), Password.Trim());
        if (auth != null)
        {
            HasError = false;
            ErrorMessage = string.Empty;
            var user = auth;
            Reset();
            _onLoginSuccess(user);
        }
        else
        {
            ErrorMessage = "Invalid User ID or Password. Please try again.";
            HasError = true;
            Password = string.Empty;
        }
    }
}
