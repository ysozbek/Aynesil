using Aynesil.Application.Features.Finance.Commands;
using Aynesil.Domain.Modules.Finance.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

public class FinanceCommandTenantTests
{
    private static readonly DateOnly Today = new(2026, 11, 12);

    private static Student NewStudent(Guid corp) => Student.Create(corp, "Ali", "Kaya");

    // ── No-parent creates ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoice_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateInvoiceCommandHandler>().Handle(new CreateInvoiceCommand(Today), default);
        (await ctx.Db.Invoices.SingleAsync(i => i.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreatePackageDefinition_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreatePackageDefinitionCommandHandler>().Handle(
            new CreatePackageDefinitionCommand("P10", "10 Seans", 1000m, null, null, 10m, 90), default);
        (await ctx.Db.PackageDefinitions.SingleAsync(p => p.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreatePromotion_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreatePromotionCommandHandler>().Handle(
            new CreatePromotionCommand("YAZ10", "Yaz", 10m), default);
        (await ctx.Db.Promotions.SingleAsync(p => p.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    // ── GrantScholarship ──────────────────────────────────────────────────────

    [Fact]
    public async Task GrantScholarship_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<GrantScholarshipCommandHandler>().Handle(
            new GrantScholarshipCommand(student.Id, null, 20m, null, null, null, null), default);

        (await ctx.Db.Scholarships.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task GrantScholarship_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantB);
        await ctx.SeedAsync(student);

        await ShouldRejectAsync(() => ctx.Handler<GrantScholarshipCommandHandler>().Handle(
            new GrantScholarshipCommand(student.Id, null, 20m, null, null, null, null), default));
        (await ctx.Db.Scholarships.CountAsync()).Should().Be(0);
    }

    // ── PurchaseStudentPackage ────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseStudentPackage_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<PurchaseStudentPackageCommandHandler>().Handle(
            new PurchaseStudentPackageCommand(student.Id, 10m, 1000m, null, null), default);

        (await ctx.Db.StudentPackages.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task PurchaseStudentPackage_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantB);
        await ctx.SeedAsync(student);

        await ShouldRejectAsync(() => ctx.Handler<PurchaseStudentPackageCommandHandler>().Handle(
            new PurchaseStudentPackageCommand(student.Id, 10m, 1000m, null, null), default));
        (await ctx.Db.StudentPackages.CountAsync()).Should().Be(0);
    }

    // ── RecordPayment ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordPayment_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantA, Today);
        await ctx.SeedAsync(invoice);

        await ctx.Handler<RecordPaymentCommandHandler>().Handle(
            new RecordPaymentCommand(100m, InvoiceId: invoice.Id), default);

        (await ctx.Db.Payments.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task RecordPayment_rejects_invoice_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantB, Today);
        await ctx.SeedAsync(invoice);

        await ShouldRejectAsync(() => ctx.Handler<RecordPaymentCommandHandler>().Handle(
            new RecordPaymentCommand(100m, InvoiceId: invoice.Id), default));
        (await ctx.Db.Payments.CountAsync()).Should().Be(0);
    }

    // ── ApplyDiscount ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyDiscount_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantA, Today);
        await ctx.SeedAsync(invoice);

        await ctx.Handler<ApplyDiscountCommandHandler>().Handle(
            new ApplyDiscountCommand(10m, InvoiceId: invoice.Id), default);

        (await ctx.Db.Discounts.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task ApplyDiscount_rejects_invoice_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantB, Today);
        await ctx.SeedAsync(invoice);

        await ShouldRejectAsync(() => ctx.Handler<ApplyDiscountCommandHandler>().Handle(
            new ApplyDiscountCommand(10m, InvoiceId: invoice.Id), default));
        (await ctx.Db.Discounts.CountAsync()).Should().Be(0);
    }
}
