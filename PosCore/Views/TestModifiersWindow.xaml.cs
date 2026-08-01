using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PosCore.Models;

namespace PosCore.Views
{
    public partial class TestModifiersWindow : Window
    {
        private decimal _basePrice = 65.00m;
        private List<ProductModifier> _mockModifiers = new();
        private List<CheckBox> _allCheckboxes = new();
        private List<RadioButton> _allRadioButtons = new();

        public TestModifiersWindow()
        {
            InitializeComponent();
            SetupMockData();
            RenderModifiers();
            UpdateTotal();
        }

        private void SetupMockData()
        {
            _mockModifiers = new List<ProductModifier>
            {
                new ProductModifier
                {
                    Name = "Tipo de Leche",
                    IsRequired = true,
                    MinSelections = 1,
                    MaxSelections = 1,
                    Options = new List<ModifierOption>
                    {
                        new ModifierOption { Name = "Entera", PriceAdjustment = 0, IsDefault = true },
                        new ModifierOption { Name = "Deslactosada", PriceAdjustment = 5 },
                        new ModifierOption { Name = "Almendra", PriceAdjustment = 12 },
                        new ModifierOption { Name = "Avena", PriceAdjustment = 15 }
                    }
                },
                new ProductModifier
                {
                    Name = "Temperatura",
                    IsRequired = true,
                    MinSelections = 1,
                    MaxSelections = 1,
                    Options = new List<ModifierOption>
                    {
                        new ModifierOption { Name = "Caliente", PriceAdjustment = 0, IsDefault = true },
                        new ModifierOption { Name = "Frío (Hielo)", PriceAdjustment = 0 },
                        new ModifierOption { Name = "Frappé", PriceAdjustment = 10 }
                    }
                },
                new ProductModifier
                {
                    Name = "Extras y Jarabes",
                    IsRequired = false,
                    MinSelections = 0,
                    MaxSelections = 3,
                    Options = new List<ModifierOption>
                    {
                        new ModifierOption { Name = "Shot Extra de Espresso", PriceAdjustment = 15 },
                        new ModifierOption { Name = "Jarabe de Vainilla", PriceAdjustment = 10 },
                        new ModifierOption { Name = "Jarabe de Caramelo", PriceAdjustment = 10 },
                        new ModifierOption { Name = "Crema Batida", PriceAdjustment = 12 }
                    }
                }
            };
        }

        private void RenderModifiers()
        {
            foreach (var modGroup in _mockModifiers)
            {
                var groupBorder = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10, 5, 10, 10),
                    Padding = new Thickness(15)
                };

                var groupStack = new StackPanel();

                // Título del grupo
                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                var titleText = new TextBlock
                {
                    Text = modGroup.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"))
                };
                
                var requirementText = new TextBlock
                {
                    Text = modGroup.IsRequired ? "Obligatorio" : "Opcional",
                    FontSize = 12,
                    Foreground = modGroup.IsRequired ? Brushes.Red : Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                headerGrid.Children.Add(titleText);
                headerGrid.Children.Add(requirementText);
                groupStack.Children.Add(headerGrid);

                // Opciones
                bool isSingleSelection = modGroup.MaxSelections == 1;

                foreach (var option in modGroup.Options)
                {
                    var optionGrid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                    optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var priceText = new TextBlock
                    {
                        Text = option.PriceAdjustment > 0 ? $"+${option.PriceAdjustment:F2}" : "",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(priceText, 1);

                    if (isSingleSelection)
                    {
                        var rb = new RadioButton
                        {
                            Content = option.Name,
                            GroupName = modGroup.Name,
                            IsChecked = option.IsDefault,
                            VerticalAlignment = VerticalAlignment.Center,
                            Tag = option
                        };
                        rb.Checked += (s, e) => UpdateTotal();
                        _allRadioButtons.Add(rb);
                        optionGrid.Children.Add(rb);
                    }
                    else
                    {
                        var cb = new CheckBox
                        {
                            Content = option.Name,
                            IsChecked = option.IsDefault,
                            VerticalAlignment = VerticalAlignment.Center,
                            Tag = option
                        };
                        cb.Checked += (s, e) => UpdateTotal();
                        cb.Unchecked += (s, e) => UpdateTotal();
                        _allCheckboxes.Add(cb);
                        optionGrid.Children.Add(cb);
                    }
                    
                    optionGrid.Children.Add(priceText);
                    groupStack.Children.Add(optionGrid);
                }

                groupBorder.Child = groupStack;
                ModifiersPanel.Children.Add(groupBorder);
            }
        }

        private void UpdateTotal()
        {
            decimal total = _basePrice;

            foreach (var rb in _allRadioButtons.Where(r => r.IsChecked == true))
            {
                var opt = (ModifierOption)rb.Tag;
                total += opt.PriceAdjustment;
            }

            foreach (var cb in _allCheckboxes.Where(c => c.IsChecked == true))
            {
                var opt = (ModifierOption)cb.Tag;
                total += opt.PriceAdjustment;
            }

            if (TotalPriceText != null)
                TotalPriceText.Text = $"${total:F2}";
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selectedModifiers = new List<object>();

            foreach (var rb in _allRadioButtons.Where(r => r.IsChecked == true))
            {
                var opt = (ModifierOption)rb.Tag;
                selectedModifiers.Add(new {
                    Nombre = opt.ProductModifier?.Name ?? ((RadioButton)rb).GroupName,
                    Seleccion = opt.Name,
                    CostoExtra = opt.PriceAdjustment
                });
            }

            foreach (var cb in _allCheckboxes.Where(c => c.IsChecked == true))
            {
                var opt = (ModifierOption)cb.Tag;
                selectedModifiers.Add(new {
                    Nombre = opt.ProductModifier?.Name ?? "Extras",
                    Seleccion = opt.Name,
                    CostoExtra = opt.PriceAdjustment
                });
            }

            var jsonResult = System.Text.Json.JsonSerializer.Serialize(selectedModifiers, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            
            MessageBox.Show($"Producto agregado con los siguientes modificadores en CustomAttributes:\n\n{jsonResult}", "Orden Actualizada", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
