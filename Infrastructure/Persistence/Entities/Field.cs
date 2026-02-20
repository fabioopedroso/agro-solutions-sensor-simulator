namespace Infrastructure.Persistence.Entities;

public class Field
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CropType { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public DateTime? PlantingDate { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Status { get; set; }
    public int PropertyId { get; set; }
    public DateTime CreatedAt { get; set; }
}
