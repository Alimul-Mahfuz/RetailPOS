# RetailPOS - Windows 7 Era Retail Point of Sale (C#)

A desktop Point of Sale (POS), customer loyalty, master data, and inventory management system designed specifically for groceries and small retail shops, crafted in **pure C# (.NET 10)** with **Avalonia UI** and authentic **Windows 7 Aero Glass** skeuomorphic aesthetics.

---

## 🌟 Modules & Features

### 1. Clean, Clutter-Free Windows 7 Logon
- **Simpler User ID & Password Login**: Clean, focused Windows 7 Aero card with User ID, masked PIN/Password, and Enter key log on.
- **Role-Based Redirection**: Administrators get full access; Cashiers/Staff are limited to Register, Customer Loyalty, and Receipts.

### 2. POS Register & Fast Grocery Checkout
- **Instant Barcode / SKU Scan**: Type or scan any barcode and hit Enter to add to the shopping cart.
- **Customer Loyalty at Checkout**: Look up customer by phone number or name, attach member to sale, and redeem points for instant cash discount.
- **Interactive Shopping Cart**: Live quantity adjustment (`+` / `-`), line totals, discount presets (5%, 10%), dynamic VAT/Tax calculation, and digital green POS total readout.
- **Clear Cart Warning Modal**: Windows 7 Aero confirmation dialog (`⚠️`) prevents accidental cart resets.
- **Payment Processing**: Cash, Credit Card, and Mobile Pay, with quick cash buttons ($5, $10, $20, $50, $100, Exact) and automatic change due calculation.
- **Auto-deducts Stock & Awards Points**: Real-time inventory deduction and member reward point accrual upon checkout.

### 3. Thermal Receipt Generation & Printing
- **80mm Thermal Receipt Slip**: Includes custom store branding, cashier name, customer loyalty rewards (points earned & redeemed), itemized products, tax breakdown, and change due.
- **Print & Export**: One-click **"Print Receipt"** (opens in Windows Notepad print dialog) and automatic archiving into `Receipts/` as `.txt` files.

### 4. Customer Loyalty & Rewards Management
- **Member Directory**: Track customer phone, name, email, membership tier (Bronze, Silver, Gold, Platinum), points balance, and lifetime spending.
- **Point Calculation & Earning Logic**: Configurable earn rate (default 1 point per $1 spent).
- **Redeem Logic**: Configurable redeem value (default 20 points = $1.00 discount).
- **Auto Tier Upgrades**: Automatic promotion from Bronze to Silver, Gold, and Platinum based on lifetime spending.
- **Activity Ledger**: Full history of points earned on sales and redeemed discounts.
- **Delete Warning Modal**: Protected member removal with confirmation.

### 5. Master Data & Store Setup (Admin Only)
- **VAT / Tax Configuration**: Tax Name (VAT / Sales Tax / GST), custom Tax Rate % (e.g. 5%, 7.5%, 10%), and Tax Registration ID.
- **Currency Setup**: Custom Currency Symbol (`$`, `€`, `£`, `৳`, `₹`, `¥`), currency code (`USD`, `EUR`, `BDT`, etc.), and position.
- **Category Master Data (CRUD)**: Add, edit, and delete grocery product categories with delete confirmation modals.
- **Store Profile**: Store Name, Address, Phone, and custom Receipt Footer message.

### 6. Product CRUD & Inventory Management
- **Full CRUD**: Add new grocery products, edit price/cost/stock/min-stock, and delete items.
- **Delete Warning Modal**: Windows 7 Aero confirmation dialog (`⚠️`) protects against accidental product deletions.
- **Inventory Metrics**: KPI tiles for Total Products, Total Stock Units, Low Stock Alerts, and Out of Stock.
- **Restock Assistant**: Quick stock presets (+10, +25, +50, +100) or custom adjustment.

### 7. Staff Account Management (Admin Only)
- Add staff accounts, update full names, reset 4-digit PINs, and deactivate accounts with Delete Warning Modals and safeguards protecting the last active administrator.

---

## 🔑 Default Accounts & Credentials

| Role | User ID / Username | Password / PIN | Access Level |
|---|---|---|---|
| **Administrator** | `admin` | `1234` | Full access (Register, Inventory, Loyalty, Master Data, Staff) |
| **Cashier / Staff** | `sarah` | `1111` | Register, Customer Loyalty, Receipts |
| **Cashier / Staff** | `david` | `2222` | Register, Customer Loyalty, Receipts |

---

## 🚀 How to Run

### Run the Application
```bash
dotnet run --project RetailPOS/RetailPOS.csproj
```

### Run the Test Suite (10 passing tests)
```bash
dotnet test RetailPOS.slnx
```
