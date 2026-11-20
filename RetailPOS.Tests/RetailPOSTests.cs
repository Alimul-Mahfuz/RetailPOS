using System;
using System.IO;
using System.Linq;
using RetailPOS.Models;
using RetailPOS.Services;
using RetailPOS.ViewModels;
using Xunit;

namespace RetailPOS.Tests;

public class RetailPOSTests
{
    [Fact]
    public void DatabaseService_Initializes_Default_Admin_And_Cashiers()
    {
        var db = new DatabaseService();
        var users = db.GetUsers();

        Assert.NotEmpty(users);

        var admin = users.FirstOrDefault(u => u.IsAdmin);
        Assert.NotNull(admin);
        Assert.Equal("admin", admin.Username);
        Assert.Equal("1234", admin.Pin);

        var cashier = users.FirstOrDefault(u => !u.IsAdmin);
        Assert.NotNull(cashier);
        Assert.Equal("Cashier", cashier.Role);
    }

    [Fact]
    public void DatabaseService_Initializes_Default_Grocery_Catalog()
    {
        var db = new DatabaseService();
        var products = db.GetProducts();

        Assert.True(products.Count >= 18);

        var milk = products.FirstOrDefault(p => p.Barcode == "1001");
        Assert.NotNull(milk);
        Assert.Equal("Whole Fresh Milk 1L", milk.Name);
        Assert.True(milk.Price > 0);
        Assert.True(milk.Stock > 0);
    }

    [Fact]
    public void Authentication_Validates_Correct_And_Incorrect_PIN()
    {
        var db = new DatabaseService();

        var adminUser = db.AuthenticateUser("admin", "1234");
        Assert.NotNull(adminUser);
        Assert.True(adminUser.IsAdmin);

        var failUser = db.AuthenticateUser("admin", "9999");
        Assert.Null(failUser);
    }

    [Fact]
    public void SimpleLogin_Authenticates_User_ID_And_Password()
    {
        var db = new DatabaseService();
        User? loggedIn = null;
        var loginVm = new LoginViewModel(db, u => loggedIn = u);

        // Valid login
        loginVm.UserId = "admin";
        loginVm.Password = "1234";
        loginVm.SubmitLoginCommand.Execute(null);

        Assert.NotNull(loggedIn);
        Assert.Equal("admin", loggedIn.Username);
        Assert.True(loggedIn.IsAdmin);

        // Invalid login
        loggedIn = null;
        loginVm.UserId = "admin";
        loginVm.Password = "wrongpass";
        loginVm.SubmitLoginCommand.Execute(null);

        Assert.Null(loggedIn);
        Assert.True(loginVm.HasError);
    }

    [Fact]
    public void AddNewProductModal_Has_Empty_Fields_And_Does_Not_Clear_Category_Filter()
    {
        var db = new DatabaseService();
        var invVm = new InventoryViewModel(db);

        // Verify initial state
        Assert.NotEmpty(invVm.Products);
        Assert.Equal("All", invVm.SelectedCategory);
        int initialCount = invVm.Products.Count;

        // Open Add Modal
        invVm.OpenAddModalCommand.Execute(null);

        // 1. Verify modal is open and fields are fresh/empty
        Assert.True(invVm.IsEditModalOpen);
        Assert.Empty(invVm.EditBarcode);
        Assert.Empty(invVm.EditName);
        Assert.Empty(invVm.EditPrice);
        Assert.Empty(invVm.EditCost);
        Assert.Empty(invVm.EditStock);
        Assert.NotEmpty(invVm.EditCategory); // Defaults to a valid category like "Produce"

        // 2. Verify category dropdown filter did NOT become empty or clear products
        Assert.Equal("All", invVm.SelectedCategory);
        Assert.Equal(initialCount, invVm.Products.Count);
        Assert.NotEmpty(invVm.FilterCategories);
        Assert.NotEmpty(invVm.ProductCategories);
        Assert.DoesNotContain("All", invVm.ProductCategories); // Product category choices must not contain "All"
    }

