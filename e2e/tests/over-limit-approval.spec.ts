import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/invoice-app.page';

test('a manager cannot approve an invoice over 50000 and the invoice stays pending', async ({ page }) => {
  const app = new InvoiceAppPage(page);
  await app.open();
  await app.signIn('demo.manager', 'ChangeMe123!');
  await app.createInvoice('Over Limit School', '50001');
  await app.submitInvoice('Over Limit School');
  await app.openApprovals();
  await app.approvePending('Over Limit School');

  const alert = page.getByRole('alert').filter({ hasText: 'not changed' });
  const pending = page.getByRole('listitem').filter({ hasText: 'Over Limit School' });
  await expect(alert).toContainText('50000');
  await expect(pending).toBeVisible();
  await expect(pending.getByRole('button', { name: /Approve invoice/ })).toBeVisible();

  await app.openInvoices();
  await expect(page.getByRole('row', { name: /Over Limit School/ })).toContainText('Pending Approval');
});
