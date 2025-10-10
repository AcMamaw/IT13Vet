namespace VeterinarianEMS.Models
{
    public class AttendanceModel
    {
        public int Number { get; set; } // row numbering
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }   // computed: e.g., "Present", "Absent"
        public DateTime DateTime { get; set; }
    }
}
