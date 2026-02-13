# Feature Specification: Reverse-Engineer Current Calculator

**Feature Branch**: `001-reverse-engineer-spec`  
**Created**: 2026-01-29  
**Status**: Draft  
**Input**: User description: "reverse-engineering the current state of the dusseldorfer calculator into a formal specification"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Calculate support for children (Priority: P1)

A user enters income details for father and mother, adds one or more children with their status, and views the per-child payment amounts for each parent.

**Why this priority**: This is the core value of the calculator: produce a payment split per child.

**Independent Test**: Can be fully tested by entering sample parent incomes and a single child and verifying a computed payment split appears.

**Acceptance Scenarios**:

1. **Given** at least one child and income data for both parents, **When** the user opens the results page, **Then** the system shows relevant income per parent and per-child payment amounts for father and mother.
2. **Given** a minor child living with one parent, **When** the results are displayed, **Then** only the non-resident (bar) parent is assigned a payment amount and the resident parent shows zero.

---

### User Story 2 - Prefill income via OCR (Priority: P2)

A user uploads a tax notice/recap file for a parent and the form is prefilled with recognized values so manual entry is reduced.

**Why this priority**: Reduces time and errors when entering complex income fields.

**Independent Test**: Can be tested by uploading a file that includes recognizable tax fields and confirming the form fields are populated.

**Acceptance Scenarios**:

1. **Given** a supported file (image or PDF), **When** the user runs OCR, **Then** the form switches to detailed mode and fills any recognized values.
2. **Given** OCR fails or text cannot be parsed, **When** the user runs OCR, **Then** an error message is shown and existing inputs remain unchanged.

---

### User Story 3 - Save and reload a calculation snapshot (Priority: P3)

A user saves the current inputs and results as a snapshot file and later reloads it to restore the calculation state.

**Why this priority**: Enables reuse and audit of specific calculations.

**Independent Test**: Can be tested by saving a snapshot, reloading it, and verifying the inputs and results are restored.

**Acceptance Scenarios**:

1. **Given** a completed calculation, **When** the user saves a snapshot, **Then** a JSON file is created with all inputs and calculated results.
2. **Given** a previously saved snapshot, **When** the user loads it, **Then** the input fields and results match the snapshot data.

---

### Edge Cases

- What happens when no children are defined? The system must ensure at least one child exists and show a warning if results are requested without children.
- How does the system handle a child in own household who is a student? It must use the fixed student need and still apply child benefit and any child income.
- What happens when parent income after deductions is zero or negative? Relevant income and available income are treated as zero for payment allocation.
- How does the system handle a minijob with school/student allowance? The first 100 EUR is ignored for pupils/students, and only the remainder reduces the need.
- What happens when an owner-occupied property has costs higher than imputed rent? The advantage is not negative and does not reduce income below other components.
- What happens when a snapshot file is invalid JSON or too large? The system shows an error and keeps current state.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support separate input flows for father, mother, and children, and provide a results view that computes payments per child.
- **FR-002**: The system MUST support two income entry modes for each parent: detailed (gross, deductions, properties) and net-only (monthly or yearly), and the net-only mode MUST bypass all deductions and properties.
- **FR-003**: The system MUST accept monetary inputs as monthly or yearly values and convert yearly values to monthly by dividing by 12.
- **FR-004**: The system MUST allow multiple properties per parent with a label and inputs for rent income, operating costs, interest, principal, and imputed rent, and allow removing properties.
- **FR-005**: The system MUST allow multiple children and require at least one child to exist at all times.
- **FR-006**: The system MUST capture, per child: name, age, residence (with father, with mother, own household), school status, student status, kindergeld active, and optional minijob income.
- **FR-007**: The system MUST compute each parent’s relevant income using the following rules:
  - Start with monthly gross minus taxes, mandatory social security, and health insurance.
  - Subtract job-related expenses: if an absolute amount is provided, use it; otherwise use a percentage of the current net (default 5%).
  - Apply property effects for each property:
    - Rented: add (rent income minus operating costs) minus (interest + principal); this may be negative.
    - Owner-occupied: add max(0, imputed rent minus min(interest + principal, imputed rent)).
  - Add monthly tax refunds and other net incomes.
  - Subtract additional pension contributions up to a cap (default 4% of gross).
