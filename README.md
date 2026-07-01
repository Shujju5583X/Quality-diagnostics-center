# Quality Diagnostics Center - Laboratory System 🏥

> A comprehensive, enterprise-grade **Medical Laboratory Management System** designed to streamline clinical workflows, client directories, and laboratory diagnostics, built with a **.NET Framework (WPF)** desktop architecture.

This powerful WPF application handles complete day-to-day laboratory operations including patient registration, referral doctor commission mapping, active panel/test type catalog navigation, keyboard-optimized clinical result entries, dynamic demographic reference ranges, professional PDF report generation, billing adjustments, and automated multi-format backups.

---

## 📢 Recent Updates (June 2026)
*   **Test Catalog Optimization:** Streamlined the master test catalog from 84 to ~57 core parameters, removing unused tests (like Malaria and HIV screening).
*   **New Tests & Panels:** Added HbA1c, ASO Titer, Culture & Sensitivity (C/S), and a comprehensive Urine Routine/Complete group. Refined standard panels (CBC, Electrolytes, KFT, LFT, Lipid Profile).
*   **Project Cleanup:** Removed obsolete logs, outdated tracking files, old build artifacts, and sample reports for a cleaner repository.

---

## ✨ Key Features

### 👤 Patient Management
*   **Demographic Profile System:** Search, view, and manage client profiles with dynamic filtering.
*   **Unified Register:** Fast registration utilizing a dropdown gender selection and age tracking.
*   **Historical Diagnostics Log:** Access a patient's historical test records linked directly to their profile.

### 🩺 Doctor & Referral Management
*   **Referral Tracking:** Link orders to referring doctors using a drop-down menu in the ordering process (defaults to "Self" if none selected).
*   **Commission Tracking:** Manage commissions for reference doctors. Track unpaid/paid commissions per invoice.
*   **Doctor Directory:** Complete CRUD operations for reference doctors, their contact info, and commission percentages.

### 🧪 Test Ordering & Catalog Management
*   **Test Catalog & Departments:** Tests are categorized dynamically under clinical departments (e.g., Biochemistry, Hematology, Microbiology).
*   **Grouped Panel Ordering:** Quickly order entire panels (such as Lipid Panel, CBC Panel, CMP Panel).
*   **Dynamic Price Calculations:** Displays running totals for selected tests and packages.

### ⌨️ Keyboard-Friendly Result Entry
*   **Optimized Grid Navigation:** Use arrow keys and the Enter key to move quickly between parameters, enabling high-speed results data entry.
*   **Double-Validation Flow:** Actions for "Verify & Save" to finalize the result, and "Edit" to unlock parameters for correction.

### 🧬 Dynamic Demographic Reference Ranges
*   **Demographic Filters:** Evaluates biological reference ranges based on the patient's age and gender.
*   **Real-time Flags:** Automatically flags values outside the demographic-specific reference range as `ABNORMAL` for priority verification.

### 💳 Billing & Invoicing System
*   **Flexible Settlements:** Choose between Cash and UPI payment methods.
*   **Taxes & Discounts:** Dynamically adds tax and discount lines to invoices.
*   **Due Tracking:** Automatically tracks unpaid/due amounts if partial or no payment has been made.
*   **Integrated Preview:** WPF-integrated PDF preview window utilizing an embedded browser control.

### 💾 Backup & Data Export Engine
*   **Automatic Exit Backup:** Backs up the SQLite database file when the app is closed.
*   **ClosedXML Excel Workbook:** Generates formatted multi-tab Excel workbooks containing worksheets for all critical data.
*   **Manual Single-File CSV Export:** Export all tables to a structured CSV file via the Settings menu.

### ⚙️ Operator & Settings Profile
*   **Operator Info Profile:** Customize clinic metadata (Name, Address, Phone) stored locally.
*   **Security & PIN Verification:** Setup and use PIN for high-privilege access and actions.
*   **Audit Logs:** Logs system events locally with daily rolling Serilog file sinks.

---

## 🏗️ Architecture & Decoupling

The system utilizes a decoupled, **layered architecture** conforming to the **MVVM (Model-View-ViewModel)** pattern for the user interface, separating presentation logic from business constraints and data access.

```mermaid
graph TD
    UI["LabSystem.UI (WPF MVVM)"] --> Services["LabSystem.Services (Business Logic)"]
    UI --> Data["LabSystem.Data (EF6 / Repository)"]
    Services --> Core["LabSystem.Core (Domain Models & Interfaces)"]
    Data --> Core
    UI --> Core
    Data --> DB[("SQLite Database (lab.db)")]
```

### Layer Breakdown

1.  **`LabSystem.Core`**: Domain entities, models, and interfaces.
2.  **`LabSystem.Data`**: Entity Framework 6 data context and repository implementations.
3.  **`LabSystem.Services`**: Business logic, PDF rendering, backing up, and billing services.
4.  **`LabSystem.UI`**: WPF presentation layer with Material Design.

---

## Technology Stack

*   **Language & Runtime:** C# 5 / .NET Framework 4.5.1
*   **User Interface:** WPF (Windows Presentation Foundation) with MaterialDesignThemes
*   **Database & ORM:** SQLite and Entity Framework 6
*   **Spreadsheet Engine:** ClosedXML
*   **PDF Engine:** PDFsharp & MigraDoc
*   **Dependency Injection:** SimpleInjector
*   **Logging:** Serilog with rolling daily file sinks
*   **Testing:** NUnit and Moq

---

## Getting Started

### Prerequisites
1.  **Visual Studio 2019+** with the **.NET desktop development** workload
2.  **.NET Framework 4.5.1 targeting pack** (included with Visual Studio)

### Setup and Database Bootstrapping
The application automatically provisions a local SQLite database (`lab.db`) on first launch if it is not found, applying migration scripts and populating seed values.

### Building and Running
Open `LabSystem.sln` in Visual Studio and press **F5** to run, or build from the command line:
```bash
msbuild LabSystem.sln /p:Configuration=Release
```

---

## 🧪 Testing

The solution includes a test suite covering billing logic, report layouts, backup integrity, and patient records.

To execute the unit tests:
```bash
dotnet test
```
