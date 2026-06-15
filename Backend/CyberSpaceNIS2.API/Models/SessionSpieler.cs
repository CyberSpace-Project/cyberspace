namespace CyberSpaceNIS2.API.Models;

public class SessionSpieler
{
    public int SessionId { get; set; }
    public int SpielerId { get; set; }
    public int? RolleId { get; set; }
    public string Status { get; set; } = "Eingeladen";
    public int Punkte { get; set; } = 0;
    public DateTime? BeigetretenAm { get; set; }
}