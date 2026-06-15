namespace CyberSpaceNIS2.API.Models;

public class Spielverlauf
{
    public int SpieverlaufId { get; set; }
    public int SessionId { get; set; }
    public int KarteId { get; set; }
    public int? OptionId { get; set; }
    public int SpielerId { get; set; }
    public DateTime Zeitstempel { get; set; } = DateTime.UtcNow;
    public int ErhaltePunkte { get; set; } = 0;
}