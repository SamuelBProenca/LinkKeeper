# Web increment — implemented and pending hosting

## Delivered locally

C#/.NET 10 modular monolith, SQLite persistent repository with embedded versioned
migration, owner-scoped operations, URL/category deduplication, search, archive,
expired history and explicit seven-day renewal in the UI (API accepts 1..36500 days).
Expiration worker runs every 60 seconds; configurable ExpirationSeconds 5..3600.
Expired rows are preserved; queries also reconcile expiry if the worker was stopped.
The worker requires an always-running process; a sleeping hosting instance cannot
perform periodic work until resumed. No notification delivery is claimed.

Responsive HTML/CSS frontend served by ASP.NET Core, with private login, HttpOnly
SameSite=Strict cookie, CSRF checks and request limits. This is a single-user
personal deployment: the shared deployment password maps to the personal owner.
Repository owner isolation is tested; multi-account login/registration is not built.
Production cookies require HTTPS. No link fetching or metadata scraping.

## Validation

30 unit/integration tests passed locally, including real SQLite persistence and
8 concurrent expiration calls with exactly one row transition. Web build passed
with zero warnings/errors. Real HTTP smoke passed: login, CSRF denial, invalid
credentials, registration, duplicate retaining expiry, search, invalid URL,
archive, renewal conflict, logout and unauthorized access. JS syntax and diff
whitespace checks passed. No visual browser QA or remote deployment yet.

The initial Microsoft.Data.Sqlite 10.0.0 restore detected a vulnerable native
SQLite dependency; upgraded to 10.0.13. Restore then passed without suppressions.
https://github.com/advisories/GHSA-2m69-gcr7-jv3q

## Run and host

Set LINKKEEPER_PASSWORD to a unique secret of at least 16 characters. Never commit it.
Set LINKKEEPER_DB to an absolute path on a durable writable volume, e.g.
/data/linkkeeper.db. Container user app must own the volume directory.
Persist ASP.NET Data Protection keys on the host if sessions must survive restart.
Build with dotnet publish src/LinkKeeper.Web -c Release. Dockerfile is provided;
it has not been built in this environment. Expose port 8080 behind HTTPS and use
/health as the liveness probe. Do not use ephemeral disk for real links.
Back up the SQLite database through SQLite backup tooling while running, or stop
the process and copy the database plus WAL together. Recovery must be tested before
production use. No production backup verification has been performed.

## Still pending from full specification

Complete CLI persistence commands,
source-message idempotency, WhatsApp official adapter and confirmation retries,
OpenAPI documentation, multi-user account identity, database outage tests,
visual mobile/browser QA and an accessible HTTPS deployment.
This increment is not declared the full MVP.

## Hosting blocker

Sites hosting runs Cloudflare Workers, not ASP.NET Core. No .NET hosting connector
was connected. Railway was discovered and suggested as a compatible next route;
no resources or costs have been provisioned. GitHub branch publication previously
returned 403; no remote PR or CI approval is claimed.

Custom category policies can be supplied through the Categories configuration array
(Name, DefaultDays, Permanent, Aliases). video/estudo/ler-depois require explicit
validity unless configured. Invalid policy configuration blocks startup.
