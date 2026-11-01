# Quickstart: Approval limits by role

## Prerequisites

- .NET 8 SDK
- Node.js for the end-to-end test
- Stryker 4.13.0 if you want the mutation score: `dotnet tool install -g dotnet-stryker --version 4.13.0`

## Run the app

```text
dotnet run --project backend/src/InvoiceApp.Api
```

Open http://localhost:5000. Demo password for every account: `ChangeMe123!`

- `demo.manager` cannot approve 50,001. The approval screen shows a reason and the invoice stays pending.
- `demo.director` can approve 500,000 alone.
- An invoice of 500,000.01 stays pending after `demo.director` approves once, and becomes approved only after `demo.director2` approves.

## Automated checks

From the repository root:

```text
dotnet test backend/InvoiceApp.sln
```

End to end, from `e2e`:

```text
npm install
npx playwright install chromium
npm test
```

Mutation score, from `backend`:

```text
dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj
```

## What a passing run shows

- Unit tests cover both ceilings, both negative rules, and the unchanged invoice.
- The Playwright test signs in as `demo.manager`, creates an invoice for 50,001, submits it, approves it, sees the reason, and still sees it pending.
