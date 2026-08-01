using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PosCore.Data;
using PosCore.Models;
using PosCore.Services;
using PosCore.ViewModels;
using Xunit;

namespace PosCore.Tests.ViewModels;

public class MainViewModelTests
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
    public void AddToCart_ShouldIncreaseQuantity_WhenProductAlreadyExists()
    {
        // Arrange
        var sessionManager = new SessionManager { CurrentTenantId = "test-tenant", Role = "Admin" };
        var dbContext = GetInMemoryDbContext(sessionManager);
        var mockApiService = new Mock<IApiService>();
        var settings = Options.Create(new AppSettings());
        
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(PosDbContext))).Returns(dbContext);
        serviceProviderMock.Setup(x => x.GetService(typeof(IApiService))).Returns(mockApiService.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(SessionManager))).Returns(sessionManager);

        var syncService = new SyncService(serviceProviderMock.Object, new NullLogger<SyncService>());
        var ticketPrinter = new TicketPrinterService(settings);
        
        var viewModel = new MainViewModel(dbContext, mockApiService.Object, settings, syncService, ticketPrinter, sessionManager);

        var product = new Product { Id = 1, Name = "Test Product", Price = 100, StockQuantity = 10 };
            
        // Act
        viewModel.AddToCartCommand.Execute(product); // Add first time
        viewModel.AddToCartCommand.Execute(product); // Add second time

        // Assert
        viewModel.Cart.Should().HaveCount(1);
        viewModel.Cart.First().Quantity.Should().Be(2);
        viewModel.Total.Should().Be(200);
    }
}
