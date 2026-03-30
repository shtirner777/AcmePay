# Solution Design Notes

This solution models `Payment` as the aggregate that owns authorization, capture, void, and refund transitions. I chose that boundary because the assessment’s pain points are not generic CRUD problems — they come from stateful money movement. The aggregate keeps the monetary invariants in one place, while the application layer coordinates validation, idempotency, persistence, audit logging, and the external card-network call.

## Idempotency and duplicate processing

The main defense against double processing is **persisted idempotency per merchant + operation + idempotency key**, combined with a **request hash**. This protects the system from the concrete failure mode in the brief: merchants retry the same request after timeouts or transient errors.

If the same key is replayed with the same payload, the API returns the cached response. If the same key is replayed with a different payload, the API returns a conflict rather than guessing which request is correct. The idempotency record is written in the same transaction as the payment state change, so a failed operation does not leave a permanently “claimed” key behind. That behavior is important for retry safety and is covered by the PostgreSQL-backed rollback tests.

I chose this approach because it is simple, explicit, and easy to defend. The API contract becomes predictable: every mutating request must carry an `Idempotency-Key`, and the server treats that key as part of the business contract instead of as a best-effort cache hint.

There is still one boundary to acknowledge: `AuthorizePayment` makes a synchronous external gateway call. In a real system, preventing every duplicate external side effect would require stronger upstream idempotency support, a durable pending-request workflow, or reconciliation/compensation. For this assessment, the local guarantee is strong and the limitation is explicit.

## Audit-log consistency

The audit trail is derived from domain events raised by the aggregate after successful state changes. The handler persists both the payment row and the corresponding audit rows inside the **same database transaction**. That directly addresses the “fails halfway through” production pain point: the system cannot commit a new payment state without its matching audit records, and it cannot commit an audit record that describes a state change that never committed.

I deliberately kept audit persistence synchronous and transaction-bound for this exercise. An outbox or asynchronous event pipeline may be appropriate later, but consistency matters more than distribution in the current scope. PostgreSQL-backed tests cover both the success path and rollback behavior when audit persistence fails.

## Concurrent capture and update safety

For capture, void, and refund flows, the repository loads the aggregate using `SELECT ... FOR UPDATE`. That serializes competing updates to the same payment row. Once the lock is acquired, the aggregate evaluates the monetary invariants again against the latest committed state. The combination of row-level locking and domain rules prevents the failure described in the brief, where two merchant servers capture the same authorization concurrently.

This is intentionally not a clever solution. It is a reliable one. The code favors explicit transaction boundaries and straightforward locking over optimistic complexity. The real PostgreSQL concurrency tests demonstrate that one concurrent capture or refund succeeds while the competing request fails cleanly, leaving the stored state and audit trail consistent.

## What I would change at 10x load

At significantly higher load, I would keep the domain model but evolve the operational design. I would add a formal migrations toolchain, an outbox for reliable event publication, stronger observability, and clearer reconciliation around external gateway side effects. I would also re-evaluate transaction length on the authorize path and the storage strategy for idempotency records on hot merchants with heavy retry traffic. The current design is intentionally compact for the assessment, but it is structured so those changes can be introduced without rewriting the aggregate rules.
