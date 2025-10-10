public class EmployeeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }

    // FKs to save into DB
    public int DepartmentID { get; set; }
    public int PositionID { get; set; }

    // Display only
    public string DepartmentName { get; set; }
    public string PositionName { get; set; }

    public string Department => $"{DepartmentName} / {PositionName}";

    public string Shift { get; set; }
    public string Sex { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
    public string ContactNumber { get; set; }
    public decimal BaseSalary { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? DOB { get; set; }

}
