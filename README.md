# AcmePay Payment Processing API

A small payment processing API built for the **ACMEPAY Back-End Developer Assessment**.

The solution focuses on the payment transaction core and includes end-to-end flows for **AuthorizePayment**, **CapturePayment**, **VoidPayment**, and **RefundPayment**. The intent is not to simulate a full payment platform, but to demonstrate deliberate design around **idempotency**, **audit consistency**, **concurrency-safe updates**, and **clear operational trade-offs**.

## Scope

### Implemented
- layered API / Application / Core / Infrastructure split
- `Payment` aggregate with domain rules and explicit state transitions
- value objects and domain events for payment operations
- working vertical slices for **AuthorizePayment**, **CapturePayment**, **VoidPayment**, and **RefundPayment**:
  - HTTP endpoints
  - application handlers
  - FluentValidation validators
  - raw SQL persistence with Dapper
  - PostgreSQL schema
  - structured logging
  - idempotency handling
  - audit log persistence in the same transaction as state changes
  - row-level locking for update flows (`SELECT ... FOR UPDATE`)
- health endpoint
- deterministic unit tests, API tests with in-memory doubles, and PostgreSQL-backed persistence/concurrency tests
- short design note: `docs/solution-design.md`
- API review note: `docs/api-review.md`

### Current API surface
- `POST /api/merchants/{merchantId}/payments/authorize`
- `POST /api/merchants/{merchantId}/payments/{paymentId}/capture`
- `POST /api/merchants/{merchantId}/payments/{paymentId}/void`
- `POST /api/merchants/{merchantId}/payments/{paymentId}/refund`
- `GET /health`
- `GET /openapi/v1.json`

## Architecture Overview

### Projects
- `src/AcmePay.Api` — ASP.NET Core API, endpoint mapping, middleware, request/response contracts
- `src/AcmePay.Application` — use cases, validators, abstractions, orchestration logic
- `src/AcmePay.Core` — domain model, aggregate, value objects, domain events, invariants
- `src/AcmePay.Infrastructure` — Dapper repositories, transaction management, PostgreSQL access, mock card network gateway
- `src/AcmePay.Common` — shared constants
- `tests/*` — unit tests, in-memory API tests, and PostgreSQL-backed persistence/concurrency tests
- `docs/*` — assessment notes and review write-up

### Persistence model
The database schema contains three main tables:
- `payments` — current aggregate state
- `payment_audit_log` — immutable audit trail for state changes
- `idempotency_requests` — deduplication records per merchant + operation + idempotency key

The API initializes the schema from:
`src/AcmePay.Infrastructure/Persistence/Sql/Schema/V001__Initial.sql`

## How to Run

### Prerequisites
- .NET 9 SDK
- Docker / Docker Compose

### 1. Start PostgreSQL
```bash
docker compose up -d postgres
```

This starts a local PostgreSQL instance on `localhost:5432` and initializes the schema automatically.

Default database settings:
- Database: `acmepay`
- Username: `postgres`
- Password: `postgres`

### 2. Run the API
```bash
dotnet restore
dotnet run --project src/AcmePay.Api
```

The default connection string is configured in:
`src/AcmePay.Api/appsettings.json`

### 3. Verify the service
Health check:
```bash
curl http://localhost:5000/health
```

OpenAPI document:
```bash
curl http://localhost:5000/openapi/v1.json
```

> Note: the app exposes the OpenAPI JSON document, not a full Swagger UI page.

## Running Tests

### Full suite (includes PostgreSQL-backed tests)
Start PostgreSQL first:
```bash
docker compose up -d postgres
dotnet test
```

The PostgreSQL-backed tests use the same default connection string as the app. You can override it with:
```bash
ACMEPAY_TEST_CONNECTION_STRING="Host=...;Port=...;Database=...;Username=...;Password=..."
```

### What is currently covered

#### Unit tests
- aggregate state transitions for authorize, capture, void, and refund
- invalid transitions such as `capture after void`, `void after capture`, and `refund before capture`
- partial vs full refund outcomes
- audit mapping correctness for refund after partial capture
- validator behavior that depends on the injected clock
- authorize handler behavior for deterministic timestamps and cached idempotent responses
- idempotency conflict handling in the authorize handler

#### API tests with in-memory doubles
- authorize happy path with deterministic timestamps and audit metadata
- authorize idempotency replay with the same payload
- authorize idempotency conflict with a different payload
- missing idempotency header on authorize
- declined authorization through the test gateway
- authorize → capture → refund lifecycle with deterministic timestamps
- authorize → void lifecycle with deterministic timestamps
- capture idempotency replay and payload conflict
- capture above remaining authorized amount
- void after capture
- refund above remaining refundable amount
- refund above the total remaining amount across multiple requests
- refund idempotency replay
- void idempotency replay after completion
- canonical problem-details shape for missing-header, validation, idempotency-conflict, and domain-rule scenarios
- `/health` uses the injected clock

