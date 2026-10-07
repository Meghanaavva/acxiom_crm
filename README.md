# AcxiomCRM

A role based CRM application covering the complete customer-sales lifecycle: lead capture, qualification and conversion,
opportunities and sales pipeline, follow-ups, activities, dashboard analytics, reports, user/role administration and audit logging.

| Layer | Technology |
|-------|------------|
| Application | **ASP.NET Core 10 MVC** (C#) – Controllers, ViewModels, Razor Views, Services |
| UI | Razor views + **Bootstrap 5**, Chart.js, jQuery **unobtrusive validation** |
| REST API | ASP.NET Core API controllers (`/api/...`) with DTOs + Swagger |
| Authentication | **ASP.NET Core Identity** – password hashing, password policy, lockout, roles, secure auth cookie |
| Database | Entity Framework Core + SQLite (migrations included, no DB server needed) |
| Security | `[ValidateAntiForgeryToken]` on every MVC POST, role + record level authorization, rate limiting, audit log |
| Optional SPA | React 19 + TypeScript client (`frontend/`) that uses the same REST API |

## 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) – check with `dotnet --version`
- Git
- Node.js 20+ – only for the optional React client

## 2. Run the project

```bash
cd backend/AcxiomCRM.Api
dotnet run
```

Open **http://localhost:5050** – this is the complete AcxiomCRM application (MVC + Razor views).

On first run it creates the SQLite database (`acxiomcrm.db`) using the EF Core migrations and adds demo data.
REST API documentation (Swagger UI): http://localhost:5050/swagger

**Optional – React client** (same features, uses the REST API) – http://localhost:5173

```bash
cd frontend
npm install
npm run dev
```

To start again with fresh demo data, stop the backend, delete `backend/AcxiomCRM.Api/acxiomcrm.db` and run it again.

### Demo accounts

| Role | Email | Password | Notes |
|------|-------|----------|-------|
| Admin | admin@acxiomcrm.com | Admin@123 | Full access |
| Manager | manager@acxiomcrm.com | Manager@123 | Team: sales1, sales2 |
| Manager | manager2@acxiomcrm.com | Manager@123 | Team: sales3 |
| Sales Executive | sales1@acxiomcrm.com | Sales@123 | Own records only |
| Sales Executive | sales2@acxiomcrm.com | Sales@123 | Own records only |
| Sales Executive | sales3@acxiomcrm.com | Sales@123 | Own records only |

New users can also register themselves – they get the Sales Executive role.

## 3. Push to GitHub

```bash
git init
git add .
git commit -m "AcxiomCRM - CRM application with ASP.NET Core and React"
git branch -M main
# create an empty repository on github.com first, then:
git remote add origin https://github.com/<your-username>/AcxiomCRM.git
git push -u origin main
```

`.gitignore` files already exclude `node_modules`, `bin`, `obj` and the database file.

## 4. Production build

```bash
cd backend/AcxiomCRM.Api
dotnet publish -c Release -o out
```

In Production the app enforces HTTPS (HSTS + redirect), shows a friendly error page and marks the cookies `Secure`.

## Project structure

```
AcxiomCRM/
├── backend/
│   ├── AcxiomCRM.slnx
│   └── AcxiomCRM.Api/
│       ├── Controllers/    MVC controllers (Razor pages of every module) + [Authorize] role checks
│       ├── Controllers/Api REST API controllers (/api/customers, /api/leads, ...)
│       ├── Views/          Razor views per module + shared layout, pagination, validation scripts
│       ├── ViewModels/     view models for the Razor views (inherit the validated request DTOs)
│       ├── ViewComponents/ header notifications
│       ├── Services/       business logic, workflows, business validation, data scope, audit
│       ├── Dtos/           API request/response objects – entities are never exposed
│       ├── Models/         EF Core entities (Identity user/role + CRM entities)
│       ├── Data/           ApplicationDbContext, migrations, demo data seeder
│       ├── Validation/     reusable DataAnnotation attributes (email, phone, length, allowed values)
│       ├── Filters/        anti-forgery validation filter for the REST API
│       ├── wwwroot/        site.css, site.js, validation.js (custom unobtrusive rules), Bootstrap, jQuery, Chart.js
│       ├── Middleware/     global exception handling
│       ├── Helpers/        constants, paging/sorting, dates, CSV export
│       └── Program.cs      Identity, cookie, anti-forgery, rate limiting, DI configuration
├── frontend/src/           optional React client of the REST API
│   ├── api/                axios client (cookie + XSRF header)
│   ├── context/            auth + toast notifications
│   ├── components/         layout, protected route, pagination, dialogs, notifications, search
│   ├── pages/              one folder per module (the "Views")
│   ├── types/              TypeScript types of the API DTOs
│   └── utils/              client-side validation, formatting, constants
└── docs/API.md             REST API documentation
```

Request flow (layered architecture from the specification):

```
Razor Views (Presentation + jQuery unobtrusive client-side validation)
  → MVC / API Controllers (Application layer: routing, [Authorize], [ValidateAntiForgeryToken], model validation)
  → Services (Domain/Business layer: rules, workflows, status transitions, data scope)
  → ApplicationDbContext (Data access: EF Core LINQ, parameterized SQL, soft delete)
  → SQLite (Database: relationships, unique indexes, check constraints)
Cross-cutting: Identity, anti-forgery, exception middleware, audit service, logging
```

## Features by module

- **Authentication (ASP.NET Core Identity)** – login, register, logout, change password.
  Only Identity password hashes are stored (PBKDF2), never plain text. Password policy: min 8 characters with
  upper case, lower case, digit and special character. Account lockout after 5 failed attempts for 15 minutes
  (configurable in `appsettings.json`). Login/logout/failed login/lockout are audited. Login and register are rate limited.
- **Authorization** – roles Admin, Manager, SalesExecutive enforced on the server with `[Authorize(Roles = ...)]`
  and at record level (data scope): Sales Executives see only their own records, Managers their team's, Admin all.
  Changing the id in the URL/API returns 403. The menu is also filtered by role. The security stamp is validated on every
  request, so deactivating a user, changing a role or resetting a password ends that user's sessions immediately.
- **Password history** – the last 5 passwords cannot be reused (change password and admin reset); stored as Identity hashes.
- **Common layout** – responsive Bootstrap layout with header, role based sidebar menu, global search, notification
  bell (overdue and today's follow-ups), user menu and footer.
- **Dashboard** – KPI cards (customers, leads, open leads, opportunities, open/won/lost, pipeline value, weighted pipeline,
  follow-ups), Chart.js charts (lead status, opportunity pipeline, monthly sales), date filters (Today / This Week /
  This Month / Custom), admin security statistics and team performance. All numbers come from the server with the user's scope.
- **Customers** – create, view, edit, delete (soft), search by name/email/phone/company, filters, sorting, pagination,
  duplicate prevention (unique email and phone), customer history (opportunities, follow-ups, activities, audit trail).
- **Leads** – capture with source/priority/status/expected value, assignment, status workflow with allowed transitions only,
  conversion of qualified leads to a customer (+ optional opportunity) in one transaction.
- **Opportunities** – pipeline stages, amount, probability, expected close date, weighted amount (Amount × Probability / 100),
  Kanban style sales pipeline board, Won/Lost outcome.
- **Follow-ups** – schedule, complete, mark missed, reschedule, cancel; today / upcoming / overdue views.
  Completing a follow-up on a New lead moves the lead to Contacted.
- **Activities** – calls, meetings, emails and tasks linked to customers or leads.
- **Users & Roles** – create/edit users, assign role and manager, activate/deactivate, unlock, reset password,
  lockout status, roles and permission matrix.
- **Audit Log** – login success/failure, lockout, unlock, logout, create, update, delete, status change, role change,
  password events, conversions and reschedules with user, timestamp, IP address, old and new values.
  Append-only: there is no update/delete API and `ApplicationDbContext` throws if an audit row is modified or deleted.
- **Reports** – customer, lead, follow-up, opportunity, pipeline (stage and owner wise), conversion, user activity and
  audit reports with filters, sorting, pagination and CSV export.

## Validation

| Rule | Client (unobtrusive / React) | Server (DataAnnotations / Service) |
|------|:-:|:-:|
| Required fields | ✔ | ✔ |
| Email format | ✔ | ✔ |
| Phone – 10 digit Indian mobile | ✔ | ✔ |
| Min / max length | ✔ | ✔ (+ column length in DB) |
| Numeric range (amount, probability, expected value) | ✔ | ✔ (+ DB check constraints) |
| Opportunity Amount > 0 for active opportunities | ✔ | ✔ |
| Probability 0 – 100 | ✔ | ✔ |
| Expected Close Date not in the past (active) | ✔ | ✔ |
| Follow-up date not earlier than today | ✔ | ✔ |
| Lead status from valid set + allowed transitions | ✔ | ✔ |
| Unique customer email and phone / duplicate customer | – | ✔ (409) |
| Foreign keys exist and are inside the user's scope | – | ✔ |

Client-side validation uses jQuery unobtrusive validation generated from the same DataAnnotations as the server
(plus small custom rules in `wwwroot/js/validation.js` for the date and amount business rules).
It can be bypassed (e.g. Postman / curl); the server still rejects the request and shows the field-wise
messages such as `Opportunity Amount must be greater than 0.`

## Security checklist

| Item | Implementation |
|------|----------------|
| Password hashing | ASP.NET Core Identity `PasswordHasher` (PBKDF2) – no custom password table |
| Password policy | `IdentityOptions.Password` (length, upper, lower, digit, special) |
| Account lockout | `IdentityOptions.Lockout` + `PasswordSignInAsync(lockoutOnFailure: true)`; admin unlock |
| Authentication cookie | Identity cookie: HttpOnly, SameSite=Strict, Secure in production, 8h sliding expiry |
| Anti-forgery | `[ValidateAntiForgeryToken]` on every MVC POST action (forms include the token automatically); REST API: `XSRF-TOKEN` cookie → `X-XSRF-TOKEN` header checked by `ValidateAntiforgeryFilter` |
| Password history | `PasswordHistory` table + `IPasswordHasher` – last 5 passwords cannot be reused |
| Role-based authorization | `[Authorize(Roles = ...)]` on controllers/actions + `ScopeService` for record ownership |
| SQL injection | EF Core LINQ only (parameterized queries), sort columns whitelisted |
| Server-side validation | DataAnnotations on DTOs + business rules in services |
| Errors | `ExceptionMiddleware` returns `{ message, errors }` – no stack traces or DB details |
| Sensitive data | Password hashes / security stamps never returned; passwords removed before audit logging |
| HTTPS | HSTS + HTTPS redirection in Production |
| Rate limiting | ASP.NET Core rate limiter on login/register |
| Secrets | Only a local SQLite file in `appsettings.json`; use environment variables / user-secrets for real DB credentials |

## API

See [docs/API.md](docs/API.md) for every endpoint, request body, response and status code, or open Swagger UI at
http://localhost:5050/swagger while the backend is running.