- **FR-008**: The system MUST calculate the child need based on the 2026 Dusseldorfer table with the following income bands, percentages, and needs per age tier (values in EUR):
  - Income bands (upper bounds): 2100, 2500, 2900, 3300, 3700, 4100, 4500, 4900, 5300, 5700, 6400, 7200, 8200, 9700, 11200.
  - Percentages by band: 100, 105, 110, 115, 120, 128, 136, 144, 152, 160, 168, 176, 184, 192, 200.
  - Needs for age 0-5: 486, 511, 535, 559, 584, 623, 661, 700, 739, 778, 817, 856, 895, 934, 972.
  - Needs for age 6-11: 558, 586, 614, 642, 670, 715, 759, 804, 849, 893, 938, 983, 1027, 1072, 1116.
  - Needs for age 12-17: 653, 686, 719, 751, 784, 836, 889, 941, 993, 1045, 1098, 1150, 1202, 1254, 1306.
  - Needs for age 18+: 698, 733, 768, 803, 838, 894, 950, 1006, 1061, 1117, 1173, 1229, 1285, 1341, 1396.
- **FR-009**: The system MUST determine the income band by rounding relevant income to the nearest whole EUR (midpoint away from zero), selecting the first band where income is less than or equal to the band upper bound; if above the highest band, use the highest band.
- **FR-010**: For student children living in their own household, the system MUST use a fixed need of 990 EUR (2026 value) regardless of income bands.
- **FR-011**: The system MUST apply child benefit (Kindergeld) of 259 EUR per month when active: half for minors and full for adults.
- **FR-012**: The system MUST reduce the child’s need by any minijob income after an allowance of 100 EUR for pupils/students; no allowance applies to other children.
- **FR-013**: The system MUST compute the relevant income used for a child as follows:
  - Minor child living with one parent: only the non-resident (bar) parent’s relevant income.
  - Adult child living with a parent: combined relevant incomes of both parents.
  - Adult child living in own household: combined relevant incomes of both parents (unless the fixed student need applies).
- **FR-014**: The system MUST split payments per child using self-support thresholds:
  - Necessary self-support: 1450 EUR per parent.
  - Regular self-support: 1750 EUR per parent.
  - For minors living with a parent: only the bar parent pays up to the child’s net need after own income, limited by bar parent’s available income after necessary self-support.
  - For others: use available income (income minus applicable self-support) to compute a proportional share, rounded to 2 decimals (midpoint away from zero).
- **FR-015**: The system MUST cap each parent’s share by the maximum they would owe if only their income were considered (liability cap), using the same child benefit and child income reductions.
- **FR-016**: The system MUST treat a child as privileged under 21 when age < 21, in general school, and not in own household; in this case, the lower (necessary) self-support applies for proportional split.
- **FR-017**: The system MUST provide a per-child breakdown explaining income band, need, child benefit, child income, and payment split.
- **FR-018**: The system MUST allow users to save a snapshot of inputs and results as a JSON file with a timestamped filename, and to load a snapshot to restore state.
- **FR-019**: The system MUST allow OCR-based prefill from image/PDF files up to 8 MB and display a success or error message.
- **FR-020**: The system MUST limit snapshot uploads to 1 MB and show an error when the file cannot be parsed.

### Key Entities *(include if feature involves data)*

- **Parent**: Represents a parent (father or mother) with income inputs, deductions, pension contributions, and property list.
- **Property**: Represents a rented or owner-occupied property with income, costs, and financing inputs.
- **Child**: Represents a child with age, residence, school/student status, kindergeld status, and optional minijob income.
- **MoneyInput**: A monetary value with a period (monthly or yearly) used across parent and child inputs.
- **Calculation Result**: Computed relevant incomes per parent and per-child need/payment breakdown.
- **Snapshot**: A saved bundle of inputs and computed results used for reload.

### Assumptions

- Users are expected to input values in EUR and monthly/yearly periods as labeled.
- Missing numeric inputs are treated as zero.
- The calculation is informational and does not replace legal advice.

### Dependencies

- OCR depends on browser support for file upload and client-side OCR execution; if OCR is not available, users can still enter values manually.
- Snapshot saving depends on browser support for saving or downloading files; if unavailable, a simple download is used.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can complete a calculation for one child and view payment amounts within 1 minute of data entry.
- **SC-002**: For standard input cases, 100% of calculated needs and payment splits match the rules in this specification.
- **SC-003**: At least 95% of users who save a snapshot can successfully reload it and see the same inputs and results.
- **SC-004**: OCR prefill completes within 30 seconds for supported files and populates at least one relevant field when recognizable values exist.
