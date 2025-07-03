namespace CRM.DTOs
{
    /// <summary>
    /// Chart verileri için DTO
    /// Grafik bileşenlerinde kullanılır
    /// </summary>
    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
        public List<string> Colors { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "bar"; // bar, line, pie, doughnut
    }
}
