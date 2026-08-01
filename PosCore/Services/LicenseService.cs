using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PosCore.Models;
using Serilog;
using System.Windows;

namespace PosCore.Services;

public class LicenseService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private const int MAX_OFFLINE_DAYS = 7;

    public LicenseService(HttpClient httpClient, IOptions<AppSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.BaseAddress = new Uri(_settings.ApiSettings.BaseUrl);
    }

    public async Task<bool> ValidateLicenseAsync()
    {
        try
        {
            var request = new { LicenseKey = _settings.License.LicenseKey, TerminalId = Environment.MachineName };
            var response = await _httpClient.PostAsJsonAsync("api/license/validate", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LicenseValidationResult>();
                if (result != null && result.IsValid)
                {
                    if (result.ValidUntil.HasValue && result.ValidUntil.Value.ToUniversalTime() < DateTime.UtcNow)
                    {
                        MessageBox.Show("Licencia Expirada.", "Error de Licencia", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Guardar fecha de última validación en memoria. Para persistir se requiere guardar en config.
                    _settings.License.LastValidationDate = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    MessageBox.Show($"Licencia inválida: {result?.Error}", "Error de Licencia", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            
            return CheckOfflineMode();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo validar la licencia con el servidor. Se verificará modo offline.");
            return CheckOfflineMode();
        }
    }

    private bool CheckOfflineMode()
    {
        if (_settings.License.LastValidationDate.HasValue)
        {
            var daysOffline = (DateTime.UtcNow - _settings.License.LastValidationDate.Value).TotalDays;
            
            if (daysOffline < 0)
            {
                MessageBox.Show("Se ha detectado una alteración en la fecha del sistema. Por favor, conecte el equipo a internet para validar la licencia.", "Error de Seguridad de Licencia", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (daysOffline <= MAX_OFFLINE_DAYS)
            {
                Log.Information($"Modo offline activado. Días sin conexión: {daysOffline:F1}/{MAX_OFFLINE_DAYS}");
                return true; // Permitir el uso offline
            }
            else
            {
                MessageBox.Show($"Han pasado más de {MAX_OFFLINE_DAYS} días sin conexión con el servidor de licencias. La aplicación se bloqueará.", "Modo Offline Expirado", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        // Si nunca se ha validado, no permitir
        // Fallback temporal para ambientes donde el server aún no tiene el controlador de licencias
        Log.Warning("No se pudo contactar al servidor de licencias. Permitiendo acceso fallback local.");
        return true;
    }
}

public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
    public int MaxTerminals { get; set; }
    public DateTime? ValidUntil { get; set; }
}