    [Fact]
    public void Product_CRUD_Works_Correctly()
    {
        var db = new DatabaseService();

        var barcode = $"SKU_{Random.Shared.Next(10000, 99999)}";
        var newProduct = new Product
        {
            Id = Guid.NewGuid().ToString("N"),
            Barcode = barcode,
            Name = "Organic Honey 500g",
            Category = "Pantry",
            Price = 7.99m,
            Cost = 4.50m,
            Stock = 20,
            MinStock = 5,
            Unit = "jar",
            IsActive = true
        };
        db.SaveProduct(newProduct);

        var retrieved = db.GetProductByBarcode(barcode);
        Assert.NotNull(retrieved);
        Assert.Equal("Organic Honey 500g", retrieved.Name);
        Assert.Equal(7.99m, retrieved.Price);

        // Update
        retrieved.Price = 8.49m;
        retrieved.Stock = 25;
        db.SaveProduct(retrieved);

        var updated = db.GetProductByBarcode(barcode);
        Assert.NotNull(updated);
        Assert.Equal(8.49m, updated.Price);
        Assert.Equal(25, updated.Stock);

        // Stock Adjustment
        db.AdjustStock(updated.Id, 10);
        var restocked = db.GetProductByBarcode(barcode);
        Assert.NotNull(restocked);
        Assert.Equal(35, restocked.Stock);

        // Delete
        db.DeleteProduct(updated.Id);
        var deleted = db.GetProductByBarcode(barcode);
        Assert.Null(deleted);
    }

    [Fact]
    public void DeleteWarningModals_Protect_Deletion_Until_Confirmed()
    {
        var db = new DatabaseService();

        var invVm = new InventoryViewModel(db);
        var prod = invVm.Products.First();
        var prodId = prod.Id;

        // Prompt delete opens modal
        invVm.PromptDeleteProductCommand.Execute(prod);
        Assert.True(invVm.IsDeleteModalOpen);
        Assert.Equal(prod.Name, invVm.DeleteTargetName);

        // Cancel keeps product
        invVm.CancelDeleteProductCommand.Execute(null);
        Assert.False(invVm.IsDeleteModalOpen);
        Assert.Contains(invVm.Products, p => p.Id == prodId);

        // Confirm removes product
        invVm.PromptDeleteProductCommand.Execute(prod);
        invVm.ConfirmDeleteProductCommand.Execute(null);
        Assert.False(invVm.IsDeleteModalOpen);
        Assert.DoesNotContain(invVm.Products, p => p.Id == prodId);

        // Restore product
        prod.IsActive = true;
        db.SaveProduct(prod);
    }

    [Fact]
    public void MasterData_VAT_Currency_And_Category_Setup()
    {
        var db = new DatabaseService();
        var masterVm = new MasterDataViewModel(db);

        // 1. Update VAT Tax and Currency
        masterVm.TaxName = "GST";
        masterVm.TaxRatePercent = "7.0";
        masterVm.CurrencySymbol = "€";
        masterVm.CurrencyCode = "EUR";
        masterVm.SaveAllSettingsCommand.Execute(null);

        var savedSettings = db.GetSettings();
        Assert.Equal("GST", savedSettings.TaxName);
        Assert.Equal(7.0m, savedSettings.TaxRatePercent);
        Assert.Equal("€", savedSettings.CurrencySymbol);
        Assert.Equal("EUR", savedSettings.CurrencyCode);

        // 2. Add New Category
        var catName = $"Frozen_{Random.Shared.Next(1000, 9999)}";
        masterVm.NewCategoryName = catName;
        masterVm.NewCategoryDescription = "Ice creams and treats";
        masterVm.AddCategoryCommand.Execute(null);

        var categories = db.GetCategories();
        Assert.Contains(categories, c => c.Name == catName);

        // 3. Delete Category with Confirmation Modal
        var addedCat = categories.First(c => c.Name == catName);
        masterVm.PromptDeleteCategoryCommand.Execute(addedCat);
        Assert.True(masterVm.IsDeleteModalOpen);
        masterVm.ConfirmDeleteCategoryCommand.Execute(null);
        Assert.False(masterVm.IsDeleteModalOpen);

        var categoriesAfter = db.GetCategories();
        Assert.DoesNotContain(categoriesAfter, c => c.Name == catName);

        // Reset Settings to default
        masterVm.TaxName = "VAT";
        masterVm.TaxRatePercent = "5.0";
        masterVm.CurrencySymbol = "$";
        masterVm.CurrencyCode = "USD";
        masterVm.SaveAllSettingsCommand.Execute(null);
    }

