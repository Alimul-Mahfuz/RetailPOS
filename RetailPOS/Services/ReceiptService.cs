using System;
using System.IO;
using System.Text;
using RetailPOS.Models;

namespace RetailPOS.Services;

public class ReceiptService
{
    private readonly DatabaseService? _db;

    public ReceiptService(DatabaseService? db = null)
    {
        _db = db;
    }

    public string GenerateReceiptText(SaleRecord sale)
    {
        var settings = _db?.GetSettings() ?? new StoreSettings();
        var sb = new StringBuilder();
        const int width = 42;

        string Center(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length >= width) return text;
            int pad = (width - text.Length) / 2;
            return text.PadLeft(pad + text.Length).PadRight(width);
        }

        string Divider(char c = '-') => new(c, width);

        sb.AppendLine(Divider('='));
        sb.AppendLine(Center(settings.StoreName));
        sb.AppendLine(Center(settings.StoreAddress));
        sb.AppendLine(Center(settings.StorePhone));
        if (!string.IsNullOrEmpty(settings.TaxId))
        {
            sb.AppendLine(Center(settings.TaxId));
        }
        sb.AppendLine(Divider('='));

        sb.AppendLine($"Receipt #: {sale.ReceiptNumber}");
        sb.AppendLine($"Date     : {sale.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Cashier  : {sale.CashierName}");
        if (!string.IsNullOrEmpty(sale.CustomerName))
        {
            sb.AppendLine($"Customer : {sale.CustomerName} ({sale.CustomerPhone})");
        }
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"{"Qty",-4} {"Item",-22} {"Price",7} {"Total",7}");
        sb.AppendLine(Divider('-'));

        foreach (var item in sale.Items)
        {
            string name = item.ProductName;
            if (name.Length > 22)
            {
                name = name.Substring(0, 20) + "..";
            }
            sb.AppendLine($"{item.Quantity,-4} {name,-22} {item.UnitPrice,7:F2} {item.LineTotal,7:F2}");
        }

        sb.AppendLine(Divider('-'));
        sb.AppendLine($"{"Subtotal:",-28} {settings.CurrencySymbol}{sale.Subtotal,11:F2}");
        if (sale.Discount > 0)
        {
            sb.AppendLine($"{"Discount:",-28} -{settings.CurrencySymbol}{sale.Discount,10:F2}");
        }
        if (sale.PointsDiscount > 0)
        {
            sb.AppendLine($"{"Loyalty Points Discount:",-28} -{settings.CurrencySymbol}{sale.PointsDiscount,10:F2}");
        }
        var taxLabel = $"{settings.TaxName} ({settings.TaxRatePercent}%):";
        sb.AppendLine($"{taxLabel,-28} {settings.CurrencySymbol}{sale.Tax,11:F2}");
        sb.AppendLine(Divider('='));
        sb.AppendLine($"{"TOTAL DUE:",-25} {settings.CurrencySymbol}{sale.Total,14:F2}");
        sb.AppendLine(Divider('='));

        sb.AppendLine($"{"Payment Method:",-28} {sale.PaymentMethod,12}");
        sb.AppendLine($"{"Amount Paid:",-28} {settings.CurrencySymbol}{sale.AmountPaid,11:F2}");
        sb.AppendLine($"{"Change Due:",-28} {settings.CurrencySymbol}{sale.ChangeDue,11:F2}");

        if (!string.IsNullOrEmpty(sale.CustomerId))
        {
            sb.AppendLine(Divider('-'));
            sb.AppendLine(Center("*** LOYALTY REWARDS ***"));
            if (sale.PointsRedeemed > 0)
            {
                sb.AppendLine($"Points Redeemed : {sale.PointsRedeemed} pts");
            }
            if (sale.PointsEarned > 0)
            {
                sb.AppendLine($"Points Earned   : +{sale.PointsEarned} pts");
            }
        }

        sb.AppendLine(Divider('-'));
        sb.AppendLine($"Total Items Sold: {sale.TotalItemsCount}");
        sb.AppendLine();

        var footerLines = settings.ReceiptFooter.Split('\n');
        foreach (var line in footerLines)
        {
            sb.AppendLine(Center(line.Trim()));
        }

        sb.AppendLine(Divider('='));
        sb.AppendLine(Center($"*{sale.ReceiptNumber}*"));
        sb.AppendLine(Divider('='));

        return sb.ToString();
    }

    public string SaveReceiptToFile(SaleRecord sale)
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var receiptsDir = Path.Combine(appDir, "Receipts");
            if (!Directory.Exists(receiptsDir))
            {
                Directory.CreateDirectory(receiptsDir);
            }

            var text = GenerateReceiptText(sale);
            var safeReceiptNo = sale.ReceiptNumber.Replace(":", "_").Replace("/", "_");
            var filename = Path.Combine(receiptsDir, $"{safeReceiptNo}.txt");
            File.WriteAllText(filename, text);
            return filename;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save receipt: {ex.Message}");
            return string.Empty;
        }
    }
}
