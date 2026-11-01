import { expect, type Page } from '@playwright/test';

export class InvoiceAppPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/');
  }

  async signIn(username: string, password: string) {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByRole('button', { name: 'Sign out' })).toBeVisible();
  }

  async createInvoice(customer: string, amount: string) {
    await this.page.getByLabel('Customer name').fill(customer);
    await this.page.getByLabel('Amount').fill(amount);
    await this.page.getByRole('button', { name: 'Create invoice' }).click();
    await expect(this.page.getByRole('row', { name: new RegExp(customer) })).toBeVisible();
  }

  async submitInvoice(customer: string) {
    const row = this.page.getByRole('row', { name: new RegExp(customer) });
    await row.getByRole('button', { name: /Submit invoice/ }).click();
    await expect(row).toContainText('Pending Approval');
  }

  async openApprovals() {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async openInvoices() {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  async approvePending(customer: string) {
    const item = this.page.getByRole('listitem').filter({ hasText: customer });
    await item.getByRole('button', { name: /Approve invoice/ }).click();
  }
}
