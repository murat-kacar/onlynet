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
TF0002  | Testing  | Warning  | Unit-tier test purity rule (AC-133). Flags any identifier inside a class carrying `[Trait("Category", "Unit")]` that resolves to `Npgsql.*`, `System.Net.Sockets.*`, `System.Net.Http.HttpClient`, `System.IO.File`/`Directory`/`FileStream`, `DateTime.Now`, or `DateTimeOffset.Now`. Move tests that need any of these to the Integration tier.
TF0003  | Design   | Warning  | Controller-on-DbContext rule (AD-0003 / TD-0022). Flags any class derived from `Microsoft.AspNetCore.Mvc.ControllerBase` that holds an EF Core `DbContext` as a field, property, or constructor parameter. Move the read or write into an application service.
