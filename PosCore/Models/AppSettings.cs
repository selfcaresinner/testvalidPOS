namespace PosCore.Models;

public class AppSettings
{
    public ApiSettings ApiSettings { get; set; } = new();
    public DatabaseSettings DatabaseSettings { get; set; } = new();
    public WhiteLabelSettings WhiteLabel { get; set; } = new();
    public ModuleSettings Modules { get; set; } = new();
    public TenantSettings Tenant { get; set; } = new();
    public PrinterSettings Printer { get; set; } = new();
    public LicenseSettings License { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public TaxSettings Tax { get; set; } = new();
    public PaymentMethodSettings PaymentMethods { get; set; } = new();
}

public class LicenseSettings
{
    public string LicenseKey { get; set; } = "VAL-TRIAL-123";
    public DateTime? LastValidationDate { get; set; }
}

public class PrinterSettings
{
    public string PortName { get; set; } = "POS-80";
    public bool PrintLogo { get; set; } = false;
}

public class TenantSettings
{
    public string CurrentTenantId { get; set; } = "TENANT_001";
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class WhiteLabelSettings
{
    public string CompanyName { get; set; } = "Default POS";
    public string PrimaryColor { get; set; } = "#FF007ACC";
    public string LogoPath { get; set; } = string.Empty;
}

public class ModuleSettings
{
    public bool EnableTableManagement { get; set; }
    public bool EnableInventoryControl { get; set; }
    public bool EnableCoupons { get; set; }
    public bool EnableLoyalty { get; set; }
}

public class SecuritySettings
{
    public string ManagerPin { get; set; } = "1234";
}

public class TaxSettings
{
    public decimal DefaultTaxRate { get; set; } = 0.16m;
    public string TaxId { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
    public string ReceiptFooter { get; set; } = "¡Gracias por su compra!";
}

public class PaymentMethodSettings
{
    public bool EnableCash { get; set; } = true;
    public bool EnableCard { get; set; } = true;
    public bool EnableTransfer { get; set; } = false;
}
