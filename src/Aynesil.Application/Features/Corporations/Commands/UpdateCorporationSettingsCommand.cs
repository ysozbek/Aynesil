using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Corporations.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Aynesil.Application.Features.Corporations.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>
/// Replaces the corporation's free-form settings JSON blob.
/// Also allows updating locale, currency, and timezone preferences.
/// </summary>
public record UpdateCorporationSettingsCommand(
    Guid Id,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber,
    string Settings,
    int RowVersion) : IRequest<CorporationSettingsDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class UpdateCorporationSettingsCommandValidator : AbstractValidator<UpdateCorporationSettingsCommand>
{
    public UpdateCorporationSettingsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DefaultLocale).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber is not null);
        RuleFor(x => x.TaxOffice).MaximumLength(150).When(x => x.TaxOffice is not null);
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x.Settings)
            .NotNull()
            .Must(BeValidJson).WithKey(MsgKeys.Validation.JsonObjectInvalid);
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class UpdateCorporationSettingsCommandHandler
    : IRequestHandler<UpdateCorporationSettingsCommand, CorporationSettingsDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCorporationSettingsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CorporationSettingsDto> Handle(UpdateCorporationSettingsCommand req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        if (corp.RowVersion != req.RowVersion)
            throw new ValidationException([ValidationFailures.For(nameof(req.RowVersion), MsgKeys.Error.Concurrency)]);

        corp.DefaultLocale = req.DefaultLocale;
        corp.DefaultCurrency = req.DefaultCurrency;
        corp.Timezone = req.Timezone;
        corp.TaxOffice = req.TaxOffice;
        corp.TaxNumber = req.TaxNumber;
        corp.UpdateSettings(req.Settings, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);

        return corp.ToSettingsDto();
    }
}
