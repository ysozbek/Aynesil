using Aynesil.Application.Features.Finance.Commands;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Y10: fatura iptalinde gerekçe zorunlu; invoice.void_reason'a yazılır ve fatura DTO'sunda döner.</summary>
public class InvoiceVoidReasonTests
{
    private static Invoice DraftInvoice() => Invoice.Create(TenantA, new DateOnly(2026, 11, 1), "TRY", null, null, null);

    [Fact]
    public async Task Void_stores_the_trimmed_reason_and_returns_it()
    {
        using var ctx = new TenantTestContext();
        var invoice = DraftInvoice();
        await ctx.SeedAsync(invoice);

        var dto = await ctx.Handler<VoidInvoiceCommandHandler>()
            .Handle(new VoidInvoiceCommand(invoice.Id, "  Hatalı tutar girildi  ", invoice.RowVersion), default);

        dto.Status.Should().Be("void");
        dto.VoidReason.Should().Be("Hatalı tutar girildi");
        (await ctx.Db.Invoices.SingleAsync()).VoidReason.Should().Be("Hatalı tutar girildi");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_rejects_a_missing_reason(string reason)
    {
        var result = new VoidInvoiceCommandValidator().Validate(new VoidInvoiceCommand(Guid.NewGuid(), reason, 1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(VoidInvoiceCommand.Reason));
    }

    [Fact]
    public void Validator_rejects_an_overlong_reason_and_accepts_a_normal_one()
    {
        var validator = new VoidInvoiceCommandValidator();
        validator.Validate(new VoidInvoiceCommand(Guid.NewGuid(), new string('x', 1001), 1)).IsValid.Should().BeFalse();
        validator.Validate(new VoidInvoiceCommand(Guid.NewGuid(), "Müşteri vazgeçti", 1)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Domain_refuses_to_void_without_a_reason()
    {
        var invoice = DraftInvoice();
        var act = () => invoice.Void(" ");
        act.Should().Throw<ArgumentException>();
        invoice.Status.Should().Be("draft");
    }
}
