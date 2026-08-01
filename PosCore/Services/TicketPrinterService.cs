using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PosCore.Models;
using Serilog;
using Microsoft.Extensions.Options;

namespace PosCore.Services
{
    public class TicketPrinterService
    {
        private readonly AppSettings _settings;

        public TicketPrinterService(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }

        // ESC/POS Commands
        private static readonly byte[] ESC_INIT = new byte[] { 27, 64 };
        private static readonly byte[] ESC_ALIGN_CENTER = new byte[] { 27, 97, 1 };
        private static readonly byte[] ESC_ALIGN_LEFT = new byte[] { 27, 97, 0 };
        private static readonly byte[] ESC_ALIGN_RIGHT = new byte[] { 27, 97, 2 };
        private static readonly byte[] ESC_BOLD_ON = new byte[] { 27, 69, 1 };
        private static readonly byte[] ESC_BOLD_OFF = new byte[] { 27, 69, 0 };
        private static readonly byte[] ESC_CUT = new byte[] { 29, 86, 66, 0 };
        private static readonly byte[] ESC_DRAWER = new byte[] { 27, 112, 0, 25, 250 };

        
        public bool PrintTicket(Order order, string? portName = null)
        {
            portName ??= _settings.Printer.PortName;
            try
            {
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Log.Warning("La impresión directa solo es compatible en Windows.");
                    return false;
                }

                // Simular chequeo de hardware (ej: sin papel, desconectada)
                // En un escenario real, esto se hace verificando el estado de la impresora WMI o API de Windows
                bool isOffline = false; // dummy
                if (isOffline)
                {
                    MessageBox.Show("La impresora está desconectada o sin papel.", "Error de Impresora", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                using (var ms = new MemoryStream())
                {
                    ms.Write(ESC_INIT, 0, ESC_INIT.Length);
                    
                    // Logo Support (Optional simulated bitmap logic)
                    if (_settings.Printer.PrintLogo)
                    {
                        ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                        WriteString(ms, "[ LOGO DE EMPRESA ]\n\n");
                    }

                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);

                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"--- {_settings.WhiteLabel.CompanyName.ToUpper()} ---\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    WriteString(ms, "Ticket de Venta\n");
                    WriteString(ms, $"Fecha: {order.OrderDate:dd/MM/yyyy HH:mm:ss}\n");
                    WriteString(ms, $"Ticket ID: {order.Id}\n");
                    WriteString(ms, "--------------------------------\n");
                    
                    ms.Write(ESC_ALIGN_LEFT, 0, ESC_ALIGN_LEFT.Length);
                    foreach (var item in order.Items)
                    {
                        string productName = item.Product?.Name ?? "Producto Indefinido";
                        if (productName.Length > 20) productName = productName.Substring(0, 20);
                        
                        string line = $"{item.Quantity}x {productName.PadRight(20)} {item.SubTotal.ToString("C").PadLeft(8)}\n";
                        WriteString(ms, line);
                    }
                    WriteString(ms, "--------------------------------\n");
                    
                    decimal taxRate = 0.16m; // IVA 16%
                    decimal subtotal = order.TotalAmount / (1 + taxRate);
                    decimal taxes = order.TotalAmount - subtotal;
                    
                    ms.Write(ESC_ALIGN_RIGHT, 0, ESC_ALIGN_RIGHT.Length);
                    WriteString(ms, $"SUBTOTAL: {subtotal.ToString("C")}\n");
                    WriteString(ms, $"IVA (16%): {taxes.ToString("C")}\n");
                    
                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"TOTAL: {order.TotalAmount.ToString("C")}\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    
                    if (!string.IsNullOrWhiteSpace(order.PaymentDetails))
                    {
                        WriteString(ms, "\nPagos:\n");
                        string[] payments = order.PaymentDetails.Split(',');
                        foreach(var p in payments) {
                            WriteString(ms, $"{p.Trim()}\n");
                        }
                    }
                    
                    WriteString(ms, $"\n{_settings.Tax?.ReceiptFooter ?? "¡Gracias por su compra!"}\n\n\n\n\n\n");
                    ms.Write(ESC_DRAWER, 0, ESC_DRAWER.Length);
                    ms.Write(ESC_CUT, 0, ESC_CUT.Length);
                    
                    byte[] dataToPrint = ms.ToArray();
                    bool success = RawPrinterHelper.SendBytesToPrinter(portName, dataToPrint);
                    
                    if (success)
                        Log.Information($"Ticket impreso exitosamente para la orden {order.Id} en la impresora {portName}");
                    else
                        Log.Error($"Error de WinSpool al enviar ticket de la orden {order.Id} a la impresora {portName}");
                    return success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error al intentar imprimir el ticket en la impresora {portName}");
                return false;
            }
        }

        
        public bool PrintShiftTicket(CashRegisterShift shift, string? portName = null)
        {
            portName ??= _settings.Printer.PortName;
            try
            {
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Log.Warning("La impresión directa solo es compatible en Windows.");
                    return false;
                }
                using (var ms = new MemoryStream())
                {
                    ms.Write(ESC_INIT, 0, ESC_INIT.Length);
                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"--- {_settings.WhiteLabel.CompanyName.ToUpper()} ---\n");
                    WriteString(ms, "*** CORTE DE TURNO ***\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    
                    WriteString(ms, $"Cajero: {shift.ClosedBy}\n");
                    WriteString(ms, $"Apertura: {shift.OpenedAt:dd/MM/yyyy HH:mm}\n");
                    WriteString(ms, $"Cierre: {shift.ClosedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}\n");
                    WriteString(ms, "--------------------------------\n");
                    
                    ms.Write(ESC_ALIGN_LEFT, 0, ESC_ALIGN_LEFT.Length);
                    WriteString(ms, $"Fondo Inicial:     {(shift.StartingCash).ToString("C").PadLeft(12)}\n");
                    WriteString(ms, $"Esperado (Total):  {(shift.ExpectedEndingCash ?? 0).ToString("C").PadLeft(12)}\n");
                    WriteString(ms, $"Contado en Caja:   {(shift.ActualEndingCash ?? 0).ToString("C").PadLeft(12)}\n");
                    WriteString(ms, "--------------------------------\n");
                    
                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"DIFERENCIA:        {(shift.Difference ?? 0).ToString("C").PadLeft(12)}\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    
                    WriteString(ms, "\n\n\n\n\n");
                    ms.Write(ESC_DRAWER, 0, ESC_DRAWER.Length);
                    ms.Write(ESC_CUT, 0, ESC_CUT.Length);
                    
                    using (var port = new System.IO.Ports.SerialPort(portName, 9600))
                    {
                        port.Open();
                        port.Write(ms.ToArray(), 0, (int)ms.Length);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing shift ticket");
                return false;
            }
        }

        public bool PrintCreditNote(Order order, string? portName = null)
        {
            portName ??= _settings.Printer.PortName;
            try
            {
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Log.Warning("La impresión directa solo es compatible en Windows.");
                    return false;
                }

                using (var ms = new MemoryStream())
                {
                    ms.Write(ESC_INIT, 0, ESC_INIT.Length);
                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"--- {_settings.WhiteLabel.CompanyName.ToUpper()} ---\n");
                    WriteString(ms, "*** NOTA DE CREDITO ***\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    WriteString(ms, $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n");
                    WriteString(ms, $"Ref Ticket ID: {order.Id}\n");
                    WriteString(ms, "--------------------------------\n");
                    
                    ms.Write(ESC_ALIGN_LEFT, 0, ESC_ALIGN_LEFT.Length);
                    foreach (var item in order.Items)
                    {
                        string productName = item.Product?.Name ?? "Producto";
                        if (productName.Length > 20) productName = productName.Substring(0, 20);
                        
                        string line = $"{item.Quantity}x {productName.PadRight(20)} {item.SubTotal.ToString("C").PadLeft(8)}\n";
                        WriteString(ms, line);
                    }
                    WriteString(ms, "--------------------------------\n");
                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"TOTAL DEVUELTO: {order.TotalAmount.ToString("C")}\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    WriteString(ms, "\nComprobante de devolucion\n\n\n\n\n\n");
                    ms.Write(ESC_DRAWER, 0, ESC_DRAWER.Length);
                    ms.Write(ESC_CUT, 0, ESC_CUT.Length);
                    
                    byte[] dataToPrint = ms.ToArray();
                    bool success = RawPrinterHelper.SendBytesToPrinter(portName, dataToPrint);
                    
                    if (success)
                        Log.Information($"Nota de credito impresa exitosamente para la orden {order.Id} en la impresora {portName}");
                    else
                        Log.Error($"Error de WinSpool al enviar nota de credito de la orden {order.Id} a la impresora {portName}");
                    return success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error al intentar imprimir la nota de credito en {portName}");
                return false;
            }
        }

        
        public bool TestPrinter(string? portName = null)
        {
            portName ??= _settings.Printer.PortName;
            try
            {
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Log.Warning("La impresión directa solo es compatible en Windows.");
                    return false;
                }

                using (var ms = new MemoryStream())
                {
                    ms.Write(ESC_INIT, 0, ESC_INIT.Length);
                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                    ms.Write(ESC_BOLD_ON, 0, ESC_BOLD_ON.Length);
                    WriteString(ms, $"--- {_settings.WhiteLabel.CompanyName.ToUpper()} ---\n");
                    ms.Write(ESC_BOLD_OFF, 0, ESC_BOLD_OFF.Length);
                    WriteString(ms, "\n*** PRUEBA DE IMPRESION ***\n\n");
                    WriteString(ms, $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n");
                    WriteString(ms, $"Impresora configurada: {portName}\n");
                    WriteString(ms, "--------------------------------\n");
                    
                    ms.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
                    WriteString(ms, "Si puedes leer esto, la impresora\n");
                    WriteString(ms, "esta configurada correctamente.\n\n\n\n\n");
                    ms.Write(ESC_DRAWER, 0, ESC_DRAWER.Length);
                    ms.Write(ESC_CUT, 0, ESC_CUT.Length);
                    
                    byte[] dataToPrint = ms.ToArray();
                    bool success = RawPrinterHelper.SendBytesToPrinter(portName, dataToPrint);
                    
                    if (success)
                        Log.Information($"Prueba de impresión exitosa en la impresora {portName}");
                    else
                        Log.Error($"Error de WinSpool al enviar prueba a la impresora {portName}");
                    return success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error al intentar imprimir la prueba en {portName}");
                return false;
            }
        }

        private void WriteString(MemoryStream ms, string text)
        {
            // Encoding 850 / UTF8 can be adjusted here if special characters appear wrong, 
            // but ASCII is safest for standard ESC/POS
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            ms.Write(bytes, 0, bytes.Length);
        }
    }
}
