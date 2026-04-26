; Unshipped analyzer releases.
;
; Diagnostic IDs introduced after the most recent tagged release of
; TabFlow. On the next release PR, the entries below are moved into
; AnalyzerReleases.Shipped.md under the corresponding version
; heading.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TF0001  | Naming   | Warning  | English-first identifier rule (AD-0015, AC-117). Per `Directory.Build.props` `TreatWarningsAsErrors=true`, this surfaces as a build break in `/src/**`.
