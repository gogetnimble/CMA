# CMA Membership Report

A .NET 8 console utility that connects to Dataverse with an **app registration
(client id + secret)**, reads `new_cmamembershipdetail`, resolves the Contact,
Category (Product) and PTMA (Account) lookups plus contact demographics, and writes
a **single self-contained, interactive HTML file** containing **four reports** the
viewer tabs between (blue colour scheme throughout):

The app pulls **every** membership-detail record — there is no year filter on the
query, so all history (right back to the oldest records) is downloaded. In the
report the Membership and Overview tabs default to **All years**; the year
dropdown (All years + every year found in the data) drills into one year.

**Status and prior years.** When a member renews, their previous year's membership
is automatically set inactive, so only the **current** membership year meaningfully
has "Active" records. The **Active** filter therefore applies **only to the current
year** (the calendar year of the data snapshot); memberships for any other year are
shown regardless of status, so historical years aren't empty. Choose **All statuses**
to apply the filter literally across every year.

The **created-by-month** chart only buckets records whose `createdon` falls inside
their membership year. For years whose records were created on incompatible dates
(migrated data, or a blank `new_membershipyear`), a monthly breakdown is
meaningless, so the report shows a **single total for that year** instead — the
records still appear in the total and in the table.

1. **Membership** — memberships for the selected year (All years by default),
   grouped by the **month** they
   were created (`createdon`) and **by PTMA** (`new_divassocaccountid` → Account
   name), with a cross-filtered detail table. Filters: year · status · PTMA · category.
2. **Overview** — an at-a-glance dashboard: Active / Practising / Retired-Lifetime /
   Resident / Student KPI tiles, a **years-to-expiry** distribution (whole years
   between today and `new_expirydate`, with Lifetime and blank buckets), and a
   **members-by-type** donut (Physician / Student / Resident / Lifetime, derived
   from the category).
3. **Demographics** — distinct contacts by **age range** (`new_age`), **gender**
   (`gendercode`) and **language** (`new_language`), with KPI tiles.
4. **Upcoming Expiry** — active memberships about to expire (`new_expirydate`):
   next 30 / 90 / 365-day counts, an expirations-by-month chart, and a soonest-first
   table with a configurable "expiring within" window.

All aggregation and filtering happen **client-side** off one embedded dataset, so
the same template can later be lifted into a Dynamics **web resource** with almost
no change (see _Path to a web resource_ below).

---

## Fields read

| Report column | Source |
|---|---|
| Contact | `new_cmamembershipdetail.new_contact` → `contact.fullname` |
| Category | `new_cmamembershipdetail.new_categoryproductid` → `product.name` |
| PTMA | `new_cmamembershipdetail.new_divassocaccountid` → `account.name` |
| Expiry | `new_expirydate` |
| Year | `new_membershipyear` |
| Status | `new_status` (option-set label via `FormattedValues`) |
| Created month | `createdon` |
| Gender | `contact.gendercode` (option-set label) |
| Age | `contact.new_age` |
| Language | `contact.new_language` (option-set label) |

The lookups and contact demographics are resolved with `LeftOuter` joins in a
single paged query, so a row with a missing lookup still appears. The Demographics
report de-duplicates by contact id (`new_contact`) so members aren't counted twice.

---

## Setup

### 1. Register an app in Entra ID and grant it Dataverse access

1. **Entra ID → App registrations → New registration.** Note the
   **Application (client) ID** and **Directory (tenant) ID**.
2. **Certificates & secrets → New client secret.** Copy the secret **value**.
3. In **Power Platform**, create an **Application User** for that app registration
   (Admin center → Environment → Settings → Users + permissions → Application users)
   and assign a security role with **read** on membership detail, Contact, Product
   and Account. Read-only is sufficient — this tool never writes.

### 2. Provide configuration

Environment variables (preferred — keeps the secret out of files):

```bash
export CMA_DATAVERSE_URL="https://yourorg.crm3.dynamics.com"
export CMA_CLIENT_ID="<application-client-id>"
export CMA_CLIENT_SECRET="<client-secret-value>"
export CMA_TENANT_ID="<tenant-id>"          # optional
export CMA_MEMBERSHIP_YEAR="2027"           # optional — defaults to latest in data
export CMA_OUTPUT_PATH="cma-membership-report.html"   # optional
```

…or copy `appsettings.sample.json` → `appsettings.json` and fill it in. Anything
not supplied is prompted for at the console (the secret is entered masked).

> `appsettings.json` and generated `*.html` reports are git-ignored — don't commit
> secrets or member data.

### 3. Run

```bash
dotnet run -c Release
# or, after: dotnet build -c Release
./bin/Release/net8.0/CMA.Utilities.MembershipReport
```

The tool connects, pages through the records, writes the HTML, and offers to open it.

---

## Resilience / disconnect handling

- Auth uses `AuthType=ClientSecret` — no interactive login, safe for scheduled runs.
- `ServiceClient` is configured with its built-in throttling retry
  (`MaxRetryCount`, `RetryPauseTime`).
- On top of that, every read runs through a retry loop with **exponential backoff
  (2s → 4s → 8s → 16s)**. If the client has dropped offline (`IsReady == false`),
  it is **rebuilt/reconnected transparently** before the next attempt. Genuine
  request errors (bad query, rejected auth) surface immediately rather than looping.

---

## Path to a web resource (next phase)

This console app is the POC. The HTML template
(`Templates/report-template.html`) reads its data from `window.__CMA_DATA__`,
which the generator injects between the `CMA_DATA_START` / `CMA_DATA_END` markers.
To run the same UI inside Dynamics:

1. Remove the injected `#cma-data` `<script>`.
2. Load the identical record shape from the caller's own session via
   `Xrm.WebApi.retrieveMultipleRecords` — a ready-to-use `loadFromDataverse()`
   using the equivalent FetchXML is included, commented, at the bottom of the
   template. No secret ships to the browser; it runs as the signed-in user.
3. Register the HTML (and it is fully self-contained bar the Google Fonts link) as
   a web resource.

---

## Files

| File | Role |
|---|---|
| `Program.cs` | Console flow: config → connect → fetch → generate. |
| `AppConfig.cs` | Config resolution (appsettings.json → env vars → prompts). |
| `DataverseConnection.cs` | Client-secret `ServiceClient`, retry + reconnect. |
| `MembershipRepository.cs` | Paged query + lookup resolution → `MembershipRecord`. |
| `MembershipRecord.cs` | Flattened row, serialized into the report. |
| `ReportGenerator.cs` | Injects the dataset into the template. |
| `Templates/report-template.html` | The interactive report (data-source agnostic). |

To add it to the utilities solution: `dotnet sln ../CMA.Utilities.sln add CMA.Utilities.MembershipReport.csproj`.
