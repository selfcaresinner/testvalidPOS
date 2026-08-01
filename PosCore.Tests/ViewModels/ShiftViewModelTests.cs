using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PosCore.Data;
using PosCore.Models;
using PosCore.Services;
using PosCore.ViewModels;
using Xunit;
using System;
using System.Linq;

namespace PosCore.Tests.ViewModels;

public class ShiftViewModelTests
{
    private PosDbContext GetInMemoryDbContext(SessionManager sessionManager)
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var dbContext = new PosDbContext(options, sessionManager);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public void LoadCurrentShift_ShouldCalculateExpectedCashCorrectly()
    {
        // Arrange
        var sessionManager = new SessionManager { CurrentTenantId = "test-tenant", Role = "Admin" };
        var dbContext = GetInMemoryDbContext(sessionManager);
        
        // Add an active shift
        var shift = new CashRegisterShift 
        { 
            OpenedAt = DateTime.Now.AddHours(-2), 
            StartingCash = 1000m, 
            IsClosed = false 
        };
        dbContext.CashRegisterShifts.Add(shift);
        dbContext.SaveChanges(); // to get shift.Id
        
        // Add some orders in cash
        dbContext.Orders.Add(new Order { OrderDate = DateTime.Now.AddHours(-1), TotalAmount = 500m, PaymentDetails = "Efectivo", IsReturned = false });
        dbContext.Orders.Add(new Order { OrderDate = DateTime.Now.AddMinutes(-30), TotalAmount = 250m, PaymentDetails = "Efectivo", IsReturned = false });
        
        // Order in card (should not be counted)
        dbContext.Orders.Add(new Order { OrderDate = DateTime.Now.AddMinutes(-15), TotalAmount = 100m, PaymentDetails = "Tarjeta", IsReturned = false });
        
        // Add a cash movement (e.g., cash withdrawal)
        dbContext.CashMovements.Add(new CashMovement { ShiftId = shift.Id, Amount = -100m, Type = "Withdrawal", Reason = "Pago proveedor" });
        
        dbContext.SaveChanges();

        // Act
        var viewModel = new ShiftViewModel(dbContext, sessionManager);

        // Assert
        viewModel.HasActiveShift.Should().BeTrue();
        viewModel.ExpectedEndingCash.Should().Be(1650m); // 1000 (Initial) + 500 (Sale 1) + 250 (Sale 2) - 100 (Withdrawal)
    }
}
