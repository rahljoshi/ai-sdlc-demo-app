using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through Draft, PendingApproval, Approved, or Rejected.
/// A manager approves up to 50,000. A director approves alone up to 500,000.
/// Above 500,000, a second and different director is required.
/// A refused attempt leaves the invoice unchanged.
/// </summary>
public class ApprovalService
{
    public const decimal ManagerLimit = 50_000m;
    public const decimal DirectorLimit = 500_000m;

    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        if (role is not ("Manager" or "Director"))
        {
            throw new ArgumentException($"Role {role} cannot approve invoices. Nothing was changed.");
        }

        if (invoice.Amount > DirectorLimit)
        {
            return ApproveAsOneOfTwo(invoice, approver, role);
        }

        if (role == "Manager" && invoice.Amount > ManagerLimit)
        {
            throw new ArgumentException(
                $"Manager can approve invoices up to {Money(ManagerLimit)}. This invoice is {Money(invoice.Amount)} and was not changed.");
        }

        Complete(invoice, approver);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice ApproveAsOneOfTwo(Invoice invoice, string approver, string role)
    {
        if (invoice.Approvals.Count == 0)
        {
            invoice.Approvals.Add(Stamp(approver, role));
            _repository.Update(invoice);
            return invoice;
        }

        if (invoice.Approvals.Any(approval => string.Equals(approval.Username, approver, StringComparison.Ordinal)))
        {
            throw new ArgumentException("The same user cannot approve an invoice twice. Nothing was changed.");
        }

        if (role != "Director")
        {
            throw new ArgumentException("The second approver must be a director. Nothing was changed.");
        }

        invoice.Approvals.Add(Stamp(approver, role));
        Complete(invoice, approver);
        return invoice;
    }

    private void Complete(Invoice invoice, string approver)
    {
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
    }

    private static InvoiceApproval Stamp(string username, string role) =>
        new()
        {
            Username = username,
            Role = role,
            At = DateTime.UtcNow
        };

    private static string Money(decimal amount) =>
        amount.ToString("0.##", CultureInfo.InvariantCulture);

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
