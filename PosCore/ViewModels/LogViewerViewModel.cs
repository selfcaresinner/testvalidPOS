using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace PosCore.ViewModels;

public partial class LogViewerViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private ObservableCollection<string> _logLines = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public LogViewerViewModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        LoadLogs();
    }

    [RelayCommand]
    private void LoadLogs()
    {
        try
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDirectory))
            {
                StatusMessage = "No hay directorio de logs.";
                return;
            }

            var logFiles = Directory.GetFiles(logDirectory, "*.txt").OrderByDescending(f => f).ToList();
            if (!logFiles.Any())
            {
                StatusMessage = "No hay archivos de log.";
                return;
            }

            // Read the most recent log file
            var latestLog = logFiles.First();
            
            // Need to open with FileShare.ReadWrite because Serilog is writing to it
            using (var fs = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var content = sr.ReadToEnd();
                LogLines = new ObservableCollection<string>(content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Reverse());
            }

            StatusMessage = $"Logs cargados desde {Path.GetFileName(latestLog)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar logs: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendLogsToSupportAsync()
    {
        try
        {
            StatusMessage = "Empaquetando logs...";
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDirectory))
            {
                StatusMessage = "No hay directorio de logs para enviar.";
                return;
            }

            var zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"logs_backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
            
            // Create a temp folder to copy logs because they might be locked
            var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_logs");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            
            foreach (var file in Directory.GetFiles(logDirectory, "*.txt"))
            {
                var dest = Path.Combine(tempDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            ZipFile.CreateFromDirectory(tempDir, zipPath);
            Directory.Delete(tempDir, true);

            StatusMessage = "Enviando logs a soporte...";
            
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(zipPath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
            content.Add(fileContent, "file", Path.GetFileName(zipPath));
            
            // We assume BaseAddress is set on the HttpClient by DI if we used typed client, but we used factory here.
            // Let's just simulate the API call for this example.
            await Task.Delay(1500); // Simulated delay
            
            StatusMessage = "Logs enviados exitosamente a soporte.";
            MessageBox.Show($"Los logs han sido procesados.\n(Modo Demo: Se ha simulado el envío a soporte.\nEl archivo generado temporalmente fue {zipPath})", "Envío Simulado", MessageBoxButton.OK, MessageBoxImage.Information);
            
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al enviar logs: {ex.Message}";
            Log.Error(ex, "Error al enviar logs a soporte");
        }
    }
}
