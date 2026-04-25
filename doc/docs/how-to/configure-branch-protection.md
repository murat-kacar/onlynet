# Configure Branch Protection

This guide describes the GitHub branch-protection settings that enforce
the constitution's review rules
([`../constitution.md`](../constitution.md) Section V) and the
review policy ([`../meta/review-policy.md`](../meta/review-policy.md)).

These settings are part of the **operational contract** of the
repository. They are not optional; a maintainer who can disable them
also can land unreviewed code, which violates V.1.

## Required Settings For `main`

Apply the following ruleset to the `main` branch
(GitHub → Settings → Branches → Add classic branch protection rule, or
the equivalent in Repository Rulesets).

### Pull Request Rules

- [x] **Require a pull request before merging**
- [x] **Require approvals: 1 minimum**
- [x] **Dismiss stale pull request approvals when new commits are
  pushed**
- [x] **Require review from Code Owners** (once `CODEOWNERS` is in place)
- [ ] Require approval of the most recent reviewable push *(off — author
  can re-request after pushing fixups)*

### Status Check Rules

- [x] **Require status checks to pass before merging**
- [x] **Require branches to be up to date before merging**

Required status checks (defined in `.github/workflows/pr.yml` per
AD-0013):

- `build` — `dotnet build` clean against the release configuration
- `test` — `dotnet test` across every test project
- `format` — `dotnet format --verify-no-changes` clean
- `markdown-lint` — markdown lint passes on every tree under
  `/doc/`
- `dead-link` — dead-link checker passes on every tree under `/doc/`
- `dep-audit` — dependency audit (e.g. GitHub Dependabot or NuGet
  audit) clean
- `sast` — SAST workflow clean

### History Rules

- [x] **Require linear history** *(no merge commits — squash or rebase
  only)*
- [x] **Require signed commits**
- [ ] Require deployments to succeed *(off — release deploy lives in
  `release.yml` and is post-merge)*

### Push Rules

- [x] **Restrict who can push to matching branches** — only the GitHub
  Actions release workflow may push the release tag; humans push only
  through PR merges
- [x] **Block force pushes**
- [x] **Restrict deletions** — `main` cannot be deleted

### Bypass Permissions

- [ ] *No bypass list.* Even repository owners merge through PRs. The
  only legitimate bypass is the stop-the-line build-fix exception
  recorded in [`../meta/review-policy.md`](../meta/review-policy.md),
  which still requires a follow-up retroactive review PR within one
  working day.

## Required Settings For Release Tags

Apply a separate ruleset to tags matching `v*.*.*`:

- [x] **Restrict who can create matching refs** — only the GitHub
  Actions `release.yml` workflow identity
- [x] **Block deletion of matching refs**
- [x] **Block force-push to matching refs**

This ensures release tags exist only after the gate workflow has
passed.

## Verifying The Configuration

The branch-protection settings are themselves a contract. Drift is a
release-gate failure. To verify:

```bash
gh api repos/<owner>/<repo>/branches/main/protection \
  | jq '{ pr_required: .required_pull_request_reviews,
          checks: .required_status_checks.contexts,
          linear: .required_linear_history.enabled,
          signed: .required_signatures.enabled,
          force_push: .allow_force_pushes.enabled,
          deletions: .allow_deletions.enabled }'
```

Expected output (illustrative):

```json
{
  "pr_required": { "required_approving_review_count": 1, "dismiss_stale_reviews": true, ... },
  "checks": ["build", "test", "format", "markdown-lint", "dead-link", "dep-audit", "sast"],
  "linear": true,
  "signed": true,
  "force_push": false,
  "deletions": false
}
```

A CI workflow `branch-protection-check` SHOULD run this verification on
every push to `main` and fail the build if any of these fields drifts.
The workflow lives at `.github/workflows/branch-protection-check.yml`
when implemented (currently a tech-debt ledger entry).

## Recovering From Disabled Protection

If branch protection is ever found disabled or weakened:

1. **Re-enable** immediately to the documented configuration above.
2. **Open a postmortem** in [`/doc/buildlog/postmortems/`](/doc/buildlog/)
   describing how it happened, what was merged in the gap, and whether
   any merged change needs retroactive review.
3. **Audit** every commit on `main` that landed during the gap. Any
   that lack a corresponding PR with one non-author approval is opened
   as a retroactive review PR and reviewed.

## Related

- [`../constitution.md`](../constitution.md) Section V — review rules
- [`../meta/review-policy.md`](../meta/review-policy.md) — review
  checklist
- [`../meta/release-gate.md`](../meta/release-gate.md) — verification
  the gate enforces
- [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)
  AD-0013 — GitHub Actions as CI platform
