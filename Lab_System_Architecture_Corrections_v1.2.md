# Lab Management System — Architecture Corrections & Implementation Plan v1.2
### Agentic Coder Handoff Document (Critical Fixes)

---

## 1. Project Overview & Directives

This document outlines critical architectural and schema corrections that must be applied to the Lab Management System rework **before** building the UI modules. These changes resolve data integrity flaws, missing financial ledgers, and broken workflow connections identified in v1.1.

**Agent Directives:**
- Apply these schema modifications sequentially.
- Use `IF NOT EXISTS` and `IF EXISTS` guards on all structural changes.
- Ensure all business logic constraints outlined below are enforced in the application state and UI components.
- Treat these changes as precedence over the original v1.1 specifications.

---

## 2. Priority 1: Core Schema Data Integrity

### 2.1 — Revert to Dynamic Age Calculation (`patients` table)
Storing static age is an anti-pattern. The system must retain date of birth to calculate current age dynamically.

```sql
-- Revert previous instruction to drop date_of_birth.
-- Ensure date_of_birth is present and age is dropped if it was added.
ALTER TABLE patients ADD COLUMN date_of_birth DATE;
-- Note: SQLite does not easily support dropping columns in older versions. 
-- If required, recreate the table migrating existing data, or ignore the 'age' column in queries.

-- Application Logic Requirement:
-- Calculate age dynamically in all GET queries:
-- SELECT name, gender, cast((julianday('now') - julianday(date_of_birth)) / 365.25 as int) as age FROM patients;

```

### 2.2 — Implement Doctor Commission Ledger (`doctor_commissions` table)

To track payouts effectively, the system requires a transactional ledger.

```sql
CREATE TABLE IF NOT EXISTS doctor_commissions (
  id                INTEGER PRIMARY KEY AUTOINCREMENT,
  doctor_id         INTEGER NOT NULL REFERENCES doctors(id),
  invoice_id        INTEGER NOT NULL REFERENCES invoices(id),
  commission_amount REAL NOT NULL,
  status            TEXT NOT NULL DEFAULT 'Unpaid' CHECK (status IN ('Unpaid', 'Paid')),
  created_at        DATETIME DEFAULT CURRENT_TIMESTAMP
);

```

**Trigger Logic:**

* Listen for `UPDATE` on `invoices.status`.
* When an invoice status changes to `'Paid'` (or when a fully paid invoice is created), `INSERT` a row into `doctor_commissions` using the `doctor_id` from the order and the current `commission` amount from the `doctors` table.

### 2.3 — Preserve Test Package Pricing (`order_tests` table)

To prevent pricing anomalies when test packages are unrolled into individual tests, the system must link them back to their package source and maintain a specific billed cost.

```sql
ALTER TABLE order_tests ADD COLUMN package_id INTEGER REFERENCES test_packages(id);
ALTER TABLE order_tests ADD COLUMN billed_cost REAL NOT NULL DEFAULT 0.0;

```

**Application Logic Requirement:**

* **Individual Test:** Set `billed_cost` = `tests.cost`.
* **Package Tests:** Distribute the total `test_packages.cost` across the unrolled tests (e.g., assign the full package price to the first test and `0.0` to the rest, or divide equally) to ensure the invoice subtotal accurately reflects the discounted package price.

---

## 3. Priority 2: Business Logic & Module Workflow

### 3.1 — Fix Billing State Machine (`invoices` table)

The system must support partial payments to utilize the `due_amount` field accurately.

```sql
-- Update the status constraint to allow partial payments.
-- For SQLite, this may require recreating the table or handling at the application layer.
-- Enforce: CHECK (status IN ('Pending', 'Partial', 'Paid'))

```

**UI & Component Logic (Module 6 - Billing):**

1. Introduce a **"Tendered Amount"** numeric input field.
2. When the operator clicks 'Cash' or 'UPI':
* Process the entered `Tendered Amount`.
* Calculate: `amount_paid` = `amount_paid` + `Tendered Amount`.
* If `amount_paid < total`: Set `status = 'Partial'`.
* If `amount_paid >= total`: Set `status = 'Paid'`.



### 3.2 — Bridge Report Generation (Module 4 & 5 Integration)

Module 5 (Report Generation) requires a trigger mechanism from the Work Queue.

**UI & Component Logic (Module 4 - Work Queue):**

1. In **Tab A (Pending Tests)**, add a "Ready to Print" state to the filter.
2. Evaluate all `order_tests` for a given `order_id`.
3. Once all tests for an order achieve `'Verified'` status, display a **"Generate Report"** action button on that order's row.
4. Clicking this button must route the user to Module 5, passing the `order_id` in the state to instantly populate the report preview.

---

## 4. Priority 3: System Resilience

### 4.1 — Complete the Backup Loop (Restore Functionality)

A backup export feature exists, but the system lacks the ability to ingest it.

**UI & Component Logic (Module 10 - Settings):**

1. Add a **"Restore from Backup"** file upload input section alongside the "Create Backup" button.
2. Accept `.csv` files matching the system's export format.
3. **Execution Safety Protocol:**
* Require explicit user confirmation ("This will overwrite current data").
* Wrap the ingestion process in a single SQL transaction (`BEGIN TRANSACTION; ... COMMIT;`).
* Clear target tables and execute bulk inserts.
* If the CSV is malformed or an error occurs, execute `ROLLBACK` to preserve the pre-restore state securely.
