using Aynesil.Application.Features.Finance.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Taslak faturaya indirim: tutarlar, tavan ve durum kuralları.</summary>
public class DraftInvoiceDiscountTests
{
    private static readonly DateOnly Today = new(2026, 11, 15);

    private static async Task<(TenantTestContext ctx, Invoice invoice)> DraftWithLine(decimal price)
    {
        var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantA, Today);
        invoice.Lines.Add(InvoiceLine.Create(TenantA, invoice.Id, "Paket", price));
        invoice.RecalculateTotals();
        await ctx.SeedAsync(invoice);
        return (ctx, invoice);
    }

    [Fact]
    public async Task Percentage_discount_reduces_the_grand_total()
    {
        var (ctx, invoice) = await DraftWithLine(1000m);
        using var _ = ctx;

        var dto = await ctx.Handler<ApplyDiscountCommandHandler>().Handle(
            new ApplyDiscountCommand(10m, true, invoice.Id, Reason: "Kardeş indirimi"), default);

        dto.Value.Should().Be(10m);
        var saved = await ctx.Db.Invoices.SingleAsync();
        saved.DiscountTotal.Should().Be(100m);
        saved.GrandTotal.Should().Be(900m);
    }

    [Fact]
    public async Task Fixed_and_percentage_discounts_stack()
    {
        var (ctx, invoice) = await DraftWithLine(1000m);
        using var _ = ctx;
        var handler = ctx.Handler<ApplyDiscountCommandHandler>();

        await handler.Handle(new ApplyDiscountCommand(10m, true, invoice.Id), default);
        await handler.Handle(new ApplyDiscountCommand(50m, false, invoice.Id), default);

        var saved = await ctx.Db.Invoices.SingleAsync();
        saved.DiscountTotal.Should().Be(150m);
        saved.GrandTotal.Should().Be(850m);
    }

    [Fact]
    public async Task A_discount_that_would_exceed_the_subtotal_is_rejected_and_nothing_is_saved()
    {
        var (ctx, invoice) = await DraftWithLine(100m);
        using var _ = ctx;
        var handler = ctx.Handler<ApplyDiscountCommandHandler>();
        await handler.Handle(new ApplyDiscountCommand(60m, false, invoice.Id), default);

        var act = () => ctx.Handler<ApplyDiscountCommandHandler>().Handle(
            new ApplyDiscountCommand(50m, false, invoice.Id), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ctx.Db.ChangeTracker.Clear(); // istek bitti: kaydedilmemiş değişiklikler atılır, veritabanındaki hâl okunur
        (await ctx.Db.Discounts.CountAsync()).Should().Be(1);
        (await ctx.Db.Invoices.SingleAsync()).GrandTotal.Should().Be(40m);
    }

    [Fact]
    public async Task Fixed_discount_on_an_invoice_without_lines_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantA, Today);
        await ctx.SeedAsync(invoice);

        var act = () => ctx.Handler<ApplyDiscountCommandHandler>().Handle(
            new ApplyDiscountCommand(10m, false, invoice.Id), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Only_draft_invoices_accept_discounts()
    {
        var (ctx, invoice) = await DraftWithLine(1000m);
        using var _ = ctx;
        var tracked = await ctx.Db.Invoices.SingleAsync();
        tracked.Issue();
        await ctx.Db.SaveChangesAsync();

        var act = () => ctx.Handler<ApplyDiscountCommandHandler>().Handle(
            new ApplyDiscountCommand(10m, true, invoice.Id), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public void Validator_rejects_percentage_above_100_and_missing_target()
    {
        var v = new ApplyDiscountCommandValidator();
        v.Validate(new ApplyDiscountCommand(101m, true, Guid.CreateVersion7())).IsValid.Should().BeFalse();
        v.Validate(new ApplyDiscountCommand(100m, true, Guid.CreateVersion7())).IsValid.Should().BeTrue();
        v.Validate(new ApplyDiscountCommand(10m, true)).IsValid.Should().BeFalse();
        v.Validate(new ApplyDiscountCommand(0m, true, Guid.CreateVersion7())).IsValid.Should().BeFalse();
    }
}
