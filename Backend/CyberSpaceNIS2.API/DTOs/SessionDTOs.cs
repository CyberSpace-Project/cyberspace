namespace CyberSpaceNIS2.API.DTOs;

public class CreateSessionRequest
{
    public int SzenarioId { get; set; }
    public int ModeratorId { get; set; }
}

public class SessionResponse
{
    public int SessionId { get; set; }
    public int SzenarioId { get; set; }
    public int ModeratorId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartZeit { get; set; }
    public DateTime? EndZeit { get; set; }
    public DateTime ErstelltAm { get; set; }
}

public class RolleWaehlenRequest
{
    public int SpielerId { get; set; }
    public int RolleId { get; set; }
}

public class OptionWaehlenRequest
{
    public int KarteId { get; set; }
    public int OptionId { get; set; }
    public int SpielerId { get; set; }
}