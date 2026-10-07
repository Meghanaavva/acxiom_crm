# AcxiomCRM REST API

Base URL: `http://localhost:5050/api`

Interactive documentation (Swagger UI) is available at `http://localhost:5050/swagger` when running in Development.

### Authentication and anti-forgery

Authentication uses **ASP.NET Core Identity** with a secure cookie (`AcxiomCRM.Auth`, HttpOnly, SameSite=Strict, Secure in production).

Every state-changing request (POST / PUT / PATCH / DELETE) must also send an **anti-forgery token**:

1. `GET /api/auth/csrf` – sets the `XSRF-TOKEN` cookie (readable by JavaScript).
2. Send its value in the `X-XSRF-TOKEN` header. Missing/invalid token -> **400** `Invalid or missing anti-forgery token...`
3. The token is linked to the user, so call `/api/auth/csrf` again after login and logout.

The React app (axios) does this automatically. With curl:

```bash
curl -c jar -b jar http://localhost:5050/api/auth/csrf
XSRF=$(grep XSRF-TOKEN jar | awk '{print $7}')
curl -c jar -b jar -H "X-XSRF-TOKEN: $XSRF" -H "Content-Type: application/json" \
  -d '{"email":"admin@acxiomcrm.com","password":"Admin@123"}' http://localhost:5050/api/auth/login
curl -c jar -b jar http://localhost:5050/api/auth/csrf          # new token for the logged in user
curl -b jar http://localhost:5050/api/customers
```

## Common formats

**Error response** (400 / 401 / 403 / 404 / 409 / 429 / 500)

```json
{ "message": "Please correct the highlighted fields.", "errors": [{ "field": "email", "message": "Enter a valid email address." }] }
```

`errors` is only present for validation (400) and duplicate (409) errors.

**Paged list response**

```json
{ "data": [ ... ], "pagination": { "page": 1, "pageSize": 10, "total": 42, "totalPages": 5 } }
```

Common list query params: `page` (default 1), `pageSize` (default 10, max 100), `search`, `sortBy`, `sortOrder` (`asc`|`desc`).

**Status codes used**: 200 OK, 201 Created, 204 No Content, 400 validation/business rule failed or missing anti-forgery token, 401 not logged in / bad credentials,
403 role or record not allowed / account locked or inactive, 404 not found, 409 duplicate or conflicting state, 429 rate limited.

## Data scope (row level security)

| Role | Sees records where `assignedTo` is... |
|------|----------------------------------------|
| Admin | anyone |
| Manager | the manager or a user whose `managerId` = manager |
| SalesExecutive | only himself |

Opening a record outside your scope returns **403**. Assigning a record to a user outside your scope returns **400** on field `assignedTo`.

## Constants

| Name | Values |
|------|--------|
| Roles | `Admin`, `Manager`, `SalesExecutive` |
| Customer status | `Active`, `Inactive`, `Prospect` |
| Lead status | `New`, `Contacted`, `Qualified`, `Unqualified`, `Converted`, `Lost` |
| Lead source | `Website`, `Referral`, `Social Media`, `Email Campaign`, `Cold Call`, `Event`, `Other` |
| Lead priority | `Low`, `Medium`, `High` |
| Opportunity stage | `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost` |
| Opportunity status (derived) | `Open`, `Won`, `Lost` |
| Follow-up type | `Call`, `Meeting`, `Email`, `Visit`, `Other` |
| Follow-up status | `Planned`, `Completed`, `Missed`, `Cancelled` |
| Activity type | `Call`, `Meeting`, `Email`, `Task` |
| Activity status | `Planned`, `Completed`, `Cancelled` |

Phone format: 10 digit Indian mobile number `^[6-9]\d{9}$`. Dates are `YYYY-MM-DD` strings unless stated.

Lead status transitions:

| From | Allowed to |
|------|-----------|
| New | Contacted, Unqualified, Lost |
| Contacted | Qualified, Unqualified, Lost |
| Qualified | Lost (and Converted via `/convert` only) |
| Unqualified | Contacted, Lost |
| Converted / Lost | none (final) |

---

## Auth — `/api/auth`

