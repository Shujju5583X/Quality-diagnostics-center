# Lab Management System — Rework Implementation Plan
### Agentic Coder Handoff Document (v1.1 — Clarifications Applied)

---

## 1. Project Overview

This document defines the full rework of the Laboratory Management System. The rework covers UI/UX restructuring, module-level feature additions, database schema updates, and keyboard-driven workflow improvements. The application is a desktop-class web app used by lab operators to manage patients, tests, work queues, reports, and billing.

**Confirmed Decisions:**
- DB migrations: inspect and work with the existing project schema; do not assume table names
- Letterhead: already configured in the project; reuse existing implementation
- Doctor commission: flat amount (REAL, not a percentage)
- Multi-order per patient: supported — a patient can have multiple open orders simultaneously
- Backup export: CSV file download (not a raw DB file)
- Tax: configurable per invoice (entered as a value at billing time, not a fixed system-wide %)

---

## 2. Database Schema Changes

> **Note to agentic coder:** Inspect the existing schema first. Use `IF NOT EXISTS` and `IF EXISTS` guards on all migrations. Do not drop or rename any column without confirming it does not break existing functionality. The changes below are additive where possible.

### 2.1 — `patients` table (modify)
```sql
-- Replace date_of_birth with integer age
-- Only run if date_of_birth column exists
ALTER TABLE patients DROP COLUMN date_of_birth;
ALTER TABLE patients ADD COLUMN age INTEGER NOT NULL DEFAULT 0;

-- Enforce gender values (run only if constraint does not exist)
-- SQLite: recreate the table with the constraint if ALTER doesn't support CHECK addition
```

