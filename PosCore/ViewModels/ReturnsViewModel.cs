using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosCore.Data;
using PosCore.Models;
using PosCore.Services;

namespace PosCore.ViewModels;

public partial class ReturnsViewModel : ObservableObject
{
    private readonly PosDbContext _dbContext;
    private readonly TicketPrinterService _ticketPrinterService;

    [ObservableProperty]
    private ObservableCollection<Order> _recentOrders = new();

    public ReturnsViewModel(PosDbContext dbContext, TicketPrinterService ticketPrinterService)
    {
        _dbContext = dbContext;
        _ticketPrinterService = ticketPrinterService;
        LoadOrdersCommand.Execute(null);
    }

    [RelayCommand]
    private void ReprintOrder(Order order)
    {
        if (order == null) return;
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            bool success = _ticketPrinterService.PrintTicket(order);
            if (success)
                MessageBox.Show("Ticket enviado a la impresora.", "Reimpresión", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Error al imprimir el ticket. Revise la impresora.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .Take(50)
            .ToListAsync();

        RecentOrders.Clear();
        foreach (var o in orders)
        {
            RecentOrders.Add(o);
        }
    }


    [RelayCommand]
    private void ReprintReturn(Order order)
    {
        if (order == null || !order.IsReturned) return;
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                _ticketPrinterService.PrintCreditNote(order, "POS-80");
                MessageBox.Show("Nota de crédito enviada a la impresora.", "Reimpresión", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir el ticket de devolución: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task PartialReturnOrderAsync(Order order)
    {
        if (order == null) return;
        if (order.IsReturned)
        {
            MessageBox.Show("Esta orden ya fue devuelta en su totalidad.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var partialWindow = new PosCore.Views.PartialReturnWindow(order);
        if (partialWindow.ShowDialog() != true) return;
        
        var selectedItems = partialWindow.ReturnItems.Where(i => i.IsSelected).ToList();
        
        var overrideWindow = new PosCore.Views.ManagerOverrideWindow("Devolución Parcial", _dbContext);
        if (overrideWindow.ShowDialog() != true || !overrideWindow.IsAuthorized) return;
        
        var reasonWindow = new PosCore.Views.ReasonWindow();
        if (reasonWindow.ShowDialog() != true) return;

        try
        {
            decimal totalRefund = 0;
            foreach(var retItem in selectedItems)
            {
                totalRefund += (retItem.UnitPrice * retItem.ReturnQuantity);
                var origItem = order.Items.First(i => i.Id == retItem.OriginalItem.Id);
                
                // Si devuelven la misma cantidad, eliminamos o marcamos
                // Para mantener historial, restaremos el precio total de la orden y cantidad
                origItem.Quantity -= retItem.ReturnQuantity;
                
                // Restaurar stock
                var product = await _dbContext.Products.FindAsync(origItem.ProductId);
                if (product != null)
                {
                    product.StockQuantity += retItem.ReturnQuantity;
                    product.LastUpdated = DateTime.Now;
                }
            }
            
            // Cleanup items con 0 cantidad
            order.Items.RemoveAll(i => i.Quantity == 0);
            
            order.TotalAmount -= totalRefund;
            if(order.TotalAmount < 0) order.TotalAmount = 0;
            
            order.ReturnReason = reasonWindow.SelectedReason + " (Devolución Parcial)";
            order.AuthorizedBy = overrideWindow.AuthorizedBy;
            order.LastUpdated = DateTime.Now;
            
            if (order.Items.Count == 0)
            {
                order.IsReturned = true; // Si devolvieron todo parcialmente
            }

            // Restar dinero de caja si fue en efectivo
            var currentShift = await _dbContext.CashRegisterShifts.FirstOrDefaultAsync(s => !s.IsClosed);
            if (currentShift != null && order.PaymentDetails.Contains("Efectivo"))
            {
                var cashMovement = new CashMovement
                {
                    ShiftId = currentShift.Id,
                    Amount = -totalRefund,
                    Type = "Salida",
                    Reason = $"Devolución Parcial Orden #{order.Id} - {reasonWindow.SelectedReason}",
                    CreatedAt = DateTime.Now
                };
                _dbContext.CashMovements.Add(cashMovement);
            }
            
            await _dbContext.SaveChangesAsync();
            MessageBox.Show($"Devolución parcial procesada. Reembolso: {totalRefund:C}", "Devolución Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadOrdersAsync();
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    [RelayCommand]
    private async Task ReturnOrderAsync(Order order)
    {
        if (order == null) return;

        if (order.IsReturned)
        {
            MessageBox.Show("Esta orden ya fue devuelta.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"¿Está seguro que desea devolver la orden {order.Id} por {order.TotalAmount:C}?\nEsto sumará los productos al inventario.", "Confirmar Devolución", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var overrideWindow = new PosCore.Views.ManagerOverrideWindow("Devolución de Orden", _dbContext);
                if (overrideWindow.ShowDialog() != true || !overrideWindow.IsAuthorized)
                {
                    return;
                }

                var reasonWindow = new PosCore.Views.ReasonWindow();
                if (reasonWindow.ShowDialog() != true)
                {
                    return;
                }

                // Marcar como devuelta
                order.IsReturned = true;

                order.ReturnReason = reasonWindow.SelectedReason;
                order.AuthorizedBy = overrideWindow.AuthorizedBy;
                order.LastUpdated = DateTime.Now;

                // Restar dinero de caja si fue en efectivo
                var currentShift = await _dbContext.CashRegisterShifts.FirstOrDefaultAsync(s => !s.IsClosed);
                if (currentShift != null && order.PaymentDetails.Contains("Efectivo"))
                {
                    var cashMovement = new CashMovement
                    {
                        ShiftId = currentShift.Id,
                        Amount = -order.TotalAmount,
                        Type = "Salida",
                        Reason = $"Devolución Orden #{order.Id} - {reasonWindow.SelectedReason}",
                        CreatedAt = DateTime.Now
                    };
                    _dbContext.CashMovements.Add(cashMovement);
                }

                // Devolver stock

                foreach (var item in order.Items)
                {
                    var product = await _dbContext.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        product.LastUpdated = DateTime.Now;

                        // Encolar actualización de producto para sync
                        var jsonOptionsProduct = new System.Text.Json.JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
                        var outboxMessageProduct = new OutboxMessage
                        {
                            EventType = "ProductUpdated",
                            Payload = System.Text.Json.JsonSerializer.Serialize(product, jsonOptionsProduct),
                            CreatedAt = DateTime.Now
                        };
                        _dbContext.OutboxMessages.Add(outboxMessageProduct);
                    }
                }

                // Encolar actualización de orden para sync (nota de crédito / devolución)
                var jsonOptionsOrder = new System.Text.Json.JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
                var outboxMessageOrder = new OutboxMessage
                {
                    EventType = "OrderReturned",
                    Payload = System.Text.Json.JsonSerializer.Serialize(order, jsonOptionsOrder),
                    CreatedAt = DateTime.Now
                };
                _dbContext.OutboxMessages.Add(outboxMessageOrder);

                await _dbContext.SaveChangesAsync();

                MessageBox.Show("Devolución procesada con éxito. El inventario ha sido restaurado.", "Devolución Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // IMPRIMIR TICKET DE NOTA DE CRÉDITO (Solo Windows)
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    try
                    {
                        _ticketPrinterService.PrintCreditNote(order, "POS-80");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al intentar imprimir el ticket de devolución: {ex.Message}");
                    }
                }

                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la devolución: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
