using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace PosCore.Services;

public class SessionData
{
    public string TenantId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SessionManager
{
    public string CurrentTenantId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    private readonly string _sessionFilePath;

    public SessionManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "PosCore");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        _sessionFilePath = Path.Combine(appFolder, "session.dat");
    }

    public void SaveSession()
    {
        try
        {
            

            var data = new SessionData
            {
                TenantId = CurrentTenantId,
                Token = Token,
                RefreshToken = RefreshToken,
                Username = Username,
                Role = Role
            };

            var json = JsonSerializer.Serialize(data);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            
            // Protect data using DPAPI
            
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                var encryptedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_sessionFilePath, encryptedBytes);
            } else {
                File.WriteAllBytes(_sessionFilePath, jsonBytes);
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving session data via DPAPI");
        }
    }

    public bool LoadSession()
    {
        try
        {
            

            if (!File.Exists(_sessionFilePath))
                return false;

            
            var encryptedBytes = File.ReadAllBytes(_sessionFilePath);
            byte[] jsonBytes;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                jsonBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            } else {
                jsonBytes = encryptedBytes;
            }

            var json = Encoding.UTF8.GetString(jsonBytes);
            
            var data = JsonSerializer.Deserialize<SessionData>(json);
            if (data != null)
            {
                CurrentTenantId = data.TenantId;
                Token = data.Token;
                RefreshToken = data.RefreshToken;
                Username = data.Username;
                Role = data.Role ?? "Admin";
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading session data via DPAPI");
            // If corruption or key changed, delete the corrupted file
            if (File.Exists(_sessionFilePath))
                File.Delete(_sessionFilePath);
        }
        return false;
    }
    
    public void ClearSession()
    {
        CurrentTenantId = string.Empty;
        Token = string.Empty;
        RefreshToken = string.Empty;
        Username = string.Empty;
        
        if (File.Exists(_sessionFilePath))
            File.Delete(_sessionFilePath);
    }
}
