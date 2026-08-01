using System;

namespace PosCore.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public int UserId { get; set; } // The cashier who opened the shift
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public decimal InitialCash { get; set; } // Fondo de caja inicial
        public decimal ExpectedCash { get; set; } // Efectivo esperado (Ventas en efectivo - Devoluciones + Inicial)
        public decimal ActualCash { get; set; } // Efectivo contado (Arqueo)
        
        public decimal Discrepancy => ActualCash - ExpectedCash; // Diferencia (Faltante / Sobrante)
        
        public bool IsClosed => EndTime.HasValue;
    }
}
