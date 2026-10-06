-- Development-only Sqlite schema: only the tables and columns this app reads or writes so far.
-- Column names follow the production iCarry MySQL database; types are approximations.

CREATE TABLE vendor (
    id            INTEGER PRIMARY KEY,
    name          TEXT    NOT NULL,
    is_on         INTEGER NOT NULL DEFAULT 1,
    shipping_self INTEGER NOT NULL DEFAULT 0,
    service_fee   TEXT    NULL            -- JSON: [{"name":"iCarry","percent":20}, ...]
);

CREATE TABLE vendor_account (
    id          INTEGER PRIMARY KEY,
    account     TEXT    NOT NULL,
    pwd         TEXT    NOT NULL,         -- unsalted SHA-1 hex, shared with icarry-newvendor and icarry-admin
    name        TEXT    NULL,
    vendor_id   INTEGER NULL,
    shop_id     INTEGER NULL,
    shop_admin  INTEGER NOT NULL DEFAULT 0,
    is_on       INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE category (
    id   INTEGER PRIMARY KEY,
    name TEXT NOT NULL
);

CREATE TABLE parcel_tax_code (
    hscode    TEXT PRIMARY KEY,
    name      TEXT NULL,
    unit_name TEXT NULL,
    tax       TEXT NULL                   -- percent, stored as text in production
);

CREATE TABLE product (
    id                  INTEGER PRIMARY KEY,
    vendor_id           INTEGER NOT NULL,
    category_id         INTEGER NULL,
    name                TEXT    NOT NULL,
    model_name          TEXT    NULL,
    serving_size        TEXT    NULL,
    price               NUMERIC NOT NULL DEFAULT 0,
    service_fee_percent NUMERIC NOT NULL DEFAULT 0,
    parcel_tax_code     TEXT    NULL,
    status              INTEGER NOT NULL DEFAULT -1,
    is_del              INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE product_model (
    id            INTEGER PRIMARY KEY,
    product_id    INTEGER NOT NULL,
    name          TEXT    NOT NULL,
    sku           TEXT    NULL,
    quantity      INTEGER NOT NULL DEFAULT 0,
    safe_quantity INTEGER NOT NULL DEFAULT 0,
    is_del        INTEGER NOT NULL DEFAULT 0
);
