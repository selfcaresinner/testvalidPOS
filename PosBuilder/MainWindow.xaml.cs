using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PosBuilder
{
    public partial class MainWindow : Window
    {
        private const string DPAPI_PREFIX = "DPAPI:";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSelectLogo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtLogoPath.Text = dialog.FileName;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImgPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar la imagen: " + ex.Message);
                }
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtStoreName.Text))
            {
                MessageBox.Show("El nombre del comercio es requerido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(TxtApiBaseUrl.Text) || !Uri.TryCreate(TxtApiBaseUrl.Text, UriKind.Absolute, out _))
            {
                MessageBox.Show("La API Base URL es inválida. Debe ser una URL completa (ej. https://api.midominio.com/).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnGenerate.IsEnabled = false;
            TxtLog.Text = "Iniciando proceso de empaquetado del POS Cliente...\n";

            try
            {
                await Task.Run(() => ProcessGeneration());
                MessageBox.Show("¡Generación del instalador completada con éxito!\nRevisa la consola para ver la ruta.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppendLog("ERROR: " + ex.Message);
                MessageBox.Show("Ocurrió un error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnGenerate.IsEnabled = true;
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.Text += message + "\n";
                SvLog.ScrollToEnd();
            });
        }

        private string ExtractHexColor(string input)
        {
            var match = Regex.Match(input, @"#[0-9A-Fa-f]{6}");
            if (match.Success) return match.Value;
            return "#1976D2"; // default
        }

        private void ProcessGeneration()
        {
            string rootDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            string posCoreDir = Path.Combine(rootDir, "PosCore");
            
            if (!Directory.Exists(posCoreDir))
            {
                posCoreDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "PosCore"));
            }

            AppendLog("Directorio del POS Cliente (PosCore): " + posCoreDir);

            // 1. Copy Logo
            string logoSource = string.Empty;
            Dispatcher.Invoke(() => logoSource = TxtLogoPath.Text);
            
            string logoDestPath = "";
            if (!string.IsNullOrEmpty(logoSource) && File.Exists(logoSource))
            {
                string assetsDir = Path.Combine(posCoreDir, "Assets");
                if (!Directory.Exists(assetsDir))
                    Directory.CreateDirectory(assetsDir);
                
                logoDestPath = Path.Combine(assetsDir, "logo.png");
                File.Copy(logoSource, logoDestPath, true);
                AppendLog("Logo personalizado copiado exitosamente.");
            }

            // 2. Generate appsettings.json
            AppendLog("Generando configuración de appsettings.json...");
            string tenantId = "";
            string storeName = "";
            string primaryColor = "";
            string apiBaseUrl = "";
            string secretKey = "";
            
            string port = "";
            string dbUrl = "";
            string jwtIssuer = "";
            string jwtAudience = "";
            string adminUser = "";
            string adminPin = "";
            string empUser = "";
            string empPin = "";
            
            bool modCoupons = false, modLoyalty = false, modInventory = false;
            bool payCash = false, payCard = false, payTransfer = false;

            Dispatcher.Invoke(() =>
            {
                if (CmbTenants.SelectedItem is ComboBoxItem tenantItem)
                {
                    tenantId = tenantItem.Tag?.ToString() ?? "TENANT_001";
                }

                storeName = TxtStoreName.Text;
                
                // Color extraction
                string colorRaw = CmbPrimaryColor.Text;
                primaryColor = ExtractHexColor(colorRaw);

                apiBaseUrl = TxtApiBaseUrl.Text;
                if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";
                
                secretKey = PwdSecretKey.Password;
                port = TxtPort.Text;
                dbUrl = TxtDatabaseUrl.Text;
                jwtIssuer = TxtJwtIssuer.Text;
                jwtAudience = TxtJwtAudience.Text;
                adminUser = TxtAdminUsername.Text;
                adminPin = TxtAdminPin.Text;
                empUser = TxtEmployeeUsername.Text;
                empPin = TxtEmployeePin.Text;

                modCoupons = ChkCoupons.IsChecked == true;
                modLoyalty = ChkLoyalty.IsChecked == true;
                modInventory = ChkInventory.IsChecked == true;

                payCash = ChkCash.IsChecked == true;
                payCard = ChkCard.IsChecked == true;
                payTransfer = ChkTransfer.IsChecked == true;
            });

            string finalSecretKey = string.IsNullOrEmpty(secretKey) ? "" : DPAPI_PREFIX + EncryptString(secretKey);

            var appSettings = new
            {
                ApiSettings = new
                {
                    BaseUrl = apiBaseUrl,
                    SecretKey = finalSecretKey
                },
                DatabaseSettings = new
                {
                    ConnectionString = "Data Source=pos_local.db"
                },
                WhiteLabel = new
                {
                    CompanyName = storeName,
                    PrimaryColor = primaryColor,
                    LogoPath = string.IsNullOrEmpty(logoDestPath) ? "" : "Assets/logo.png"
                },
                Modules = new
                {
                    EnableTableManagement = false,
                    EnableInventoryControl = modInventory,
                    EnableCoupons = modCoupons,
                    EnableLoyalty = modLoyalty
                },
                PaymentMethods = new
                {
                    EnableCash = payCash,
                    EnableCard = payCard,
                    EnableTransfer = payTransfer
                },
                Tenant = new
                {
                    CurrentTenantId = tenantId
                }
            };

            string settingsJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
            string appSettingsPath = Path.Combine(posCoreDir, "appsettings.json");
            File.WriteAllText(appSettingsPath, settingsJson);
            AppendLog("Archivo appsettings.json configurado correctamente para: " + storeName);

            // Generate backend .env file for Railway
            string envContent = $"PORT={port}\n" +
                                $"ConnectionStrings__DefaultConnection={dbUrl}\n" +
                                $"DATABASE_URL={dbUrl}\n" +
                                $"Jwt__Key={secretKey}\n" +
                                $"Jwt__Issuer={jwtIssuer}\n" +
                                $"Jwt__Audience={jwtAudience}\n";
            string envFilePath = Path.Combine(rootDir, "railway.env");
            File.WriteAllText(envFilePath, envContent);
            AppendLog($"Archivo de entorno para Railway generado en: {envFilePath}");

            // Generate tenant SQL seed
            string tenantSql = $@"-- Initial users for {storeName} ({tenantId})
INSERT INTO ""Users"" (""Username"", ""Pin"", ""Role"", ""TenantId"") VALUES 
('{adminUser}', '{adminPin}', 'Admin', '{tenantId}'),
('{empUser}', '{empPin}', 'Cajero', '{tenantId}')
ON CONFLICT DO NOTHING;
";
            string sqlFilePath = Path.Combine(rootDir, $"{tenantId}_seed.sql");
            File.WriteAllText(sqlFilePath, tenantSql);
            AppendLog($"Archivo de inicialización SQL generado en: {sqlFilePath}");


            // 3. Execute build_and_package.ps1
            string scriptPath = Path.Combine(posCoreDir, "build_and_package.ps1");
            if (!File.Exists(scriptPath))
            {
                AppendLog("ADVERTENCIA: No se encontró build_and_package.ps1. Verifica los archivos del proyecto.");
                return;
            }

            AppendLog("Ejecutando script de empaquetado (Squirrel)...");
            
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null) throw new Exception("No se pudo iniciar PowerShell.");

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) AppendLog(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) AppendLog("ERROR: " + e.Data);
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"El script de PowerShell falló con código {process.ExitCode}");
                }
            }

            AppendLog("==========================================");
            AppendLog("¡Éxito! El instalador (Setup.exe) se encuentra en la carpeta Releases del proyecto.");
        }

        private static string EncryptString(string plainText)
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                return plainText;

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
    }
}
