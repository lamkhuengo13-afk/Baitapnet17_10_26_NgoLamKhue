using System;

namespace ClinicAppointmentManager.Models
{
    public class Appointment
    {
        public string AppointmentId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public DateTime AppointmentDateTime { get; set; } = DateTime.Now;
        public string Doctor { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "Chờ khám";
    }
}
