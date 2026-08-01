using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosCore.Data;
using PosCore.Models;
using System.Windows;
using System.Text.Json;
using System;

namespace PosCore.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly PosDbContext _dbContext;
    private readonly Services.SyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private Product _editingProduct = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        if (LoadProductsCommand.CanExecute(null))
            LoadProductsCommand.Execute(null);
    }


    public InventoryViewModel(PosDbContext dbContext, Services.SyncService syncService)
    {
        _dbContext = dbContext;
        _syncService = syncService;

        _syncService.OnSyncCompleted += () => 
        {
            if (LoadProductsCommand.CanExecute(null))
            {
                LoadProductsCommand.Execute(null);
            }
        };

        LoadProductsCommand.Execute(null);
    }

    
    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var query = _dbContext.Products.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var lowerQuery = SearchQuery.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(lowerQuery) || p.Barcode.ToLower().Contains(lowerQuery));
        }

        var products = await query.ToListAsync();
        Products.Clear();
        foreach (var p in products)
        {
            Products.Add(p);
        }
    }


    [RelayCommand]
    private void AddProduct()
    {
        EditingProduct = new Product { StockQuantity = 0, Price = 0, MinStockThreshold = 10 };
        IsEditing = true;
    }

    [RelayCommand]
    private void EditProduct()
    {
        if (SelectedProduct == null) return;
        
        EditingProduct = new Product
        {
            Id = SelectedProduct.Id,
            Name = SelectedProduct.Name,
            Barcode = SelectedProduct.Barcode,
            Price = SelectedProduct.Price,
            StockQuantity = SelectedProduct.StockQuantity,
            MinStockThreshold = SelectedProduct.MinStockThreshold,
            TenantId = SelectedProduct.TenantId,
            LastUpdated = SelectedProduct.LastUpdated
        };
        
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(EditingProduct.Name) || string.IsNullOrWhiteSpace(EditingProduct.Barcode))
        {
            MessageBox.Show("El nombre y el código de barras son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string eventType = "";

            if (EditingProduct.Id == 0)
            {
                bool exists = await _dbContext.Products.AnyAsync(p => p.Barcode == EditingProduct.Barcode);
                if (exists)
                {
                    MessageBox.Show("El código de barras ya existe.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _dbContext.Products.Add(EditingProduct);
                eventType = "ProductCreated";
            }
            else
            {
                bool exists = await _dbContext.Products.AnyAsync(p => p.Barcode == EditingProduct.Barcode && p.Id != EditingProduct.Id);
                if (exists)
                {
                    MessageBox.Show("El código de barras ya está asignado a otro producto.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var existing = await _dbContext.Products.FindAsync(EditingProduct.Id);
                if (existing != null)
                {
                    existing.Name = EditingProduct.Name;
                    existing.Barcode = EditingProduct.Barcode;
                    existing.Price = EditingProduct.Price;
                    existing.StockQuantity = EditingProduct.StockQuantity;
                    existing.MinStockThreshold = EditingProduct.MinStockThreshold;
                    existing.LastUpdated = DateTime.UtcNow; // Set explicitly before saving to outbox
                    _dbContext.Products.Update(existing);
                    
                    // Actualizar el EditingProduct con la info más reciente para el outbox
                    EditingProduct.LastUpdated = existing.LastUpdated;
                }
                eventType = "ProductUpdated";
            }

            var jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
            var outboxMessage = new OutboxMessage
            {
                EventType = eventType,
                Payload = JsonSerializer.Serialize(EditingProduct, jsonOptions),
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.OutboxMessages.Add(outboxMessage);

            await _dbContext.SaveChangesAsync();
            IsEditing = false;
            await LoadProductsAsync();
            MessageBox.Show("Producto guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error al guardar producto: {ex.Message}\nDetalle: {ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }


    [RelayCommand]
    private void GenerateBarcode()
    {
        if (EditingProduct != null)
        {
            EditingProduct.Barcode = "GEN-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }

        [RelayCommand]
    private async Task ImportProductsAsync()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Archivos CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
            Title = "Seleccionar archivo CSV de productos"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(openFileDialog.FileName);
                if (lines.Length <= 1)
                {
                    System.Windows.MessageBox.Show("El archivo está vacío o no tiene datos válidos.", "Importar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                int importedCount = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cols = line.Split(',');
                    if (cols.Length >= 5) // Barcode, Name, Category, Price, StockQuantity
                    {
                        var barcode = cols[0].Trim();
                        var existing = System.Linq.Enumerable.FirstOrDefault(_dbContext.Products, p => p.Barcode == barcode);
                        if (existing == null)
                        {
                            var newProduct = new PosCore.Models.Product
                            {
                                Barcode = barcode,
                                Name = cols[1].Trim(),
                                Category = cols[2].Trim(),
                                Price = decimal.TryParse(cols[3].Trim(), out decimal p) ? p : 0m,
                                StockQuantity = int.TryParse(cols[4].Trim(), out int sq) ? sq : 0,
                                MinStockThreshold = cols.Length > 5 && int.TryParse(cols[5].Trim(), out int mst) ? mst : 10,
                                LastUpdated = System.DateTime.Now
                            };
                            _dbContext.Products.Add(newProduct);

                            var payload = System.Text.Json.JsonSerializer.Serialize(newProduct, new System.Text.Json.JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles });
                            _dbContext.OutboxMessages.Add(new PosCore.Models.OutboxMessage
                            {
                                EventType = "ProductCreated",
                                Payload = payload,
                                CreatedAt = System.DateTime.Now
                            });

                            importedCount++;
                        }
                    }
                }

                if (importedCount > 0)
                {
                    await _dbContext.SaveChangesAsync();
                    LoadProductsCommand.Execute(null);
                    System.Windows.MessageBox.Show($"Se importaron {importedCount} productos exitosamente.", "Importar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("No se encontraron productos nuevos para importar.", "Importar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al importar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

            [RelayCommand]
    private void ExportProducts()
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Archivos PDF (*.pdf)|*.pdf|Archivos Excel (*.xls)|*.xls",
            Title = "Guardar productos exportados",
            FileName = "Inventario_Productos"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                var allProducts = System.Linq.Enumerable.ToList(_dbContext.Products);
                
                if (saveFileDialog.FileName.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                {
                    QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(QuestPDF.Helpers.PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                            page.PageColor(QuestPDF.Helpers.Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(QuestPDF.Helpers.Fonts.Arial));

                            page.Header().Element(ComposeHeader);
                            page.Content().Element(x => ComposeContent(x, allProducts));
                            page.Footer().Element(ComposeFooter);
                        });
                    })
                    .GeneratePdf(saveFileDialog.FileName);
                }
                else
                {
                    var html = new System.Text.StringBuilder();
                    html.AppendLine("<html><head><meta charset='utf-8'><style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #dddddd; padding: 8px; text-align: left; } th { background-color: #f2f2f2; }</style></head><body>");
                    html.AppendLine("<h2>Inventario de Productos</h2>");
                    html.AppendLine("<table><tr><th>Código</th><th>Nombre</th><th>Categoría</th><th>Precio</th><th>Stock</th><th>Min. Stock</th></tr>");

                    foreach (var product in allProducts)
                    {
                        html.AppendLine($"<tr><td>{product.Barcode}</td><td>{product.Name}</td><td>{product.Category}</td><td>{product.Price:C}</td><td>{product.StockQuantity}</td><td>{product.MinStockThreshold}</td></tr>");
                    }
                    html.AppendLine("</table></body></html>");
                    System.IO.File.WriteAllText(saveFileDialog.FileName, html.ToString(), System.Text.Encoding.UTF8);
                }

                System.Windows.MessageBox.Show("Productos exportados exitosamente.", "Exportar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void ComposeHeader(QuestPDF.Infrastructure.IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Reporte de Inventario").FontSize(20).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                column.Item().Text($"Generado el: {System.DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(QuestPDF.Infrastructure.IContainer container, System.Collections.Generic.List<PosCore.Models.Product> products)
    {
        container.PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Código");
                    header.Cell().Element(CellStyle).Text("Nombre");
                    header.Cell().Element(CellStyle).Text("Categoría");
                    header.Cell().Element(CellStyle).Text("Precio");
                    header.Cell().Element(CellStyle).Text("Stock");

                    QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black);
                    }
                });

                foreach (var product in products)
                {
                    table.Cell().Element(CellStyle).Text(product.Barcode);
                    table.Cell().Element(CellStyle).Text(product.Name);
                    table.Cell().Element(CellStyle).Text(product.Category);
                    table.Cell().Element(CellStyle).Text(product.Price.ToString("C"));
                    table.Cell().Element(CellStyle).Text(product.StockQuantity.ToString());

                    QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                }
            });
        });
    }

    private void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
        });
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        var productToDelete = SelectedProduct;
        if (productToDelete == null) return;
        var result = MessageBox.Show($"¿Está seguro de eliminar el producto '{productToDelete.Name}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var productId = productToDelete.Id;
                var outboxMessage = new PosCore.Models.OutboxMessage
                {
                    EventType = "ProductDeleted",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new { Id = productId, Barcode = productToDelete.Barcode }),
                    CreatedAt = System.DateTime.Now
                };
                _dbContext.OutboxMessages.Add(outboxMessage);
                await _dbContext.Products.Where(p => p.Id == productId).ExecuteDeleteAsync();
                await _dbContext.SaveChangesAsync();
                await LoadProductsAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
