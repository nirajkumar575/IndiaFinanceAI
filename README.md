# IndiaFinanceAI

## Lead Management

The API persists customer enquiries in PostgreSQL. Lead lists and details are only available through the JWT-protected admin API. The admin dashboard stores its access token in memory and clears it when closed or signed out.

Lead priority is a transparent sales-only score:

| Signal | Points |
| --- | ---: |
| Callback requested | 20 |
| Valid mobile number | 20 |
| Required enquiry fields complete | 20 |
| Preferred contact time selected | 15 |
| Contact consent given | 15 |
| Active lead status | 10 |

High priority is 70–100, Medium is 40–69, and Low is 0–39. Lead summaries include only recorded enquiry details.

## Codespaces Setup

Set these as Codespaces environment secrets or in the terminal environment. Do not commit credentials:

- `ConnectionStrings__FinanceDatabase`: PostgreSQL connection string.
- `Jwt__SigningKey`: random signing key of at least 32 UTF-8 bytes.
- `Admin__Username`: admin login name.
- `Admin__Password`: admin password of at least 16 characters.
- `Jwt__Issuer` and `Jwt__Audience`: optional; default to `IndiaFinanceAI` and `IndiaFinanceAI.Admin`.

The API applies EF Core migrations at startup. Run the existing backend and Angular workspaces with:

```bash
dotnet run --project src/backend/IndiaFinanceAI.Api/IndiaFinanceAI.Api.csproj
npm --prefix src/frontend run start -- --host 0.0.0.0 --port 4200
```

The Angular dev server proxies `/api` requests to `http://127.0.0.1:5080`; configure `ASPNETCORE_URLS` accordingly when running the API in Codespaces.

## WhatsApp Business

WhatsApp consent is independent from general loan-enquiry consent. Both the enquiry form and AI assistant ask for separate optional WhatsApp opt-in. The admin dashboard disables WhatsApp actions without that opt-in and blocks them after an opt-out.

Preparing a `wa.me` link only records `LinkReadyNotSent`; it does not open WhatsApp or send a message. The admin must review the prefilled message and explicitly open the link. Cloud API templates also require an explicit admin action. A provider `Accepted` status is not a delivery confirmation. No campaign or promotional message is sent automatically.

Cloud API is disabled unless `Enabled`, all required credentials and at least one approved template are configured. Add these as Codespaces Secrets/process environment settings only; never commit them:

- `WhatsApp__Enabled=true`
- `WhatsApp__AccessToken`: Meta access token.
- `WhatsApp__PhoneNumberId`: WhatsApp Business phone-number ID.
- `WhatsApp__ApiVersion`: a currently supported Graph API version from Meta's developer dashboard.
- `WhatsApp__AppSecret`: Meta app secret used for webhook signature verification.
- `WhatsApp__WebhookVerifyToken`: a private random value chosen for webhook verification.
- `WhatsApp__ApprovedTemplates__0`, `WhatsApp__ApprovedTemplates__1`, ...: exact Meta-approved template names.

To configure Meta: create a Meta Developer app, add the WhatsApp product, connect an eligible business phone number, and submit the required message templates for approval. Store the resulting credentials in Codespaces Secrets. Configure the HTTPS webhook callback as `/api/webhooks/whatsapp`, subscribe to message/status events, and enter the same private verify token. The webhook validates `X-Hub-Signature-256`, records provider delivery status, and processes `STOP`, `UNSUBSCRIBE` and `CANCEL` as opt-out commands. Follow Meta's current consent, customer-service-window and approved-template rules before contacting customers.

## Production Security

- Production startup requires a non-wildcard `AllowedHosts`, `Jwt__SigningKey` (at least 32 bytes), `Admin__Username`, `Admin__Password` (at least 16 characters), and the PostgreSQL connection string. Store these only in the hosting provider's secret configuration.
- `Cors__AllowedOrigins__0` and subsequent indexed entries are optional exact HTTPS origins. Leave CORS unset when using the Vercel same-origin API rewrite. Wildcard origins and credentialed CORS are not enabled.
- `ForwardedHeaders__KnownProxies__0` must be set to each exact trusted proxy IP in Production. Production startup fails without it so TLS termination cannot be silently assumed. Forwarded client/protocol headers are ignored from untrusted peers. Do not copy arbitrary `X-Forwarded-For` values into a trusted list.
- API responses use HSTS and generic production exception handling. Rate limits are in-memory and keyed by the immediate/trusted client IP: login 5/minute, enquiry submissions 5/minute, WhatsApp admin actions 20/minute and webhook 120/minute. Configure the hosting edge/WAF as well; multi-replica deployments need a distributed limiter.
- Only authenticated Admin-role tokens can read lead details. Public enquiry creation validates all fields and rate-limits submissions. Add a managed CAPTCHA/WAF rule before broad public acquisition if spam persists.

## Database Operations

