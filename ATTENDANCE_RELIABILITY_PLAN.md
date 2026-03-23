# Attendance Reliability Plan

Purpose: make attendance and Excel reports production-safe, calendar-correct, and audit-friendly without silently undercounting or overcounting hours.

## Engineering Stance

- Always prefer correctness over convenience.
- Be skeptical before changing attendance math, date boundaries, or payroll-adjacent logic.
- Do not depend on server local timezone.
- Keep logic deterministic across environments and deployments.
- Optimize for fast queries, explicit business rules, and easy review by humans and agents.

## Deployment Rule

- Use `Asia/Tashkent` as the business timezone everywhere.
- This must work the same even if the app is deployed in another country or another server timezone.
- Implementation principle: compute business dates in Tashkent time, then convert the report/filter boundaries to UTC for querying.

Status: implemented

## Locked Business Rules

### 1. Business timezone

- Canonical timezone: `Asia/Tashkent`
- Server timezone must not affect results.

Status: implemented

### 2. Shift ownership

- A shift belongs to the `check-in` day.
- Night shifts crossing midnight still belong to the day when the employee checked in.

Status: implemented

### 3. Summary/detail consistency

- Summary sheet and detail sheet must use the same business-day logic.
- No UTC/local mismatch in grouping or counting days.

Status: implemented

### 4. Open shifts

- Open shifts stay open.
- Do not auto-close, cap, or rewrite them.
- They may remain visible as open in reports.

Status: implemented

### 5. Duplicate scan protection

- Add debounce/deduplication for suspicious repeated RFID scans.
- Goal: prevent accidental double check-in/check-out from inflating or breaking attendance.

Status: implemented

### 6. Anomaly handling

- Do not hard-cap shift duration.
- If a shift looks suspicious, flag it as anomalous.
- Hours must still be calculated; anomaly is informational, not blocking.

Status: implemented

### 7. Hour calculation

- No rounding.
- No lunch deduction.
- No overtime logic.
- Only calculate actual worked duration from attendance events.

Status: implemented

### 8. Manual/admin edits

- Manual/admin-created attendance must be visible in Excel.
- Prefer a clear marker in report output.
- Good options:
  - mark the row directly in detail sheet, or
  - add a summary section in sheet 1 listing manually edited shifts
- Detail sheet must still show the actual calculated hours.

Status: implemented

### 9. Grouping key

- Internal grouping should use stable identity such as employee ID.
- Excel output must still show employee names.

Status: implemented

## Recommended Implementation Order

### Phase 1: correctness first

- [x] Replace `Month/Day` filtering with Tashkent-local business ranges converted to UTC
- [x] Make `15 kunlik`, `bugungacha`, and other reports use the same date-boundary utility
- [x] Make summary sheet and detail sheet count/group by the same local-day rule
- [x] Keep shift ownership anchored to check-in day

### Phase 2: data quality protection

- [x] Add RFID debounce / duplicate event protection
- [x] Add anomaly flags for suspiciously long or inconsistent shifts
- [x] Ensure anomaly flags do not stop hour calculation

### Phase 3: auditability

- [x] Add a way to distinguish manual/admin attendance records
- [x] Surface manual/admin markers in Excel without hiding worked hours
- [x] Prefer a compact audit summary in sheet 1 plus visible row-level marking in detail sheet

### Phase 4: safety net

- [x] Add tests for:
  - 15th/16th boundary
  - 28/29 February
  - 30/31 month end
  - check-in before midnight, check-out after midnight
  - open shifts
  - duplicate scans
  - anomaly-flagged long shifts

## What Is Already Done

- [x] Business rules discussed and narrowed with product direction
- [x] Main risks identified in current logic
- [x] Plan documented in a root-level file
- [x] Phase 1 timezone-safe report boundaries implemented
- [x] Duplicate scan protection added to attendance handling
- [x] Non-blocking anomaly markers added to Excel reports
- [x] Manual/admin source persistence and migration
- [x] Tests
- [x] Excel audit markers

## Notes For Future Changes

- Do not mix display timezone conversion with filtering logic.
- Do not use employee name as the grouping key.
- Do not introduce hidden rounding.
- Do not silently “fix” bad data; flag it.
- Every report change should be reviewed against real edge cases before release.