| Method | Endpoint | Access | Body | Response |
|--------|----------|--------|------|----------|
| GET | `/auth/csrf` | Public | – | 204, sets `XSRF-TOKEN` cookie |
| POST | `/auth/register` | Public, rate limited | `{ name, email, password, confirmPassword }` | 201 `{ message }` – creates a **SalesExecutive** |
| POST | `/auth/login` | Public, rate limited | `{ email, password }` | 200 `{ user: UserDto }` + sets the auth cookie |
| POST | `/auth/logout` | Authenticated | – | 200 `{ message }` (clears the cookie and updates the security stamp, so all sessions of the user end) |
| GET | `/auth/me` | Authenticated | – | `UserDto` |
| POST | `/auth/change-password` | Authenticated | `{ currentPassword, newPassword, confirmPassword }` | 200 `{ message }` (signed out, user must login again) |

Login rules (ASP.NET Core Identity `SignInManager` with `lockoutOnFailure: true`): wrong password -> 401 `Invalid email or password.` After `MAX_FAILED_LOGINS` (5) failures the account is locked for
`LOCKOUT_MINUTES` (15) -> 403 `Account is locked ... Try again after N minute(s).` Inactive -> 403. Successful login resets the counter.
Password policy: min 8 chars, at least one uppercase, one lowercase, one digit and one special character, max 64.

`UserDto`:
```ts
{ id, name, email, role, roleId, managerId, managerName, isActive, isLockedOut, lockoutEnd, failedLoginCount, lastLoginAt, createdAt }
```

## Users — `/api/users`

| Method | Endpoint | Access | Notes |
|--------|----------|--------|-------|
| GET | `/users` | Admin, Manager (own team only) | filters: `search` (name/email), `role`, `isActive` (`true`/`false`) – paged `UserDto` |
| GET | `/users/lookup` | All | `[{ id, name, role }]` active users the caller can assign records to (Admin: all, Manager: self+team, Sales: self) |
| GET | `/users/managers` | Admin | `[{ id, name }]` active managers (for the manager dropdown) |
| GET | `/users/:id` | Admin | `UserDto` |
| POST | `/users` | Admin | `{ name, email, password, role, managerId?, isActive }` -> 201 `UserDto` |
| PUT | `/users/:id` | Admin | `{ name, email, role, managerId?, isActive }` -> `UserDto` (role change audited as `RoleChange`) |
| PATCH | `/users/:id/status` | Admin | `{ isActive }` – admin cannot deactivate himself |
| POST | `/users/:id/reset-password` | Admin | `{ newPassword }` – policy applied, also unlocks |
| POST | `/users/:id/unlock` | Admin | clears lockout + failed count |

## Roles — `/api/roles` (Admin)

| Method | Endpoint | Response |
|--------|----------|----------|
| GET | `/roles` | `[{ id, name, description, userCount }]` |
| GET | `/roles/permissions` | `{ modules: string[], matrix: { [role]: { [module]: string } } }` – the permission matrix |

## Customers — `/api/customers`

| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/customers` | All (scoped). Filters: `search` (name, email, phone, company, code), `status`, `assignedTo`. Sort: `customerName, customerCode, email, companyName, status, createdAt` |
| GET | `/customers/:id` | All (scoped) |
| GET | `/customers/:id/history` | All (scoped) -> `{ opportunities, followUps, activities, convertedFromLead, auditLogs }` |
| POST | `/customers` | All -> 201 |
| PUT | `/customers/:id` | All (scoped) |
| DELETE | `/customers/:id` | Admin, Manager. Soft delete. 409 if customer has open opportunities |

Body: `{ customerName*, email*, phone*, companyName, address, city, state, status*, notes, assignedTo }`
Rules: name max 100, email unique, phone unique (409 duplicate), status in list. `assignedTo` defaults to the caller.

`CustomerDto`: `{ id, customerCode, customerName, email, phone, companyName, address, city, state, status, notes, assignedTo, assignedToName, createdBy, createdByName, createdAt, updatedAt }`

## Leads — `/api/leads`

| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/leads` | Scoped. Filters: `search` (name, company, email, phone, code), `status`, `source`, `priority`, `assignedTo`. Sort: `leadName, leadCode, companyName, status, priority, expectedValue, createdAt` |
| GET | `/leads/:id` | Scoped |
| POST | `/leads` | All -> 201 |
| PUT | `/leads/:id` | Scoped. If status changes it must follow the transition table. Converted/Lost leads cannot be edited (409) |
| PATCH | `/leads/:id/status` | Scoped. `{ status }` – transition validated (400 `Lead status cannot change from X to Y.`) |
| POST | `/leads/:id/convert` | Scoped. Lead must be `Qualified` (409 otherwise) |
| DELETE | `/leads/:id` | Admin, Manager. Soft delete |