Production does not run schema migrations during API startup. Use a single controlled migration job, not every API replica. In Codespaces or CI:

1. Take a provider snapshot and a portable backup before applying schema changes.
2. Set `ConnectionStrings__FinanceDatabase` to the Neon **direct** connection string (not a transaction-pooler URL) and restore the pinned tool: `dotnet tool restore`.
3. Review pending SQL with `dotnet ef migrations script --idempotent --project src/backend/IndiaFinanceAI.Infrastructure/IndiaFinanceAI.Infrastructure.csproj --startup-project src/backend/IndiaFinanceAI.Api/IndiaFinanceAI.Api.csproj`.
4. Apply from a controlled deployment job using `dotnet ef database update --project src/backend/IndiaFinanceAI.Infrastructure/IndiaFinanceAI.Infrastructure.csproj --startup-project src/backend/IndiaFinanceAI.Api/IndiaFinanceAI.Api.csproj`.
5. Verify `/health/ready`, the applied migration list, enquiry writes and authorized lead reads before shifting traffic.

For a portable backup use `pg_dump --format=custom "$ConnectionStrings__FinanceDatabase" --file=indiafinanceai-$(date -u +%Y%m%dT%H%M%SZ).dump`; protect the dump as sensitive data and never put it in the repository. Recovery: provision a separate Neon branch/database, restore with `pg_restore`, verify counts and migration state, then promote only after a restore rehearsal. Enable provider backups/PITR and define retention before production; a backup that has not been restored in a rehearsal is unverified.

## Hosting Preparation

Provider terms and pricing were checked on 7 November 2026; re-check them at purchase because plans and quotas change.

- **Angular on Vercel:** set project root to `src/frontend`; the existing `vercel.json` builds `dist/india-finance-ai-web/browser`, applies browser security headers and rewrites `/api/*` to an API host. Replace `REPLACE_WITH_API_HOST` with the deployed HTTPS API hostname before creating a deployment. Vercel's official [Hobby plan documentation](https://vercel.com/docs/plans/hobby) says Hobby is for personal projects and restricts use to non-commercial personal use; this commercial platform therefore needs a paid plan such as Pro.
- **ASP.NET API on Azure App Service for Linux:** use the root `Dockerfile`, port 8080, a paid plan sized for the expected traffic, and a deployment slot/health probe where available. Azure's official [App Service pricing page](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/) states its Free plan is for trials/learning and does not support production workloads. Do not base production availability on the Free tier.
- **PostgreSQL on Neon:** provision a paid production project/compute tier with appropriate backup/restore retention and TLS. The official [Neon pricing page](https://neon.com/pricing), checked on 7 November 2026, listed Free at $0 with 100 projects, 100 CU-hours and 1 GB per project; Launch and Scale are usage-based, with the page showing typical examples around $15/month and $701/month respectively. Free is suitable for development/preview only for this sensitive workload; verify current SLA, retention and region terms before selecting a paid tier.

Deployment sequence: create Neon and rehearse a restore; build/push the API container to a private registry; configure Azure secrets, `AllowedHosts`, CORS origins, trusted proxy IPs and health probes; run the reviewed migration job; deploy the API; replace the Vercel API-host placeholder and deploy the frontend; then run the checks above. This repository has not been published or deployed.

## WhatsApp Business

WhatsApp consent is separate from loan-enquiry contact consent. Manual links and Cloud API templates are available only for leads with WhatsApp opt-in and without an opt-out record. Preparing a `wa.me` link does not send a message; an admin must review and open it. Cloud API templates require an explicit admin action and an approved template. An API `Accepted` result is not a delivery confirmation.

Cloud API is disabled unless every required value is supplied through Codespaces Secrets or the process environment. Never put Meta credentials in source or commit them:

- `WhatsApp__Enabled=true`
- `WhatsApp__AccessToken`: Meta access token.
- `WhatsApp__PhoneNumberId`: WhatsApp Business phone-number ID.
- `WhatsApp__ApiVersion`: a currently supported Graph API version from Meta's developer dashboard.
- `WhatsApp__AppSecret`: Meta app secret used to validate webhook signatures.
- `WhatsApp__WebhookVerifyToken`: a private random value chosen for webhook verification.
- `WhatsApp__ApprovedTemplates__0`, `WhatsApp__ApprovedTemplates__1`, ...: exact approved template names.

Meta setup: create/configure a Meta Developer app, add the WhatsApp product, connect an eligible business phone number, create and obtain approval for message templates, then store the app credentials and template names as Codespaces Secrets. Configure the HTTPS webhook callback as `/api/webhooks/whatsapp`, subscribe to message/status events, and use the same private verify token. The webhook validates `X-Hub-Signature-256`, records delivery statuses, and treats `STOP`, `UNSUBSCRIBE` or `CANCEL` as opt-out. Follow Meta's current rules for consent, the customer-service window, and approved templates; the integration does not send promotional messages automatically.