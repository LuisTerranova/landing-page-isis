using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Builds the unified clinical record timeline by merging appointment records and standalone notes.
/// </summary>
public class ProntuarioHandler(AppDbContext context) : IProntuarioHandler
{
    public async Task<PaginatedResponse<ProntuarioTimelineItemDto>> GetTimelineByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        DateTime? filterMonthYear,
        CancellationToken ct
    )
    {
        var (appointments, notes) = BuildPatientQueries(
            patientId,
            includeNotes: true,
            includeDrafts: true
        );

        return await GetPaginated(appointments, notes, page, pageSize, filterMonthYear, ct);
    }

    public async Task<PaginatedResponse<ProntuarioTimelineItemDto>> GetTimelineByCoupleId(
        int page,
        int pageSize,
        Guid coupleId,
        DateTime? filterMonthYear,
        CancellationToken ct
    )
    {
        var partners = await GetCouplePartners(coupleId, ct);
        if (partners is null)
            return new PaginatedResponse<ProntuarioTimelineItemDto>([], 0, page, pageSize);

        var (appointments, notes) = BuildCoupleQueries(
            coupleId,
            partners.Value,
            includeNotes: true,
            includeDrafts: true
        );

        return await GetPaginated(appointments, notes, page, pageSize, filterMonthYear, ct);
    }

    public async Task<IReadOnlyList<ProntuarioTimelineItemDto>> GetFullTimelineByPatientId(
        Guid patientId,
        bool includeNotes,
        bool includeDrafts,
        CancellationToken ct
    )
    {
        var (appointments, notes) = BuildPatientQueries(patientId, includeNotes, includeDrafts);
        return await GetFullTimeline(appointments, notes, ct);
    }

    public async Task<IReadOnlyList<ProntuarioTimelineItemDto>> GetFullTimelineByCoupleId(
        Guid coupleId,
        bool includeNotes,
        bool includeDrafts,
        CancellationToken ct
    )
    {
        var partners = await GetCouplePartners(coupleId, ct);
        if (partners is null)
            return [];

        var (appointments, notes) = BuildCoupleQueries(
            coupleId,
            partners.Value,
            includeNotes,
            includeDrafts
        );

        return await GetFullTimeline(appointments, notes, ct);
    }

    private (
        IQueryable<Appointment> Appointments,
        IQueryable<AdditionalNote>? Notes
    ) BuildPatientQueries(Guid patientId, bool includeNotes, bool includeDrafts)
    {
        var appointments = context
            .Appointments.AsNoTracking()
            .Where(a => a.PatientId == patientId && a.Record != null);

        if (!includeDrafts)
            appointments = appointments.Where(a => !a.Record!.IsDraft);

        IQueryable<AdditionalNote>? notes = null;

        if (includeNotes)
        {
            notes = context
                .AdditionalNotes.AsNoTracking()
                .Where(n => n.PatientId == patientId && !n.IsDeleted);
        }

        return (appointments, notes);
    }

    private (
        IQueryable<Appointment> Appointments,
        IQueryable<AdditionalNote>? Notes
    ) BuildCoupleQueries(
        Guid coupleId,
        (Guid Partner1Id, Guid Partner2Id) partners,
        bool includeNotes,
        bool includeDrafts
    )
    {
        var partner1Id = partners.Partner1Id;
        var partner2Id = partners.Partner2Id;

        // An appointment can belong either to the couple or directly to one of the partners.
        // The OR expression returns each appointment row at most once, so no duplicate ids are produced.
        var appointments = context
            .Appointments.AsNoTracking()
            .Where(a =>
                a.Record != null
                && (
                    a.CoupleId == coupleId || a.PatientId == partner1Id || a.PatientId == partner2Id
                )
            );

        if (!includeDrafts)
            appointments = appointments.Where(a => !a.Record!.IsDraft);

        IQueryable<AdditionalNote>? notes = null;

        if (includeNotes)
        {
            notes = context
                .AdditionalNotes.AsNoTracking()
                .Where(n => n.CoupleId == coupleId && !n.IsDeleted);
        }

        return (appointments, notes);
    }

    private async Task<(Guid Partner1Id, Guid Partner2Id)?> GetCouplePartners(
        Guid coupleId,
        CancellationToken ct
    )
    {
        var couple = await context
            .Couples.AsNoTracking()
            .Where(c => c.Id == coupleId)
            .Select(c => new { c.Patient1Id, c.Patient2Id })
            .FirstOrDefaultAsync(ct);

        return couple is null ? null : (couple.Patient1Id, couple.Patient2Id);
    }

    private static async Task<PaginatedResponse<ProntuarioTimelineItemDto>> GetPaginated(
        IQueryable<Appointment> appointments,
        IQueryable<AdditionalNote>? notes,
        int page,
        int pageSize,
        DateTime? filterMonthYear,
        CancellationToken ct
    )
    {
        var monthRange = GetMonthRange(filterMonthYear);

        if (monthRange.HasValue)
        {
            appointments = ApplyMonthFilter(appointments, monthRange.Value);
            notes = notes is null ? null : ApplyMonthFilter(notes, monthRange.Value);
        }

        var appointmentCount = await appointments.CountAsync(ct);
        var noteCount = notes is null ? 0 : await notes.CountAsync(ct);
        var totalItems = appointmentCount + noteCount;

        if (totalItems <= 0)
            return new PaginatedResponse<ProntuarioTimelineItemDto>([], totalItems, page, pageSize);

        // Each source contributes at most the first (page + 1) * pageSize items of the global
        // ordering, which is enough to build any page of the merged timeline without loading it all.
        var limit = (page + 1) * pageSize;

        var appointmentItems = await appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.Record!.CreatedAt)
            .Take(limit)
            .Select(a => new ProntuarioTimelineItemDto(
                a.Record!.Id,
                ProntuarioItemType.Consulta,
                a.AppointmentDate,
                a.Record!.Note ?? string.Empty,
                a.Record.CreatedAt,
                a.Record.RectifiedAt,
                a.Id,
                a.Record.IsDraft
            ))
            .ToListAsync(ct);

        var noteItems = notes is null
            ? new List<ProntuarioTimelineItemDto>()
            : await notes
                .OrderByDescending(n => n.NoteDate)
                .ThenByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new ProntuarioTimelineItemDto(
                    n.Id,
                    ProntuarioItemType.Anotacao,
                    n.NoteDate,
                    n.Content,
                    n.CreatedAt,
                    n.UpdatedAt,
                    null,
                    false
                ))
                .ToListAsync(ct);

        var items = appointmentItems
            .Concat(noteItems)
            .OrderByDescending(x => x.EventDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponse<ProntuarioTimelineItemDto>(items, totalItems, page, pageSize);
    }

    private static async Task<IReadOnlyList<ProntuarioTimelineItemDto>> GetFullTimeline(
        IQueryable<Appointment> appointments,
        IQueryable<AdditionalNote>? notes,
        CancellationToken ct
    )
    {
        var appointmentItems = await appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.Record!.CreatedAt)
            .Select(a => new ProntuarioTimelineItemDto(
                a.Record!.Id,
                ProntuarioItemType.Consulta,
                a.AppointmentDate,
                a.Record!.Note ?? string.Empty,
                a.Record.CreatedAt,
                a.Record.RectifiedAt,
                a.Id,
                a.Record.IsDraft
            ))
            .ToListAsync(ct);

        var noteItems = notes is null
            ? new List<ProntuarioTimelineItemDto>()
            : await notes
                .OrderByDescending(n => n.NoteDate)
                .ThenByDescending(n => n.CreatedAt)
                .Select(n => new ProntuarioTimelineItemDto(
                    n.Id,
                    ProntuarioItemType.Anotacao,
                    n.NoteDate,
                    n.Content,
                    n.CreatedAt,
                    n.UpdatedAt,
                    null,
                    false
                ))
                .ToListAsync(ct);

        return appointmentItems
            .Concat(noteItems)
            .OrderByDescending(x => x.EventDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();
    }

    private static (DateTimeOffset Start, DateTimeOffset End)? GetMonthRange(
        DateTime? filterMonthYear
    )
    {
        if (!filterMonthYear.HasValue)
            return null;

        // Convert the local Porto Velho month boundaries to UTC to align with PostgreSQL timestamp storage
        var localStart = new DateTime(
            filterMonthYear.Value.Year,
            filterMonthYear.Value.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Unspecified
        );

        return (
            localStart.ToPortoVelhoDateTimeOffset(),
            localStart.AddMonths(1).ToPortoVelhoDateTimeOffset()
        );
    }

    private static IQueryable<Appointment> ApplyMonthFilter(
        IQueryable<Appointment> query,
        (DateTimeOffset Start, DateTimeOffset End) range
    )
    {
        return query.Where(a => a.AppointmentDate >= range.Start && a.AppointmentDate < range.End);
    }

    private static IQueryable<AdditionalNote> ApplyMonthFilter(
        IQueryable<AdditionalNote> query,
        (DateTimeOffset Start, DateTimeOffset End) range
    )
    {
        return query.Where(n => n.NoteDate >= range.Start && n.NoteDate < range.End);
    }
}
