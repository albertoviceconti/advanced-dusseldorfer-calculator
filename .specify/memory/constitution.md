<!--
Sync Impact Report
- Version change: 0.0.0 -> 0.1.0
- Modified principles:
  - Template placeholder 1 -> Legal Fidelity and Traceability
  - Template placeholder 2 -> Local, Dependency-Light Execution
  - Template placeholder 3 -> Privacy by Default
  - Template placeholder 4 -> Explainable Results
  - Template placeholder 5 -> Robust Input Modeling
- Added sections:
  - Product Scope and Constraints
  - Development Workflow and Quality Gates
- Removed sections: None
- Templates requiring updates:
  - ✅ updated `.specify/templates/plan-template.md`
  - ✅ updated `.specify/templates/spec-template.md`
  - ✅ updated `.specify/templates/tasks-template.md`
- Follow-up TODOs: None
-->
# Advanced Dusseldorfer Calculator Constitution

## Core Principles

### Legal Fidelity and Traceability
All calculations MUST follow the published Dusseldorfer Tabelle and applicable
legal adjustments. Every output MUST surface the table year, income group, age
bracket, and each adjustment step used. Any change to calculation rules MUST
cite the official source and update the rule mapping.

### Local, Dependency-Light Execution
All calculation logic MUST execute locally within the Blazor application with
no external service calls. External dependencies (packages, services, or APIs)
are prohibited unless explicitly approved in Governance with a documented need.

### Privacy by Default
Personal data MUST stay in memory by default and MUST NOT be logged or shared.
Persistence or export of data MUST be opt-in and initiated by the user with
clear confirmation. Provide a clear reset path that wipes in-session data.

### Explainable Results
The UI MUST present a readable calculation breakdown, including inputs,
intermediate steps, and final results. Validation errors MUST identify the
field, rule, and expected range or format.

### Robust Input Modeling
The system MUST support multiple parents, multiple children, and relevant income
types and deductions. Inputs MUST be validated for ranges and consistency, and
defaults MUST be explicit and visible to the user before calculation.

## Product Scope and Constraints
This is a Blazor web application for calculating child support amounts based on
the Dusseldorfer Tabelle. The app MUST function without external network calls
for core calculations and MUST not require third-party services. The primary
delivery target is a responsive web UI for desktop and mobile browsers.

## Development Workflow and Quality Gates
Changes to calculation rules MUST include an updated table source reference and
at least one verification step (test or documented manual check). UI changes
MUST preserve the explainability breakdown and validation clarity. All changes
MUST include a constitution compliance check before merge.

## Governance
This constitution supersedes local conventions and templates. Amendments require
documentation of the change, rationale, and any migration notes. Versioning
follows SemVer: MAJOR for breaking governance changes, MINOR for new principles
or significant expansions, PATCH for clarifications and typo fixes. Every PR
MUST include a brief compliance review against the Core Principles.

**Version**: 0.1.0 | **Ratified**: 2026-01-20 | **Last Amended**: 2026-01-20
