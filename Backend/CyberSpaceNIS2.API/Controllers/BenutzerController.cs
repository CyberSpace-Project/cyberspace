using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberSpaceNIS2.API.Data;

namespace CyberSpaceNIS2.API.Controllers;

[ApiController]
[Route("api/benutzer")]
public class BenutzerController : ControllerBase
{
    private readonly AppDbContext _db;

    public BenutzerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAlleBenutzer()
    {
        var benutzer = await _db.Benutzer.Select(b => new
        {
            b.BenutzerId,
            b.Benutzername,
            b.Email,
            b.Rolle,
            b.ErstelltAm,
            b.Punkte,
            b.Abteilung
        }).ToListAsync();

        return Ok(benutzer);
    }

    [HttpPut("{id}/berechtigung")]
    public async Task<IActionResult> BerechtigungAendern(int id, [FromBody] BerechtigungRequest request)
    {
        var benutzer = await _db.Benutzer.FirstOrDefaultAsync(b => b.BenutzerId == id);
        if (benutzer == null)
            return NotFound(new { message = "Benutzer nicht gefunden." });

        var erlaubteRollen = new[] { "Spieler", "Moderator", "Admin" };
        if (!erlaubteRollen.Contains(request.NeueRolle))
            return BadRequest(new { message = "Rolle muss Spieler, Moderator oder Admin sein." });

        benutzer.Rolle = request.NeueRolle;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Berechtigung geaendert.", benutzerId = id, neueRolle = request.NeueRolle });
    }
}

public class BerechtigungRequest
{
    public string NeueRolle { get; set; } = string.Empty;
}