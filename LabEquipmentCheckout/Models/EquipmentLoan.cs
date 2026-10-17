using System;

namespace LabEquipmentCheckout.Models
{
    public class EquipmentLoan
    {
        public string LoanId { get; set; } = "";
        public string StudentId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string EquipmentName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public DateTime BorrowDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
        public DateTime? ActualReturnDate { get; set; }

        public string Status
        {
            get
            {
                if (ActualReturnDate.HasValue)
                    return "Đã trả";

                return DueDate.Date < DateTime.Today ? "Quá hạn" : "Đang mượn";
            }
        }
    }
}
