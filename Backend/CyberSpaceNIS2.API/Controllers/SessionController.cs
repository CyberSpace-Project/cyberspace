using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberSpaceNIS2.API.Data;
using CyberSpaceNIS2.API.DTOs;
using CyberSpaceNIS2.API.Models;

namespace CyberSpaceNIS2.API.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly AppDbContext _db;

    public SessionController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/sessions → Neue Session erstellen
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        var szenario = await _db.Szenario.FirstOrDefaultAsync(s => s.SzenarioId == request.SzenarioId);
        if (szenario == null)
            return BadRequest(new { message = "Szenario nicht gefunden." });
        if (szenario.Status != "Aktiv")
            return BadRequest(new { message = "Nur aktive Szenarien koennen gestartet werden." });

        var moderator = await _db.Benutzer.FirstOrDefaultAsync(b => b.BenutzerId == request.ModeratorId);
        if (moderator == null)
            return BadRequest(new { message = "Moderator nicht gefunden." });

        var session = new SpielSession
        {
            SzenarioId = request.SzenarioId,
            ModeratorId = request.ModeratorId,
            Status = "Offen"
        };

        _db.Session.Add(session);
        await _db.SaveChangesAsync();

        return Created($"/api/sessions/{session.SessionId}", MapToResponse(session));
    }

    // GET /api/sessions → Alle Sessions auflisten
    [HttpGet]
    public async Task<IActionResult> GetAlleSessions()
    {
        var sessions = await _db.Session.ToListAsync();
        return Ok(sessions.Select(MapToResponse));
    }

    // GET /api/sessions/aktiv → Nur aktive Sessions
    [HttpGet("aktiv")]
    public async Task<IActionResult> GetAktiveSessions()
    {
        var sessions = await _db.Session
            .Where(s => s.Status == "Offen" || s.Status == "Laufend")
            .ToListAsync();
        return Ok(sessions.Select(MapToResponse));
    }

    // GET /api/sessions/moderator → Sessions fuer Moderator-Dashboard
    [HttpGet("moderator")]
    public async Task<IActionResult> GetModeratorSessions()
    {
        var sessions = await _db.Session.ToListAsync();
        return Ok(sessions.Select(MapToResponse));
    }

    // GET /api/sessions/{id} → Einzelne Session
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        return Ok(MapToResponse(session));
    }

    // GET /api/sessions/{id}/details → Session-Details
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetSessionDetails(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        var szenario = await _db.Szenario.FirstOrDefaultAsync(s => s.SzenarioId == session.SzenarioId);

        return Ok(new
        {
            sessionId = session.SessionId,
            szenarioTitel = szenario?.Titel ?? "Unbekannt",
            schwierigkeitsGrad = szenario?.SchwierigkeitsGrad ?? "",
            status = session.Status,
            startZeit = session.StartZeit,
            endZeit = session.EndZeit,
            erstelltAm = session.ErstelltAm
        });
    }

    // POST /api/sessions/{id}/start → Spiel starten
    [HttpPost("{id}/start")]
    public async Task<IActionResult> SessionStarten(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        if (session.Status != "Offen")
            return BadRequest(new { message = "Nur offene Sessions koennen gestartet werden." });

        session.Status = "Laufend";
        session.StartZeit = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(session));
    }

    // POST /api/sessions/{id}/pause → Spiel pausieren
    [HttpPost("{id}/pause")]
    public async Task<IActionResult> SessionPausieren(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        if (session.Status != "Laufend")
            return BadRequest(new { message = "Nur laufende Sessions koennen pausiert werden." });

        session.Status = "Offen";
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(session));
    }

    // POST /api/sessions/{id}/resume → Spiel fortsetzen
    [HttpPost("{id}/resume")]
    public async Task<IActionResult> SessionFortsetzen(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        session.Status = "Laufend";
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(session));
    }

    // POST /api/sessions/{id}/end → Spiel beenden
    [HttpPost("{id}/end")]
    public async Task<IActionResult> SessionBeenden(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        if (session.Status == "Beendet")
            return BadRequest(new { message = "Session ist bereits beendet." });

        session.Status = "Beendet";
        session.EndZeit = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(session));
    }

    // PATCH /api/sessions/{id}/abbrechen → Spiel abbrechen
    [HttpPatch("{id}/abbrechen")]
    public async Task<IActionResult> SessionAbbrechen(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        if (session.Status == "Beendet")
            return BadRequest(new { message = "Beendete Sessions koennen nicht abgebrochen werden." });

        session.Status = "Abgebrochen";
        session.EndZeit = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToResponse(session));
    }

    // GET /api/sessions/{id}/ergebnis → Spielergebnis
    [HttpGet("{id}/ergebnis")]
    public async Task<IActionResult> GetErgebnis(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        if (session.Status != "Beendet")
            return BadRequest(new { message = "Ergebnis nur fuer beendete Sessions verfuegbar." });

        var szenario = await _db.Szenario.FirstOrDefaultAsync(s => s.SzenarioId == session.SzenarioId);

        return Ok(new
        {
            sessionId = session.SessionId,
            szenarioTitel = szenario?.Titel ?? "Unbekannt",
            status = session.Status,
            startZeit = session.StartZeit,
            endZeit = session.EndZeit,
            message = "Session erfolgreich abgeschlossen!"
        });
    }
    
    // GET /api/sessions/{id}/rollen → Verfuegbare Rollen fuer Session
    [HttpGet("{id}/rollen")]
    public async Task<IActionResult> GetSessionRollen(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        var rollen = await _db.Rollen.ToListAsync();
        var vergebeneRollen = await _db.SessionSpieler
            .Where(ss => ss.SessionId == id && ss.RolleId != null)
            .Select(ss => ss.RolleId)
            .ToListAsync();

        var result = rollen.Select(r => new
        {
            rolleId = r.RolleId,
            name = r.Name,
            beschreibung = r.Beschreibung,
            farbe = r.Farbe,
            farbeHex = r.FarbeHex,
            status = vergebeneRollen.Contains(r.RolleId) ? "Vergeben" : "Verfuegbar"
        });

        return Ok(result);
    }

    // POST /api/sessions/{id}/rolle-waehlen → Rolle waehlen
    [HttpPost("{id}/rolle-waehlen")]
    public async Task<IActionResult> RolleWaehlen(int id, [FromBody] RolleWaehlenRequest request)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        var rolleVergeben = await _db.SessionSpieler
            .AnyAsync(ss => ss.SessionId == id && ss.RolleId == request.RolleId);
        if (rolleVergeben)
            return BadRequest(new { message = "Diese Rolle ist bereits vergeben." });

        var spieler = await _db.SessionSpieler
            .FirstOrDefaultAsync(ss => ss.SessionId == id && ss.SpielerId == request.SpielerId);

        if (spieler == null)
        {
            spieler = new SessionSpieler
            {
                SessionId = id,
                SpielerId = request.SpielerId,
                RolleId = request.RolleId,
                Status = "Aktiv",
                BeigetretenAm = DateTime.UtcNow
            };
            _db.SessionSpieler.Add(spieler);
        }
        else
        {
            spieler.RolleId = request.RolleId;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Rolle erfolgreich gewaehlt.", rolleId = request.RolleId });
    }

    // GET /api/sessions/{id}/karte → Naechste Karte ziehen
    [HttpGet("{id}/karte")]
    public async Task<IActionResult> GetNaechsteKarte(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        if (session.Status != "Laufend")
            return BadRequest(new { message = "Session muss laufend sein." });

        var szenario = await _db.Szenario.FirstOrDefaultAsync(s => s.SzenarioId == session.SzenarioId);
        var phasen = await _db.Phase
            .Where(p => p.SzenarioId == session.SzenarioId)
            .OrderBy(p => p.Reihenfolge)
            .ToListAsync();

        var bereitsGezogen = await _db.Spielverlauf
            .Where(sv => sv.SessionId == id)
            .Select(sv => sv.KarteId)
            .ToListAsync();

        foreach (var phase in phasen)
        {
            var karte = await _db.Karte
                .Include(k => k.Optionen)
                .Where(k => k.PhaseId == phase.PhaseId && k.KartenTyp == "Aktion" && !bereitsGezogen.Contains(k.KarteId))
                .OrderBy(k => k.Reihenfolge)
                .FirstOrDefaultAsync();

            if (karte != null)
            {
                return Ok(new
                {
                    karteId = karte.KarteId,
                    kartenCode = karte.KartenCode,
                    titel = karte.Titel,
                    inhalt = karte.Inhalt,
                    kartenTyp = karte.KartenTyp,
                    punkte = karte.Punkte,
                    phase = phase.Titel,
                    phaseNummer = phase.Reihenfolge,
                    optionen = karte.Optionen.Select(o => new
                    {
                        optionId = o.OptionId,
                        text = o.Text,
                        punkte = o.Punkte
                    })
                });
            }
        }

        return Ok(new { message = "Keine weiteren Karten verfuegbar.", fertig = true });
    }

    // POST /api/sessions/{id}/option → Option waehlen und Punkte vergeben
    [HttpPost("{id}/option")]
    public async Task<IActionResult> OptionWaehlen(int id, [FromBody] OptionWaehlenRequest request)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        var karte = await _db.Karte.FirstOrDefaultAsync(k => k.KarteId == request.KarteId);
        if (karte == null)
            return BadRequest(new { message = "Karte nicht gefunden." });

        var option = await _db.Option.FirstOrDefaultAsync(o => o.OptionId == request.OptionId && o.KarteId == request.KarteId);
        if (option == null)
            return BadRequest(new { message = "Option nicht gefunden." });

        var verlauf = new Spielverlauf
        {
            SessionId = id,
            KarteId = request.KarteId,
            OptionId = request.OptionId,
            SpielerId = request.SpielerId,
            ErhaltePunkte = option.Punkte
        };
        _db.Spielverlauf.Add(verlauf);

        var spieler = await _db.SessionSpieler
            .FirstOrDefaultAsync(ss => ss.SessionId == id && ss.SpielerId == request.SpielerId);
        if (spieler != null)
            spieler.Punkte += option.Punkte;

        await _db.SaveChangesAsync();

        var reactioKarte = await _db.Karte
            .FirstOrDefaultAsync(k => k.AktioKarteId == request.KarteId && k.KartenTyp == "Reaktion");

        return Ok(new
        {
            richtig = option.IstRichtig,
            erhaltePunkte = option.Punkte,
            gesamtPunkte = spieler?.Punkte ?? 0,
            reactio = reactioKarte != null ? new
            {
                titel = reactioKarte.Titel,
                inhalt = reactioKarte.Inhalt,
                reaktionsTyp = reactioKarte.ReaktionsTyp,
                punkte = reactioKarte.Punkte
            } : null
        });
    }

    // POST /api/sessions/{id}/auswertung → Spielauswertung
    [HttpPost("{id}/auswertung")]
    public async Task<IActionResult> Auswertung(int id)
    {
        var session = await _db.Session.FirstOrDefaultAsync(s => s.SessionId == id);
        if (session == null)
            return NotFound(new { message = "Session nicht gefunden." });

        var szenario = await _db.Szenario.FirstOrDefaultAsync(s => s.SzenarioId == session.SzenarioId);
        var spieler = await _db.SessionSpieler.Where(ss => ss.SessionId == id).ToListAsync();
        var gesamtPunkte = spieler.Sum(s => s.Punkte);
        var minPunkte = szenario?.MinPunkte ?? 0;

        var gewonnen = gesamtPunkte >= minPunkte;

        session.Status = "Beendet";
        session.EndZeit = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            sessionId = id,
            szenarioTitel = szenario?.Titel ?? "Unbekannt",
            gesamtPunkte,
            minPunkte,
            gewonnen,
            message = gewonnen ? "Glueckwunsch! Szenario erfolgreich abgeschlossen!" : "Leider nicht genug Punkte. Versuche es erneut!",
            spieler = spieler.Select(s => new { s.SpielerId, s.Punkte, s.RolleId })
        });
    }
    
    
    
    
    // Helper
    private static SessionResponse MapToResponse(SpielSession s)
    {
        return new SessionResponse
        {
            SessionId = s.SessionId,
            SzenarioId = s.SzenarioId,
            ModeratorId = s.ModeratorId,
            Status = s.Status,
            StartZeit = s.StartZeit,
            EndZeit = s.EndZeit,
            ErstelltAm = s.ErstelltAm
        };
    }
    
    
}