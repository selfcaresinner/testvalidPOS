using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PosCore.Models;

namespace PosCore.Services;

public static class SecureConfigManager
{
    private const string DPAPI_PREFIX = "DPAPI:";

    public static AppSettings LoadAndSecureConfig(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("appsettings.json not found", filePath);

        var json = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppSettings();

        bool needsSave = false;

        
        // Secure SecretKey
        if (!string.IsNullOrEmpty(settings.ApiSettings.SecretKey) && !settings.ApiSettings.SecretKey.StartsWith(DPAPI_PREFIX))
        {
            settings.ApiSettings.SecretKey = DPAPI_PREFIX + EncryptString(settings.ApiSettings.SecretKey);
            needsSave = true;
        }

        // Secure BaseUrl
        if (!string.IsNullOrEmpty(settings.ApiSettings.BaseUrl) && !settings.ApiSettings.BaseUrl.StartsWith(DPAPI_PREFIX))
        {
            settings.ApiSettings.BaseUrl = DPAPI_PREFIX + EncryptString(settings.ApiSettings.BaseUrl);
            needsSave = true;
        }

        // Secure ConnectionString
        if (!string.IsNullOrEmpty(settings.DatabaseSettings.ConnectionString) && !settings.DatabaseSettings.ConnectionString.StartsWith(DPAPI_PREFIX))
        {
            settings.DatabaseSettings.ConnectionString = DPAPI_PREFIX + EncryptString(settings.DatabaseSettings.ConnectionString);
            needsSave = true;
        }

        if (needsSave)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Return a decrypted copy for memory
        var decryptedSettings = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
        
        
        if (!string.IsNullOrEmpty(decryptedSettings.ApiSettings.SecretKey) && decryptedSettings.ApiSettings.SecretKey.StartsWith(DPAPI_PREFIX))
        {
            decryptedSettings.ApiSettings.SecretKey = DecryptString(decryptedSettings.ApiSettings.SecretKey.Substring(DPAPI_PREFIX.Length));
        }

        if (decryptedSettings.ApiSettings.BaseUrl.StartsWith(DPAPI_PREFIX))
        {
            decryptedSettings.ApiSettings.BaseUrl = DecryptString(decryptedSettings.ApiSettings.BaseUrl.Substring(DPAPI_PREFIX.Length));
        }

        if (decryptedSettings.DatabaseSettings.ConnectionString.StartsWith(DPAPI_PREFIX))
        {
            decryptedSettings.DatabaseSettings.ConnectionString = DecryptString(decryptedSettings.DatabaseSettings.ConnectionString.Substring(DPAPI_PREFIX.Length));
        }

        return decryptedSettings;
    }

    private static string EncryptString(string plainText)
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            return plainText; // Fallback for non-Windows (e.g., CI/CD)

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string DecryptString(string encryptedBase64)
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            return encryptedBase64;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to decrypt: " + ex.Message);
            return string.Empty;
        }
    }
}
