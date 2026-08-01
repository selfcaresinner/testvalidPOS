using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PosCore.Data;
using PosCore.Models;

namespace PosCore.Services;

public class SyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncService> _logger;
    private readonly System.Timers.Timer _timer;
    private bool _isSyncing = false;
    private readonly System.Threading.SemaphoreSlim _syncSemaphore = new System.Threading.SemaphoreSlim(1, 1);
    private DateTime _lastSyncTime = DateTime.MinValue;
    
    public event Action? OnSyncCompleted;
    public event Action<bool>? OnNetworkStatusChanged;
    private bool _isOffline = false;
    public bool IsOffline
    {
        get => _isOffline;
        private set
        {
            if (_isOffline != value)
            {
                _isOffline = value;
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnNetworkStatusChanged?.Invoke(_isOffline));
            }
        }
    }

    public SyncService(IServiceProvider serviceProvider, ILogger<SyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Configurar timer para ejecutar cada 10 segundos
        _timer = new System.Timers.Timer(10000);
        _timer.Elapsed += async (sender, e) => { try { await SyncDataAsync(); } catch (Exception ex) { _logger.LogError(ex, "Sync failed"); } };
    }

    public void Start()
    {
        _timer.Start();
        Task.Run(async () => await SyncDataAsync());
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public async Task SyncDataAsync()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var apiService = scope.ServiceProvider.GetRequiredService<IApiService>();
            var sessionManager = scope.ServiceProvider.GetRequiredService<SessionManager>();

            // Solo sincronizamos si hay un usuario autenticado
            if (!sessionManager.IsAuthenticated) return;

            // 1. Sincronización Inversa (Descargar cambios de BD Central)
            await PullUpdatesFromServerAsync(dbContext, apiService);

            // 2. Procesar Outbox local para enviar al servidor
            var pendingMessages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(500)
                .ToListAsync();

            if (pendingMessages.Any())
            {
                _logger.LogInformation($"Iniciando sincronización: {pendingMessages.Count} mensajes pendientes.");

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        bool success = false;

                    if (message.EventType == "OrderCreated")
                    {
                        var order = JsonSerializer.Deserialize<Order>(message.Payload);
                        if (order != null)
                        {
                            success = await apiService.SyncOrderAsync(order);
                        }
                    }
                    else if (message.EventType == "ProductUpdated" || message.EventType == "ProductCreated")
                    {
                        var product = JsonSerializer.Deserialize<Product>(message.Payload);
                        if (product != null)
                        {
                            success = await apiService.SyncProductAsync(product);
                        }
                    }

                    if (success)
                    {
                        message.ProcessedAt = DateTime.UtcNow;
                        _logger.LogInformation($"Mensaje ID {message.Id} ({message.EventType}) sincronizado con éxito.");
                    }
                    else
                    {
                        message.RetryCount++;
                        if (message.RetryCount >= 5)
                        {
                            _logger.LogError($"Mensaje ID {message.Id} superó el límite máximo de reintentos. Marcando como procesado con error.");
                            message.ProcessedAt = DateTime.UtcNow; // O IsProcessed = true
                        }
                        else
                        {
                            _logger.LogWarning($"Fallo al sincronizar Mensaje ID {message.Id}. Intento {message.RetryCount}. Aplicando Backoff indefinido.");
                            await dbContext.SaveChangesAsync();
                            await Task.Delay((int)Math.Pow(2, message.RetryCount) * 1000);
                            break;
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        if (message.RetryCount >= 5)
                        {
                            _logger.LogError($"Mensaje ID {message.Id} superó el límite máximo de reintentos con excepción: {ex.Message}. Marcando como procesado con error.");
                            message.ProcessedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            _logger.LogWarning($"Excepcion al sincronizar Mensaje ID {message.Id}: {ex.Message}");
                            await dbContext.SaveChangesAsync();
                            await Task.Delay((int)Math.Pow(2, message.RetryCount) * 1000);
                            break;
                        }
                    }
                }

                await dbContext.SaveChangesAsync();
            }
            
            IsOffline = false;
            // Notificar a la UI si hubo cambios o simplemente al terminar un ciclo de sync exitoso
            // Para evitar re-renders excesivos, idealmente solo lo llamamos si hubo pendingMessages o cloudProducts, pero por simplicidad lo llamamos siempre que termine sin error
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                OnSyncCompleted?.Invoke();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico durante el proceso de sincronización.");
            IsOffline = true;
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    private async Task PullUpdatesFromServerAsync(PosDbContext dbContext, IApiService apiService)
    {
        try
        {
            List<Product> cloudProducts;

            if (_lastSyncTime == DateTime.MinValue)
            {
                // Primera vez, descargamos todo
                cloudProducts = await apiService.GetProductsAsync();
            }
            else
            {
                // Descargamos solo cambios
                cloudProducts = await apiService.GetChangesAsync(_lastSyncTime);
            }

            if (cloudProducts.Any())
            {
                foreach (var cloudProduct in cloudProducts)
                {
                    // Buscamos por código de barras, que es más estable que los IDs generados localmente
                    var localProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Barcode == cloudProduct.Barcode);
                    
                    if (localProduct == null)
                    {
                        // Insertamos temporalmente con Id = 0 para que EF Core auto-asigne el Id local
                        cloudProduct.Id = 0; 
                        dbContext.Products.Add(cloudProduct);
                    }
                    
                    else
                    {
                        // Resolución de Conflictos: 
                        // El precio y nombre ganan los de la nube.
                        // El stock se fusiona: Si en caja se vendió algo, la cantidad es menor. 
                        // Para simplificar, tomamos el mínimo o si la caja restó stock localmente.
                        
                        if (cloudProduct.LastUpdated > localProduct.LastUpdated)
                        {
                            _logger.LogInformation($"Actualizando producto {localProduct.Barcode} con versión del servidor (Conflicto Resuelto).");
                            localProduct.Name = cloudProduct.Name;
                            localProduct.Price = cloudProduct.Price;
                            
                            // Fusionar inventario de forma segura: si local es menor, restarle a la nube esa diferencia
                            // Esto asume que la diferencia local fue por ventas offline.
                            localProduct.StockQuantity = Math.Min(localProduct.StockQuantity, cloudProduct.StockQuantity);
                            
                            localProduct.LastUpdated = cloudProduct.LastUpdated;
                            dbContext.Products.Update(localProduct);
                        }
                    }

                }

                await dbContext.SaveChangesAsync();
            }

            // Actualizamos la hora del último sync exitoso
            _lastSyncTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al traer actualizaciones del servidor.");
            IsOffline = true;
            throw;
        }
    }
}
