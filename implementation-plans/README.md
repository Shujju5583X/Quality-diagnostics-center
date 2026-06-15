# Implementation Plans — Quality Diagnostics Center

## Phase Status

| Phase | Name | Status | Effort | Dependencies |
|-------|------|--------|--------|--------------|
| **Phase 1** | Fix Foundations | ✅ COMPLETED | 1 day | None |
| **Phase 2** | Core Lab Features | ✅ COMPLETED | 1 day | Phase 1 |
| **Phase 3** | Financial | ✅ COMPLETED | 2-3 days | Phase 2 |
| **Phase 4** | Clinical | ✅ COMPLETED | 3-4 days | Phase 2 |
| **Phase 5** | Operations | ✅ COMPLETED | 3-4 days | Phase 3, 4 |
| **Phase 6** | Scale | ✅ COMPLETED | 4-5 days | Phase 5 |

## Dependency Graph

```
Phase 1 (Foundations) → Phase 2 (Core Lab) → Phase 3 (Financial)  → Phase 5 (Operations) → Phase 6 (Scale)
                                            → Phase 4 (Clinical)  ↗
```

## What Each Phase Covers

### Phase 3 — Financial
- Discount/tax UI inputs on billing tab
- Partial payment support (pay portion of invoice)
- Revenue reports (daily/weekly/monthly summaries)
- Payment history per patient

### Phase 4 — Clinical
- Report amendment workflow UI (amend button + reason dialog)
- Patient history view (cross-order results over time)
- Patient trends visualization (charts for key markers)
- QC module (control runs, Levey-Jennings charts, Westgard rules)

### Phase 5 — Operations
- SMS notification gateway (appointment reminders, result ready)
- Appointment system (booking, scheduling, calendar view)
- Staff management (CRUD, role assignment, schedule)

### Phase 6 — Scale
- Multi-branch support (branch entity, data isolation)
- Database migration (SQLite → PostgreSQL/SQL Server option)
- REST API layer (for mobile app / external integrations)

## How to Use These Files

1. Pick a phase you want to work on
2. Open the corresponding `.md` file
3. Follow the task list in order (each task has file paths and code snippets)
4. After completing a phase, run `dotnet build` and `dotnet test` to verify
5. Commit and push to GitHub

## Pre-Phase 3 Checklist

Before starting Phase 3, ensure:
- [ ] Phase 1 and Phase 2 are committed and pushed
- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` passes all 25 tests
- [ ] Application launches and basic workflow works (register patient → order → results → PDF)
