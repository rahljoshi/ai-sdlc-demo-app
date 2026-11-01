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
        Assert.Empty(invoice.Approvals);
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
    public void Approve_AboveDirectorLimit_RecordsFirstApproverAndStaysPending()
    {
        var invoice = Pending(ApprovalService.DirectorLimit + 0.01m);

        var result = ServiceFor(invoice).Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Null(result.DecisionNote);
        var recorded = Assert.Single(result.Approvals);
        Assert.Equal("demo.manager", recorded.Username);
        Assert.Equal("Manager", recorded.Role);
    }

    [Fact]
    public void Approve_SameUserAsBothApprovers_LeavesTheFirstRecordInPlace()
    {
        var invoice = Pending(ApprovalService.DirectorLimit + 0.01m);
        var service = ServiceFor(invoice);
        service.Approve("INV-1", "demo.director", "Director");
        var firstAt = invoice.Approvals[0].At;

        var error = Assert.Throws<ArgumentException>(() => service.Approve("INV-1", "demo.director", "Director"));

        Assert.Contains("same user", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nothing was changed", error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        var recorded = Assert.Single(invoice.Approvals);
        Assert.Equal("demo.director", recorded.Username);
        Assert.Equal(firstAt, recorded.At);
    }

    [Fact]
    public void Approve_SecondApproverWhoIsNotADirector_LeavesTheFirstRecordInPlace()
    {
        var invoice = Pending(600_000m);
        var service = ServiceFor(invoice);
        service.Approve("INV-1", "demo.director", "Director");
        var firstAt = invoice.Approvals[0].At;

        var error = Assert.Throws<ArgumentException>(() => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Contains("director", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nothing was changed", error.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        var recorded = Assert.Single(invoice.Approvals);
        Assert.Equal("demo.director", recorded.Username);
        Assert.Equal(firstAt, recorded.At);
    }

    [Fact]
    public void Approve_TwoDistinctDirectors_ApprovesAboveTheDirectorLimit()
    {
        var invoice = Pending(ApprovalService.DirectorLimit + 0.01m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        var result = service.Approve("INV-1", "demo.director2", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director2", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(2, result.Approvals.Count);
        Assert.Equal("demo.director", result.Approvals[0].Username);
        Assert.Equal("demo.director2", result.Approvals[1].Username);
    }

    [Fact]
    public void Approve_ManagerThenDifferentDirector_ApprovesAboveTheDirectorLimit()
    {
        var invoice = Pending(750_000m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");
        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(new[] { "demo.manager", "demo.director" }, result.Approvals.Select(approval => approval.Username));
    }

    [Fact]
    public void Approve_SameUserIsBlockedEvenWhenAnotherApproverIsRecorded()
    {
        var invoice = Pending(600_000m);
        invoice.Approvals.Add(new InvoiceApproval { Username = "demo.director", Role = "Director", At = DateTime.UtcNow });
        invoice.Approvals.Add(new InvoiceApproval { Username = "demo.manager", Role = "Manager", At = DateTime.UtcNow });
        var service = ServiceFor(invoice);

        var error = Assert.Throws<ArgumentException>(() => service.Approve("INV-1", "demo.director", "Director"));

        Assert.Contains("same user", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Equal(2, invoice.Approvals.Count);
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
