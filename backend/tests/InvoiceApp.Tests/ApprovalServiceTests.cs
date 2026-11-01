using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status) =>
        new() { Id = id, CustomerName = "Test School", Amount = 100m, Status = status };

    private static Invoice Pending(decimal amount) =>
        new()
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = amount,
            Status = InvoiceStatus.PendingApproval
        };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    private static void AssertUntouched(Invoice invoice)
    {
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Equal("Test School", invoice.CustomerName);
    }

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.approver", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.approver", "Manager"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.approver", "Manager"));
    }

    [Fact]
    public void Approve_ManagerAtLimit_Approves()
    {
        var invoice = Pending(ApprovalService.ManagerLimit);

        var result = ServiceFor(invoice).Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
    }

    [Fact]
    public void Approve_ManagerOneCentOverLimit_LeavesInvoiceUnchanged()
    {
        var invoice = Pending(ApprovalService.ManagerLimit + 0.01m);
        var service = ServiceFor(invoice);

        var error = Assert.Throws<ArgumentException>(() => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Contains("50000", error.Message);
        Assert.Contains("not changed", error.Message, StringComparison.OrdinalIgnoreCase);
        AssertUntouched(invoice);
        Assert.Equal(ApprovalService.ManagerLimit + 0.01m, invoice.Amount);
    }

    [Fact]
    public void Approve_DirectorAtLimit_Approves()
    {
        var invoice = Pending(ApprovalService.DirectorLimit);

        var result = ServiceFor(invoice).Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
    }

    [Fact]
    public void Approve_DirectorOneCentOverLimit_LeavesInvoiceUnchanged()
    {
        var invoice = Pending(ApprovalService.DirectorLimit + 0.01m);
        var service = ServiceFor(invoice);

        var error = Assert.Throws<ArgumentException>(() => service.Approve("INV-1", "demo.director", "Director"));

        Assert.Contains("500000", error.Message);
        Assert.Contains("not changed", error.Message, StringComparison.OrdinalIgnoreCase);
        AssertUntouched(invoice);
        Assert.Equal(ApprovalService.DirectorLimit + 0.01m, invoice.Amount);
    }

    [Theory]
    [InlineData("Approver")]
    [InlineData("Admin")]
    public void Approve_RoleWithoutACeiling_LeavesInvoiceUnchanged(string role)
    {
        var invoice = Pending(100m);
        var service = ServiceFor(invoice);

        var error = Assert.Throws<ArgumentException>(() => service.Approve("INV-1", "demo.user", role));

        Assert.Contains(role, error.Message);
        Assert.Contains("Nothing was changed", error.Message);
        AssertUntouched(invoice);
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }
}
