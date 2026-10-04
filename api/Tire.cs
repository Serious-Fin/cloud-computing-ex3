public enum TireType
{
    Summer,
    Winter,
    AllSeason
}

public class Tire
{
    public int Id { get; set; }
    public string? Brand { get; set; }
    public TireType? Type { get; set; }
    public int RimDiameter { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int ViewsLastHour { get; set; }
    public DateTimeOffset? ViewsUpdatedAt { get; set; }
}
