using System;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosCore.Data;
using PosCore.Models;
using PosCore.Services;

namespace PosCore.ViewModels;

public partial class ShiftViewModel : ObservableObject
{
    private readonly PosDbContext _dbContext;
    private readonly SessionManager _sessionManager;

    [ObservableProperty]
    private CashRegisterShift? _currentShift;

    [ObservableProperty]
    private decimal _startingCash = 0;

    [ObservableProperty]
    private decimal _actualEndingCash = 0;

    [ObservableProperty]
    private decimal _expectedEndingCash = 0;

    [ObservableProperty]
    private decimal _difference = 0;

    [ObservableProperty]
    private bool _hasActiveShift = false;

    private readonly TicketPrinterService _ticketPrinterService;
    public ShiftViewModel(PosDbContext dbContext, SessionManager sessionManager, TicketPrinterService ticketPrinterService)
    {
        _dbContext = dbContext;
        _sessionManager = sessionManager;
        _ticketPrinterService = ticketPrinterService;
        LoadCurrentShift();
    }

    private void LoadCurrentShift()
    {
        CurrentShift = _dbContext.CashRegisterShifts.FirstOrDefault(s => !s.IsClosed);
        HasActiveShift = CurrentShift != null;

        if (HasActiveShift && CurrentShift != null)
        {
            // Calculate Expected Ending Cash
            // Start with starting cash
            decimal cashSales = _dbContext.Orders
                .Where(o => o.OrderDate >= CurrentShift.OpenedAt && !o.IsReturned && o.PaymentDetails.Contains("Efectivo"))
                .AsEnumerable()
                .Sum(o => o.TotalAmount);
            
            // Add cash movements (in/out)
            decimal movements = _dbContext.CashMovements
                .Where(c => c.ShiftId == CurrentShift.Id)
                .AsEnumerable()
                .Sum(c => c.Amount);

            ExpectedEndingCash = CurrentShift.StartingCash + cashSales + movements;
            ActualEndingCash = ExpectedEndingCash; // Default to expected for easy closing
            CalculateDifference();
        }
    }

    partial void OnActualEndingCashChanged(decimal value)
    {
        CalculateDifference();
    }

    private void CalculateDifference()
    {
        Difference = ActualEndingCash - ExpectedEndingCash;
    }

    [RelayCommand]
    private void OpenShift(Window window)
    {
        if (HasActiveShift)
        {
            MessageBox.Show("Ya hay un turno abierto.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var shift = new CashRegisterShift
        {
            OpenedAt = DateTime.Now,
            OpenedBy = string.IsNullOrEmpty(_sessionManager.Username) ? "Admin" : _sessionManager.Username,
            StartingCash = StartingCash,
            IsClosed = false
        };

        _dbContext.CashRegisterShifts.Add(shift);
        _dbContext.SaveChanges();

        MessageBox.Show($"Turno abierto con un fondo de {StartingCash:C}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        window.Close();
    }


    [RelayCommand]
    private void RegisterWithdrawal()
    {
        if (!HasActiveShift || CurrentShift == null) return;
        
        var dialog = new PosCore.Views.WithdrawalWindow();
        if (dialog.ShowDialog() == true && dialog.Amount > 0)
        {
            var movement = new CashMovement
            {
                ShiftId = CurrentShift.Id,
                Amount = -dialog.Amount,
                Type = "Salida",
                Reason = string.IsNullOrWhiteSpace(dialog.Reason) ? "Retiro Parcial" : dialog.Reason,
                CreatedAt = DateTime.Now
            };
            _dbContext.CashMovements.Add(movement);
            _dbContext.SaveChanges();
            
            MessageBox.Show($"Retiro de {dialog.Amount:C} registrado correctamente.", "Retiro de Efectivo", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadCurrentShift(); // Recalculate
        }
    }

    [RelayCommand]
    private void CloseShift(Window window)
    {
        if (!HasActiveShift || CurrentShift == null)
        {
            MessageBox.Show("No hay turno activo para cerrar.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CurrentShift.ClosedAt = DateTime.Now;
        CurrentShift.ClosedBy = string.IsNullOrEmpty(_sessionManager.Username) ? "Admin" : _sessionManager.Username;
        CurrentShift.ExpectedEndingCash = ExpectedEndingCash;
        CurrentShift.ActualEndingCash = ActualEndingCash;
        CurrentShift.Difference = Difference;
        CurrentShift.IsClosed = true;

        _dbContext.SaveChanges();

        MessageBox.Show($"Turno cerrado.\nEsperado: {ExpectedEndingCash:C}\nContado: {ActualEndingCash:C}\nDiferencia: {Difference:C}", "Arqueo de Caja", MessageBoxButton.OK, MessageBoxImage.Information);
        window.Close();
    }
}
