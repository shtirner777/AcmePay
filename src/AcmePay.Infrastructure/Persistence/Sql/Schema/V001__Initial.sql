create table if not exists payments
(
    payment_id uuid primary key,
    merchant_id varchar(100) not null,
    status integer not null,

    authorized_amount numeric(18,2) not null,
    captured_amount numeric(18,2) not null,
    refunded_amount numeric(18,2) not null,
    currency char(3) not null,

    card_network integer not null,
    cardholder_name varchar(200) not null,
    masked_pan varchar(32) not null,
    card_fingerprint varchar(128) not null,
    expiry_month integer not null,
    expiry_year integer not null,

    authorization_reference varchar(100) not null,

    authorized_at_utc timestamptz not null,
    last_modified_at_utc timestamptz not null,

    constraint ck_payments_amounts_non_negative
    check (authorized_amount >= 0 and captured_amount >= 0 and refunded_amount >= 0),

    constraint ck_payments_currency_length
    check (char_length(currency) = 3),

    constraint ck_payments_expiry_month
    check (expiry_month between 1 and 12)
    );

create index if not exists ix_payments_merchant_id on payments(merchant_id);
create index if not exists ix_payments_card_fingerprint on payments(card_fingerprint);
create index if not exists ix_payments_authorized_at_utc on payments(authorized_at_utc);

create table if not exists payment_audit_log
(
    audit_id bigint generated always as identity primary key,
    aggregate_type varchar(100) not null,
    aggregate_id varchar(100) not null,
    event_type varchar(150) not null,
    triggered_by varchar(200) not null,
    correlation_id varchar(100) null,
    occurred_on_utc timestamptz not null,
    old_state varchar(100) null,
    new_state varchar(100) null,
    payload_json jsonb null,
    created_at_utc timestamptz not null default now()
    );

create index if not exists ix_payment_audit_log_aggregate
    on payment_audit_log(aggregate_type, aggregate_id);

create index if not exists ix_payment_audit_log_occurred_on_utc
    on payment_audit_log(occurred_on_utc);

create table if not exists idempotency_requests
(
    merchant_id varchar(100) not null,
    operation varchar(50) not null,
    idempotency_key varchar(200) not null,
    request_hash varchar(128) not null,
    state integer not null,
    response_json jsonb null,
    requested_at_utc timestamptz not null,
    completed_at_utc timestamptz null,

    constraint pk_idempotency_requests
    primary key (merchant_id, operation, idempotency_key)
    );

create index if not exists ix_idempotency_requests_requested_at_utc
    on idempotency_requests(requested_at_utc);