#### PostgreSQL-backed persistence and concurrency tests
- repository rollback leaves no persisted payment row
- `SELECT ... FOR UPDATE` blocks a competing reader until the first transaction commits
- audit write failure rolls back payment, audit, and idempotency rows
- gateway timeout/failure rolls back the idempotency claim and allows safe retry with the same key
- authorize → capture → refund keeps aggregate state and audit trail aligned in the real database
- concurrent capture on the same payment results in exactly one successful capture
- concurrent refund on the same payment results in exactly one successful refund
- API-level retry after audit failure using the same idempotency key
- API-level retry after gateway failure using the same idempotency key

## Example Request

### Authorize a payment
```bash
curl -X POST "http://localhost:5000/api/merchants/merchant-123/payments/authorize" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: auth-001" \
  -H "X-Correlation-Id: corr-001" \
  -H "X-Triggered-By: merchant-api" \
  -d '{
    "amount": 100.00,
    "currency": "USD",
    "cardholderName": "John Doe",
    "pan": "4111111111111111",
    "expiryMonth": 12,
    "expiryYear": 2030,
    "cvv": "123"
  }'
```

### Capture a payment
```bash
curl -X POST "http://localhost:5000/api/merchants/merchant-123/payments/<payment-id>/capture" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: cap-001" \
  -H "X-Correlation-Id: corr-002" \
  -H "X-Triggered-By: merchant-ops" \
  -d '{
    "amount": 60.00
  }'
```

### Void a payment
```bash
curl -X POST "http://localhost:5000/api/merchants/merchant-123/payments/<payment-id>/void" \
  -H "Idempotency-Key: void-001" \
  -H "X-Correlation-Id: corr-003" \
  -H "X-Triggered-By: merchant-ops"
```

### Refund a payment
```bash
curl -X POST "http://localhost:5000/api/merchants/merchant-123/payments/<payment-id>/refund" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: refund-001" \
  -H "X-Correlation-Id: corr-004" \
  -H "X-Triggered-By: support-agent" \
  -d '{
    "amount": 25.00
  }'
```

### Notes about the mock card gateway
- PANs starting with `4` are treated as Visa
- PANs in the `51-55` range are treated as Mastercard
- PANs ending with `0000` are declined by the mock gateway
- successful authorizations return a generated authorization reference

## Idempotency

All mutating payment endpoints require the `Idempotency-Key` header.

Idempotency is stored per:
- merchant
- operation
- idempotency key

The request payload is also hashed. If the same idempotency key is reused with a **different** request payload, the API returns a conflict response instead of silently accepting it.

If the exact same request is retried after a successful operation, the cached response is returned.

## Audit Trail

Every successful payment state change produces domain events, and those events are mapped to audit log entries. The payment state and corresponding audit entries are persisted in the **same database transaction** so that the audit log cannot drift from the aggregate state due to partial writes.

## Error Handling

The API returns canonical problem-details payloads for common failure modes, including:
- validation failures
- missing required headers
- idempotency conflicts
- declined authorizations
- business/domain rule violations
- concurrency conflicts
- unexpected server errors

## What did you leave out, and why?

This solution intentionally does **not** try to implement the full payment platform.

Left out for now:
- authentication / authorization
- rate limiting / abuse protection
- database migrations toolchain beyond the initial SQL schema script
- a real card network integration
- outbox/event publishing and asynchronous messaging
- observability beyond structured logs
- production-grade reconciliation around external side effects

Why:
- The assessment explicitly prioritizes **deliberate scope** over a large unfinished system.
- I chose to keep the current design intentionally simple: synchronous request/response flows, a single database, raw SQL, and a mock gateway.
- The next investment should go into operational hardening rather than adding more layers.

## Known Limitations

- The current solution uses a **mock** card network gateway.
- `AuthorizePayment` still performs the external gateway call inside the synchronous application flow; that is acceptable for the exercise, but a production design would require stronger guarantees around external side effects, retries, and reconciliation.
- PostgreSQL-backed persistence and concurrency tests are present, but the API-level concurrency harness is still intentionally small and focused on the highest-value cases.

## Future Work

The next logical steps are:
1. expand API-level PostgreSQL concurrency coverage even further
2. formalize migrations and environment setup
3. add authentication and richer operational telemetry
4. introduce stronger production safeguards around external side effects and reconciliation