Body: `{ leadName*, email, phone, companyName, source*, status (default New), priority*, expectedValue, notes, assignedTo }`
Rules: at least one of email / phone required; expectedValue 0 – 100000000; on create status may only be `New`, `Contacted` or `Qualified`.

Convert body: `{ createOpportunity: boolean, opportunityName?, amount?, probability?, expectedCloseDate? }` (opportunity fields follow opportunity rules when `createOpportunity` is true).
If a customer with the same email or phone already exists it is linked instead of creating a duplicate; otherwise lead email and phone are both required to create the customer (400).
Response: `{ lead: LeadDto, customer: CustomerDto, opportunity: OpportunityDto | null }`. Audited as `Convert`.

`LeadDto`: `{ id, leadCode, leadName, email, phone, companyName, source, status, priority, expectedValue, notes, assignedTo, assignedToName, createdBy, convertedCustomerId, convertedCustomerName, convertedAt, createdAt, updatedAt }`

## Opportunities — `/api/opportunities`

| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/opportunities` | Scoped. Filters: `search` (name), `customerId`, `stage`, `status` (`Open`/`Won`/`Lost`), `assignedTo`. Sort: `opportunityName, amount, probability, expectedCloseDate, stage, createdAt` |
| GET | `/opportunities/pipeline` | Scoped. `{ stages: [{ stage, count, amount, weightedAmount, items: OpportunityDto[] }] }` for the pipeline board |
| GET | `/opportunities/:id` | Scoped |
| POST | `/opportunities` | All -> 201 |
| PUT | `/opportunities/:id` | Scoped. Won/Lost opportunities cannot be edited (409) |
| PATCH | `/opportunities/:id/stage` | Scoped. `{ stage }` – Won sets probability 100, Lost sets 0. Closed ones cannot move (409) |
| DELETE | `/opportunities/:id` | Admin, Manager. Soft delete |

Body: `{ opportunityName*, customerId*, leadId, amount*, stage*, probability*, expectedCloseDate*, source, notes, assignedTo }`
Business rules (400 with field errors):
- `amount` cannot be negative; for active stages (Qualification/Proposal/Negotiation) it must be > 0 -> `Opportunity Amount must be greater than 0.`
- `probability` integer 0–100 -> `Probability must be between 0 and 100.`
- `expectedCloseDate` cannot be in the past for active stages -> `Expected Close Date cannot be in the past.`
- `customerId` must exist and be in the caller's scope; `leadId` (optional) must exist.

`OpportunityDto`: `{ id, opportunityName, customerId, customerName, leadId, leadName, amount, stage, probability, weightedAmount, expectedCloseDate, source, status, closedAt, notes, assignedTo, assignedToName, createdBy, createdAt, updatedAt }`

## Follow-ups — `/api/followups`

| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/followups` | Scoped. Filters: `search` (subject), `status` (or `Overdue`), `view` (`today`/`upcoming`/`overdue`), `assignedTo`, `dateFrom`, `dateTo`, `customerId`, `leadId`, `opportunityId`. Sort: `followUpDate, subject, status, createdAt` (default `followUpDate asc`) |
| GET | `/followups/:id` | Scoped |
| POST | `/followups` | All -> 201 |
| PUT | `/followups/:id` | Scoped, only while `Planned` (409 otherwise) |
| PATCH | `/followups/:id/complete` | `{ remarks? }` – only Planned/Missed. If linked lead is `New` it moves to `Contacted` |
| PATCH | `/followups/:id/missed` | only Planned |
| PATCH | `/followups/:id/cancel` | only Planned/Missed |
| POST | `/followups/:id/reschedule` | `{ followUpDate, remarks? }` – date >= today, status back to Planned. Audited as `Reschedule` |
| DELETE | `/followups/:id` | Admin, Manager |

Body: `{ subject*, followUpType*, followUpDate*, remarks, customerId, leadId, opportunityId, assignedTo }`
Rules: at least one of customer / lead / opportunity required; linked records must exist and be in scope;
`followUpDate` cannot be earlier than today -> `Follow-up date cannot be earlier than today.`

`FollowUpDto`: `{ id, subject, followUpType, followUpDate, status, isOverdue, remarks, customerId, customerName, leadId, leadName, opportunityId, opportunityName, assignedTo, assignedToName, createdBy, completedAt, createdAt, updatedAt }`

## Activities — `/api/activities`

| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/activities` | Scoped. Filters: `search` (subject), `activityType`, `status`, `assignedTo`, `dateFrom`, `dateTo`, `customerId`, `leadId`. Sort: `activityDate, subject, activityType, status, createdAt` |
| GET | `/activities/:id` | Scoped |
| POST | `/activities` | All -> 201 |
| PUT | `/activities/:id` | Scoped |
| DELETE | `/activities/:id` | Scoped (owner) / Admin / Manager |

Body: `{ activityType*, subject*, description, activityDate* (ISO datetime), status*, customerId, leadId, assignedTo }` — subject max 150, description max 1000.

`ActivityDto`: `{ id, activityType, subject, description, activityDate, status, customerId, customerName, leadId, leadName, assignedTo, assignedToName, createdBy, createdAt, updatedAt }`

## Audit logs — `/api/audit-logs`

| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/audit-logs` | Admin (all), Manager (only logs of self + team). Filters: `userId`, `entityName`, `action`, `result`, `dateFrom`, `dateTo`, `search` (details/userName). Paged, newest first |
| GET | `/audit-logs/filters` | Admin, Manager -> `{ actions: string[], entities: string[] }` (distinct values) |

There is no update or delete endpoint. The model also blocks updates and deletes (append only).

`AuditLogDto`: `{ id, userId, userName, action, entityName, recordId, oldValue, newValue, result, details, ipAddress, createdAt }`

## Dashboard — `/api/dashboard`

`GET /dashboard?range=all|today|week|month|custom&from=YYYY-MM-DD&to=YYYY-MM-DD` (default `all`). Range filters by record `createdAt`. Scoped by role.

```ts
{
  range: { from: string | null, to: string | null },
  kpis: {
    totalCustomers, totalLeads, openLeads, totalOpportunities, openOpportunities,
    wonOpportunities, lostOpportunities, totalPipelineValue, weightedPipelineValue,
    wonAmount, pendingFollowUps, overdueFollowUps
  },
  charts: {
    leadStatus: { labels: string[], data: number[] },            // New, Contacted, Qualified, Unqualified, Lost, Converted
    opportunityPipeline: { labels: string[], counts: number[], amounts: number[] }, // 5 stages
    monthlySales: { labels: string[], won: number[], lost: number[] } // last 6 months, amounts by closedAt
  },
  upcomingFollowUps: FollowUpDto[],   // next 5 planned
  recentActivities: ActivityDto[],    // latest 5
  adminStats: { totalUsers, activeUsers, lockedUsers, failedLoginsToday } | null, // Admin only
  teamPerformance: [{ userId, userName, openOpportunities, pipelineValue, wonAmount }] | null // Admin/Manager
}
```

## Reports — `/api/reports`

Tabular reports accept `page`, `pageSize`, `sortBy`, `sortOrder`, `dateFrom`, `dateTo` (on createdAt unless stated) and return the paged format.
Add `format=csv` to download all matching rows as CSV.

| Endpoint | Access | Extra filters | Row |
|----------|--------|---------------|-----|
| `/reports/customers` | All (scoped) | `status`, `assignedTo` | `{ customerCode, customerName, companyName, email, phone, status, assignedToName, createdAt }` |
| `/reports/leads` | All (scoped) | `status`, `source`, `assignedTo` | `{ leadCode, leadName, companyName, source, status, priority, expectedValue, assignedToName, converted (bool), convertedAt, createdAt }` |
| `/reports/followups` | All (scoped) | `status` (incl. `Overdue`), `assignedTo` (dates on followUpDate) | `{ subject, followUpType, followUpDate, status, isOverdue, relatedTo, assignedToName }` |
| `/reports/opportunities` | All (scoped) | `stage`, `assignedTo` | `{ opportunityName, customerName, stage, amount, probability, weightedAmount, expectedCloseDate, assignedToName, createdAt }` |
| `/reports/pipeline` | All (scoped) | – | `{ byStage: [{ stage, count, amount, weightedAmount }], byOwner: [{ ownerId, ownerName, count, amount, weightedAmount }], totals: { count, amount, weightedAmount } }` (open opportunities, not paged, csv = byOwner) |
| `/reports/conversion` | All (scoped) | – | `{ totalLeads, convertedLeads, lostLeads, conversionRate, totalOpportunities, wonOpportunities, lostOpportunities, winRate, wonAmount, bySource: [{ source, total, converted, conversionRate }] }` (not paged, csv = bySource) |
| `/reports/user-activity` | Admin, Manager (team) | – | `{ userId, userName, role, totalActions, logins, failedLogins, creates, updates, deletes, lastActivity }` |
| `/reports/audit` | Admin | `userId`, `entityName`, `action`, `result` | `AuditLogDto` |
