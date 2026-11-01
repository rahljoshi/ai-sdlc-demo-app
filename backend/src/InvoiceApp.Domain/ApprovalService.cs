using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// One invoice at a time. A manager or director may approve within a ceiling.
/// Amounts above that ceiling are refused and the invoice is left unchanged.
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

        var limit = CeilingFor(role);
        if (invoice.Amount > limit)
        {
            throw new ArgumentException(
                $"{role} can approve invoices up to {Money(limit)}. This invoice is {Money(invoice.Amount)} and was not changed.");
        }

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
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

    private static decimal CeilingFor(string role) => role switch
    {
        "Manager" => ManagerLimit,
        "Director" => DirectorLimit,
        _ => throw new ArgumentException($"Role {role} cannot approve invoices. Nothing was changed.")
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