    [Fact]
    public void CustomerLoyalty_Calculates_Points_And_Redeem_Discount()
    {
        var db = new DatabaseService();
        var receiptService = new ReceiptService(db);
        var loyaltyVm = new LoyaltyViewModel(db);

        // 1. Enroll Customer with unique phone
        var uniquePhone = $"555-{Random.Shared.Next(1000, 9999)}";
        loyaltyVm.OpenAddModalCommand.Execute(null);
        loyaltyVm.EditPhone = uniquePhone;
        loyaltyVm.EditFullName = "Alice Johnson";
        loyaltyVm.EditEmail = "alice@example.com";
        loyaltyVm.EditTier = "Bronze";
        loyaltyVm.SaveCustomerFormCommand.Execute(null);

        var customer = db.GetCustomerByPhone(uniquePhone);
        Assert.NotNull(customer);
        Assert.Equal("Alice Johnson", customer.FullName);
        Assert.Equal(25, customer.Points); // 25 welcome points

        // 2. Manual award points
        loyaltyVm.OpenPointsModalCommand.Execute(customer);
        loyaltyVm.AdjustPointsAmount = "75";
        loyaltyVm.ApplyPointsAdjustmentCommand.Execute(null);

        customer = db.GetCustomerByPhone(uniquePhone)!;
        Assert.Equal(100, customer.Points); // 25 + 75 = 100 points

        // 3. Checkout with Loyalty Attachment & Redemption
        var currentUser = db.GetUsers().First(u => u.IsAdmin);
        var registerVm = new RegisterViewModel(db, receiptService, () => currentUser);

        // Add 3 grocery items so subtotal is comfortably > $2.00
        var product = db.GetProducts().First(p => p.Stock >= 10);
        registerVm.AddToCartCommand.Execute(product);
        registerVm.IncrementQuantityCommand.Execute(registerVm.Cart[0]);
        registerVm.IncrementQuantityCommand.Execute(registerVm.Cart[0]);

        // Attach customer
        registerVm.CustomerSearchQuery = uniquePhone;
        registerVm.SearchAndAttachCustomerCommand.Execute(null);
        Assert.True(registerVm.HasAttachedCustomer);
        Assert.Equal("Alice Johnson", registerVm.AttachedCustomer!.FullName);

        // Redeem 40 points (40 * 0.05 = $2.00 discount)
        registerVm.RedeemCustomerPointsCommand.Execute("40");
        Assert.Equal(40, registerVm.PointsToRedeem);
        Assert.Equal(2.00m, registerVm.PointsDiscount);

        // Complete checkout
        registerVm.TenderedAmount = registerVm.Total;
        registerVm.ProcessCheckoutCommand.Execute(null);

        Assert.True(registerVm.IsReceiptModalOpen);
        Assert.Contains("LOYALTY REWARDS", registerVm.ReceiptText);

        // Clean up
        db.DeleteCustomer(customer.Id);
    }

    [Fact]
    public void UserManagement_Add_And_Guards_Last_Admin()
    {
        var db = new DatabaseService();

        var username = $"clerk_{Random.Shared.Next(1000, 9999)}";
        var newStaff = new User
        {
            Id = Guid.NewGuid().ToString("N"),
            Username = username,
            FullName = "Test Clerk",
            Pin = "5555",
            Role = "Cashier",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        db.SaveUser(newStaff);

        var auth = db.AuthenticateUser(username, "5555");
        Assert.NotNull(auth);
        Assert.False(auth.IsAdmin);

        // Test ViewModel Guard: Current user cannot delete self
        var vm = new UserManagementViewModel(db, () => auth);
        vm.PromptDeleteUserCommand.Execute(auth);
        Assert.Contains("Cannot delete the currently logged in", vm.StatusMessage);

        // Clean up
        db.DeleteUser(newStaff.Id);
    }
}