### 2.2 — `doctors` table (create if not exists)
```sql
CREATE TABLE IF NOT EXISTS doctors (
  id          INTEGER PRIMARY KEY AUTOINCREMENT,
  name        TEXT NOT NULL,
  phone       TEXT NOT NULL,
  commission  REAL NOT NULL DEFAULT 0.0,  -- flat amount, not a percentage
  created_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
  updated_at  DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### 2.3 — Test Catalog tables (create if not exists)
```sql
CREATE TABLE IF NOT EXISTS departments (
  id    INTEGER PRIMARY KEY AUTOINCREMENT,
  name  TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS tests (
  id               INTEGER PRIMARY KEY AUTOINCREMENT,
  department_id    INTEGER NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
  name             TEXT NOT NULL,
  unit             TEXT,
  ref_range_male   TEXT,
  ref_range_female TEXT,
  cost             REAL NOT NULL DEFAULT 0.0
);

CREATE TABLE IF NOT EXISTS test_packages (
  id    INTEGER PRIMARY KEY AUTOINCREMENT,
  name  TEXT NOT NULL,
  cost  REAL NOT NULL DEFAULT 0.0
);

CREATE TABLE IF NOT EXISTS package_tests (
  package_id  INTEGER NOT NULL REFERENCES test_packages(id) ON DELETE CASCADE,
  test_id     INTEGER NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
  PRIMARY KEY (package_id, test_id)
);
```

### 2.4 — `orders` table (create or modify)
```sql
CREATE TABLE IF NOT EXISTS orders (
  id          INTEGER PRIMARY KEY AUTOINCREMENT,
  patient_id  INTEGER NOT NULL REFERENCES patients(id),
  doctor_id   INTEGER REFERENCES doctors(id),  -- NULL means Self-referred
  referred_by TEXT NOT NULL DEFAULT 'Self',
  status      TEXT NOT NULL DEFAULT 'Pending'
                CHECK (status IN ('Pending', 'Completed')),
  created_at  DATETIME DEFAULT CURRENT_TIMESTAMP
);
-- A patient can have multiple orders; no unique constraint on patient_id
```

### 2.5 — `order_tests` table (create if not exists)
```sql
CREATE TABLE IF NOT EXISTS order_tests (
  id        INTEGER PRIMARY KEY AUTOINCREMENT,
  order_id  INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
  test_id   INTEGER NOT NULL REFERENCES tests(id),
  result    TEXT,
  status    TEXT NOT NULL DEFAULT 'Pending'
              CHECK (status IN ('Pending', 'Completed', 'Verified'))
);
```

### 2.6 — `invoices` table (create or modify)
```sql
CREATE TABLE IF NOT EXISTS invoices (
  id              INTEGER PRIMARY KEY AUTOINCREMENT,
  order_id        INTEGER NOT NULL REFERENCES orders(id),
  patient_id      INTEGER NOT NULL REFERENCES patients(id),
  subtotal        REAL NOT NULL,
  discount        REAL NOT NULL DEFAULT 0.0,   -- flat discount amount
  tax             REAL NOT NULL DEFAULT 0.0,   -- configurable per invoice (flat amount entered at billing time)
  total           REAL NOT NULL,               -- computed: subtotal - discount + tax
  amount_paid     REAL NOT NULL DEFAULT 0.0,
  due_amount      REAL GENERATED ALWAYS AS (total - amount_paid) STORED,
  payment_method  TEXT CHECK (payment_method IN ('Cash', 'UPI', NULL)),
  status          TEXT NOT NULL DEFAULT 'Pending'
                    CHECK (status IN ('Pending', 'Paid')),
  created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);
```
> **Tax logic note:** `tax` is a flat rupee amount entered by the operator at the time of billing — not a system-wide percentage. The UI should have a plain numeric input for tax. Total = subtotal − discount + tax.

### 2.7 — `settings` table (create if not exists)
```sql
CREATE TABLE IF NOT EXISTS settings (
  key    TEXT PRIMARY KEY,
  value  TEXT
);
INSERT OR IGNORE INTO settings (key, value) VALUES
  ('operator_name', ''),
  ('operator_address', ''),
  ('operator_phone', ''),
  ('letterhead_path', ''),  -- already set up in project; preserve existing value
  ('last_backup', NULL);
```

---

## 3. Module-by-Module Implementation Plan

---

### MODULE 0 — Sidebar (Global Layout)

**Behavior:**
- Default state: collapsed — icons only, no labels visible
- On hover: expands smoothly to show icon + label side by side
- On click (pin): locks open; stays expanded until clicked again to unpin/collapse
- CSS transition: `width 0.25s ease`
- Pin state persisted to `localStorage` key `sidebar_pinned`
- Active route item is highlighted

**Nav Items (order fixed):**
1. Overview
2. Patients
3. New Order
4. Work Queue
5. Report Generation
6. Billing
7. Test Catalog
8. Doctors
9. Settings

---

### MODULE 1 — Overview

**Today's Summary Cards:**
| Card | Data Source |
|---|---|
| Patients Registered Today | `COUNT(*) FROM patients WHERE DATE(created_at) = DATE('now')` |
| Orders Placed Today | `COUNT(*) FROM orders WHERE DATE(created_at) = DATE('now')` |
| Pending Tests | `COUNT(*) FROM order_tests WHERE status = 'Pending'` |
| Completed Tests | `COUNT(*) FROM order_tests WHERE status = 'Verified'` |
| Amount Collected Today | `SUM(amount_paid) FROM invoices WHERE status='Paid' AND DATE(created_at)=DATE('now')` |
| Unpaid Invoices | `COUNT(*) FROM invoices WHERE status = 'Pending'` |

- Layout: responsive card grid
- Cards refresh on module load

---

### MODULE 2 — Patients

**Registration Form:**
| Field | Type | Validation |
|---|---|---|
| Name | Text | Required |
| Gender | Dropdown: Select Gender / Male / Female / Others | Required |
| Age | Integer input | Required, 0–120 |
| Phone Number | Text (numeric) | Required |

- On submit: `INSERT INTO patients`
- Patient list shown below with search bar
- Columns: Name | Age | Gender | Phone | Edit
- Edit opens the form pre-filled for that patient

---

### MODULE 3 — New Order

**Three-part form:**

**Part 1 — Select Patient**
- Searchable dropdown from `patients` table
- Shows: Name + Age + Phone for disambiguation
- A patient can be selected for multiple simultaneous orders

**Part 2 — Select Tests**
- Two-pane layout:
  - Left pane: list of Departments + Packages (from `departments` and `test_packages`)
  - Right pane: tests belonging to selected department or package (from `tests` / `package_tests`)
- Each test row shows: Name | Cost | Ref Range Male | Ref Range Female
- Package row shows: Package Name | Included Tests (collapsed list) | Package Total Cost
- Selecting a test or package adds it to the selection summary
- Selection summary (below or right panel): lists chosen tests/packages with individual costs
- Running total displayed prominently at the bottom of the summary

**Part 3 — Referred By**
- Dropdown populated from `doctors` table
- First option: "Self" → sets `doctor_id = NULL`, `referred_by = 'Self'`
- Selecting a doctor → sets `doctor_id = doctor.id`, `referred_by = doctor.name`

**On Submit:**
- `INSERT INTO orders` (patient_id, doctor_id, referred_by, status='Pending')
- `INSERT INTO order_tests` for each selected test (one row per test)
- `INSERT INTO invoices` (subtotal = sum of selected test/package costs, discount=0, tax=0, total=subtotal, status='Pending')

---

### MODULE 4 — Work Queue

**Two tabs:**

#### Tab A — Pending Tests
| Column | Source |
|---|---|
| Patient Name | `patients.name` |
| Test Name | `tests.name` |
| Doctor Name | `doctors.name` or "Self" |
| Status | `order_tests.status` badge (Pending / Completed / Verified) |

- Filter bar: All / Pending / Completed / Verified
- Clicking a row navigates to Result Entry for that order

#### Tab B — Result Entry

Displayed per order. Each test in the order is a row:

| Column | Behavior |
|---|---|
| Test Name | Read-only |
| Result Value | Editable text input |
| Unit | Read-only from `tests.unit` |
| Ref Range | Show Male or Female range based on `patients.gender` |

**Keyboard Navigation (implement precisely):**
- Clicking a result input focuses it
- `Enter` key → save current value to component state, move focus to **next** result input
- `↓ Arrow` → move focus to next result input
- `↑ Arrow` → move focus to previous result input
- All entered values persist in component state throughout navigation
- Navigation wraps within the current order's test list only

**Action Buttons (per order):**
- **Verify & Save** — writes all result values from state to `order_tests.result`, sets each `order_tests.status = 'Verified'`, sets `orders.status = 'Completed'`
- **Edit** — re-enables all result inputs for the order, sets `order_tests.status = 'Pending'` for rows being edited

---

### MODULE 5 — Report Generation

**Preview Panel:**
- Renders a formatted report with:
  - Patient info (name, age, gender, phone)
  - Order date
  - Referring doctor (or "Self")
  - Table: Test Name | Result | Unit | Reference Range (gender-matched)
  - Operator/lab details from `settings` table

**Letterhead Options Dropdown:**
1. Select Option *(placeholder, disabled)*
2. With Letterhead → shows letterhead block in preview
3. Without Letterhead → hides letterhead block in preview

Letterhead is toggled via CSS class on the preview container. The letterhead itself already exists in the project — do not recreate it, just connect the toggle.

**Save as PDF:**
- Two sub-options: With Letterhead | Without Letterhead
- Apply the appropriate letterhead CSS class, then trigger PDF generation
- Use existing PDF library in the project

**Print:**
- Two sub-options: With Letterhead | Without Letterhead
- Apply CSS class, trigger `window.print()` with `@media print` styles

---

### MODULE 6 — Billing

#### Tab 1 — Invoice List
| Column | Details |
|---|---|
| Patient Name | From `patients` via `invoices.patient_id` |
| Amount | `invoices.total` |
| Status | Badge: Pending (yellow) / Paid (green) |

- Clicking a row opens Tab 2 with that invoice's detail

#### Tab 2 — Invoice Detail

**Display logic:**
- Subtotal — always shown
- Discount line — shown ONLY if `discount > 0`
- Tax line — shown ONLY if `tax > 0`
- Total — always shown
- Due Amount — shown ONLY if `due_amount > 0`

**Input fields (editable before payment):**
- Discount: plain numeric input (flat rupee amount)
- Tax: plain numeric input (flat rupee amount, configurable per invoice)
- These update `total` in real time: `total = subtotal − discount + tax`

**Payment:**
- Two buttons: **Cash** | **UPI**
- Clicking either: sets `invoices.payment_method`, sets `status = 'Paid'`, sets `amount_paid = total`, enables Generate Bill button
- Only one method can be active at a time (toggle)

**Generate Bill:**
- Enabled only after payment method is selected
- Opens a bill preview modal showing: patient info, itemized tests, subtotal, discount (if any), tax (if any), total, amount paid, payment method
- Modal actions: **Save as PDF** | **Print**

---

### MODULE 7 — Test Catalog

**Layout:** Two-column

**Left column — Departments:**
- List of all departments from `departments` table
- Clicking a department filters the right column
- Add Department button (inline form or modal): name input → `INSERT INTO departments`
- Delete Department (with confirmation; cascades to tests)

**Right column — Tests in selected department:**
- Table: Name | Unit | Male Range | Female Range | Cost | Edit | Delete
- Add Test button → form: Name, Unit, Male Range, Female Range, Cost → `INSERT INTO tests`
- Edit → inline or modal form pre-filled → `UPDATE tests`
- Delete → confirmation → `DELETE FROM tests`
- Empty state: "No tests in this department yet. Add one above."

---

### MODULE 8 — Doctors

#### Tab 1 — Doctor Form
| Field | Type | Notes |
|---|---|---|
| Doctor Name | Text | Required |
| Phone Number | Text (numeric) | Required |
| Commission Fee | Number | Flat rupee amount, default 0 |

- Add button: `INSERT INTO doctors`
- When editing (from Tab 2): form pre-fills, submit runs `UPDATE doctors`

#### Tab 2 — Doctors List
| Column | Details |
|---|---|
| Name | `doctors.name` |
| Phone | `doctors.phone` |
| Commission | `doctors.commission` (shown as ₹ flat amount) |
| Actions | Edit | Delete |

- Edit → switches to Tab 1 with form pre-filled
- Delete → confirmation dialog → `DELETE FROM doctors`
- This table is the data source for the "Referred By" dropdown in New Order

---

### MODULE 9 — Settings

#### Section 1 — Operator Information
Fields: Lab/Operator Name | Address | Phone Number
- Loaded from `settings` table on mount
- Save button: `UPDATE settings SET value = ? WHERE key = ?` for each field
- These values populate the report letterhead

#### Section 2 — Data Backup
- **Create Backup button**: exports patient, order, invoice, doctor, and test data as a CSV file download
  - One CSV per table, or a combined multi-sheet export if the format supports it
  - Filename format: `lab_backup_YYYY-MM-DD.csv`
  - On success: `UPDATE settings SET value = datetime('now') WHERE key = 'last_backup'`
- **Last Backup:** displayed below the button, read from `settings` where `key = 'last_backup'`
  - If NULL: show "No backup has been created yet."

---

## 4. Agentic Coder Prompt

> **Copy and paste everything inside the code block below directly to your agentic coder:**

```
You are implementing a full rework of an existing Laboratory Management System desktop web 
application. The codebase and database already exist. Follow each step precisely and in order. 
Inspect the existing code and schema before making any changes. After completing each step, 
confirm it before moving on.

---

## GROUND RULES
- Use the existing tech stack; do not introduce new frameworks
- Inspect the live DB schema before running any migration
- Use IF NOT EXISTS / IF EXISTS guards on all schema changes
- Preserve all existing data; no destructive migrations without explicit confirmation
- Do not recreate the letterhead — it already exists; only wire up the toggle
- After each module is complete, run a quick smoke test and confirm before proceeding

---

## STEP 0 — DATABASE MIGRATIONS

Inspect the existing schema first. Then apply only the changes below that are not already present.

1. patients table:
   - Remove `date_of_birth` column if it exists; add `age INTEGER NOT NULL DEFAULT 0`
   - Enforce gender values: 'Male', 'Female', 'Others' (recreate table with CHECK if needed in SQLite)

2. Create `doctors` table if not exists:
   id, name (TEXT NOT NULL), phone (TEXT NOT NULL), commission (REAL DEFAULT 0.0 — flat amount),
   created_at, updated_at

3. Create `departments` table if not exists: id, name (TEXT UNIQUE NOT NULL)

4. Create `tests` table if not exists:
   id, department_id (FK → departments), name, unit, ref_range_male, ref_range_female, 
   cost (REAL DEFAULT 0.0)

5. Create `test_packages` table if not exists: id, name, cost (REAL)

6. Create `package_tests` join table if not exists: package_id (FK), test_id (FK), composite PK

7. Create or update `orders` table:
   id, patient_id (FK), doctor_id (FK nullable — NULL = Self), referred_by (TEXT DEFAULT 'Self'),
   status ('Pending'/'Completed'), created_at
   NOTE: A patient can have multiple open orders — no unique constraint on patient_id.

8. Create `order_tests` table if not exists:
   id, order_id (FK), test_id (FK), result (TEXT), status ('Pending'/'Completed'/'Verified')

9. Create `invoices` table if not exists:
   id, order_id (FK), patient_id (FK), subtotal (REAL), discount (REAL DEFAULT 0),
   tax (REAL DEFAULT 0 — flat amount entered per invoice, not a percentage),
   total (REAL), amount_paid (REAL DEFAULT 0),
   due_amount (GENERATED ALWAYS AS total - amount_paid STORED),
   payment_method ('Cash'/'UPI'/NULL), status ('Pending'/'Paid'), created_at
   Total formula: subtotal - discount + tax

10. Create `settings` table if not exists:
    key (TEXT PRIMARY KEY), value (TEXT)
    INSERT OR IGNORE: operator_name, operator_address, operator_phone, letterhead_path, last_backup

---

## STEP 1 — GLOBAL SIDEBAR

Replace the existing sidebar with a collapsible version:
- Default: collapsed — icons only
- Hover: expands — shows icon + label
- Click to pin: stays expanded until clicked again
- Smooth CSS width transition: 0.25s ease
- Persist pin state in localStorage key 'sidebar_pinned'
- Highlight the active route item

Nav items in this exact order:
Overview | Patients | New Order | Work Queue | Report Generation | Billing | Test Catalog | Doctors | Settings

---

## STEP 2 — OVERVIEW MODULE

Show today's summary in a card grid:
- Patients registered today
- Orders placed today  
- Pending tests (order_tests.status = 'Pending')
- Completed/Verified tests
- Total amount collected today (sum of amount_paid where status='Paid' and today's date)
- Unpaid invoices count

All data filtered by DATE('now'). Cards refresh on module load.

---

## STEP 3 — PATIENTS MODULE

Registration form fields:
- Name: text, required
- Gender: dropdown → "Select Gender" (disabled placeholder) / Male / Female / Others, required
- Age: integer input (NOT date picker), required, min 0 max 120
- Phone Number: text, numeric only, required

On submit: INSERT into patients.
Show searchable patient list below: Name | Age | Gender | Phone | Edit button
Edit pre-fills the form for UPDATE.

---

## STEP 4 — NEW ORDER MODULE

Three-part form:

Part 1 — Patient selector
Searchable dropdown from patients table. Shows Name + Age for disambiguation.
A patient may have multiple simultaneous open orders — do not restrict this.

Part 2 — Test selector (two-pane layout)
Left pane: Departments list + Packages list
Right pane: Tests within the selected department or package
Each test shows: Name | Cost | Male Ref Range | Female Ref Range
Package shows: Package Name | Included tests | Package total cost
Selected items appear in a summary panel with running cost total.

Part 3 — Referred By
Dropdown from doctors table. First option: "Self" → doctor_id = NULL.
Selecting a doctor → doctor_id = that doctor's id.

On submit:
- INSERT into orders (patient_id, doctor_id, referred_by)
- INSERT into order_tests for each selected test
- INSERT into invoices (subtotal = sum of costs, discount=0, tax=0, total=subtotal, status='Pending')

---

## STEP 5 — WORK QUEUE MODULE

Two tabs:

TAB A — Pending Tests
Columns: Patient Name | Test Name | Doctor Name (or "Self") | Status badge
Filter bar: All / Pending / Completed / Verified
Clicking a row navigates to Result Entry for that order.

TAB B — Result Entry
Per order, show a table of its tests:
Columns: Test Name | Result (editable input) | Unit | Ref Range (use patient's gender to pick Male or Female range)

KEYBOARD NAVIGATION — implement exactly as follows:
- Clicking a result input focuses it
- Enter key: save current value to state, move focus to next result input
- Down Arrow: move focus to next result input
- Up Arrow: move focus to previous result input
- All values persist in component state throughout keyboard navigation

Buttons (per order):
- "Verify & Save": write all results to order_tests.result, set status='Verified' for each, set orders.status='Completed'
- "Edit": re-enable all result inputs, set status back to 'Pending' for that order's tests

---

## STEP 6 — REPORT GENERATION MODULE

Show a formatted report preview:
- Patient info, order date, referring doctor
- Table: Test Name | Result | Unit | Reference Range (gender-matched)
- Operator details from settings table

Letterhead toggle — DO NOT recreate the letterhead, it already exists in the project.
Just wire up a CSS class toggle to show/hide it based on the dropdown selection.

Dropdown options: "Select Option" (placeholder) / "With Letterhead" / "Without Letterhead"

Save as PDF button: sub-options With / Without Letterhead → apply class → generate PDF using existing library.
Print button: sub-options With / Without Letterhead → apply class → window.print()

---

## STEP 7 — BILLING MODULE

TAB 1 — Invoice List
Columns: Patient Name | Total Amount | Status (Pending/Paid badge)
Clicking a row opens Tab 2 with that invoice.

TAB 2 — Invoice Detail
Display:
- Subtotal (always)
- Discount (show line ONLY if discount > 0)
- Tax (show line ONLY if tax > 0)
- Total (always)
- Due Amount (show ONLY if due_amount > 0)

Editable inputs before payment:
- Discount: flat rupee amount input
- Tax: flat rupee amount input (configurable per invoice — not a fixed %)
- Real-time total update: total = subtotal - discount + tax

Payment buttons: Cash | UPI (toggle — only one active at a time)
On payment selection: set payment_method, status='Paid', amount_paid=total → enable Generate Bill button

Generate Bill button → modal preview of the bill
Modal shows: patient info, itemized tests, subtotal, discount (if >0), tax (if >0), total, payment method
Modal buttons: Save as PDF | Print

---

## STEP 8 — TEST CATALOG MODULE

Two-column layout:
Left: Department list + Add Department button
Right: Tests in the selected department

Department actions: Add (name input) | Delete (with cascade confirmation)
Test table columns: Name | Unit | Male Range | Female Range | Cost | Edit | Delete
Add Test form: Name, Unit, Male Range, Female Range, Cost
Edit: pre-fill form → UPDATE tests
Delete: confirmation → DELETE

---

## STEP 9 — DOCTORS MODULE

TAB 1 — Doctor Form
Fields: Doctor Name (required) | Phone Number (required) | Commission Fee (flat ₹ amount, default 0)
Add saves to doctors table. Edit (triggered from Tab 2) pre-fills and runs UPDATE.

TAB 2 — Doctors List
Columns: Name | Phone | Commission (₹) | Edit | Delete
Full CRUD. This is the data source for the Referred By dropdown in New Order.

---

## STEP 10 — SETTINGS MODULE

Section 1 — Operator Information
Fields: Lab Name | Address | Phone
Load from settings table on mount. Save button writes each field back.
These values appear in the report letterhead.

Section 2 — Backup
"Create Backup" button:
- Exports all key tables (patients, orders, order_tests, tests, departments, doctors, invoices) as CSV
- File download, named: lab_backup_YYYY-MM-DD.csv
- On success: UPDATE settings SET value=datetime('now') WHERE key='last_backup'
Show last backup timestamp below the button. If null: "No backup created yet."

---

## GENERAL REQUIREMENTS
- Validate all required fields before submission; show inline errors
- Toast/snackbar notifications for all success and error actions
- All tables handle empty state with a descriptive message (no blank tables)
- Optimized for desktop/tablet landscape — mobile not required
- Consistent design system across all modules (colors, spacing, typography)
- No hardcoded values — everything from the database
- Commission is always displayed and stored as a flat ₹ amount
- Tax is always a flat ₹ amount entered per invoice, never a system percentage
```

---

## 5. Implementation Order (Dependency-Sequenced)

| Priority | Step | Module | Why |
|---|---|---|---|
| 1 | STEP 0 | DB Migrations | Foundation for everything |
| 2 | STEP 1 | Sidebar | Navigation shell needed first |
| 3 | STEP 8 | Test Catalog | Needed by New Order & Work Queue |
| 4 | STEP 9 | Doctors | Needed by New Order |
| 5 | STEP 3 | Patients | Needed by New Order |
| 6 | STEP 4 | New Order | Core data entry workflow |
| 7 | STEP 5 | Work Queue | Depends on orders + tests |
| 8 | STEP 6 | Report Generation | Depends on verified results |
| 9 | STEP 7 | Billing | Depends on orders + invoices |
| 10 | STEP 2 | Overview | Aggregates all modules |
| 11 | STEP 10 | Settings | Standalone, lowest dependency |

---

## 6. Resolved Decisions (for reference)

| Item | Decision |
|---|---|
| Existing DB | Agentic coder inspects and handles; migrations use IF NOT EXISTS guards |
| Letterhead | Already in project; wire toggle only, do not recreate |
| Commission fee | Flat ₹ amount stored as REAL |
| Multi-order per patient | Fully supported; no restriction |
| Backup format | CSV file download, named `lab_backup_YYYY-MM-DD.csv` |
| Tax type | Flat ₹ amount, configurable per invoice at billing time |

---

*Document prepared for agentic coder handoff — Lab Management System Rework v1.1*
