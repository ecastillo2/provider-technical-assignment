# Submission Checklist

Walk through this list once before sending the GitHub link. Cross items off as you go.

## 1. Code is clean

- [ ] `dotnet build` succeeds with **zero warnings, zero errors**
- [ ] `dotnet test` shows **all 20 tests passing**
- [ ] `dotnet run` starts the app, opens the browser to the home page

## 2. Smoke test in the browser

- [ ] Home page renders
- [ ] **Dashboard** — both charts render; "Active w/ expired licenses" card shows ≥ 1
- [ ] **Providers Index** — shows 7 providers; search "DeKalb" filters; Status filter works
- [ ] **Provider Details** — opens, shows licenses, "Add License" CTA visible
- [ ] **Create Provider** — submit creates a new row, redirects to Details
- [ ] **Edit Provider** — change name and save, see flash message
- [ ] **Delete (soft)** — provider disappears from listing, appears in Audit
- [ ] **Audit page** — pre-seeded "North Fulton Behavioral" is shown
- [ ] **Restore** — restore that provider, confirm it (and its license) come back
- [ ] **License Create** — try expiration in the past → validation error on Create
- [ ] **License Edit** — past expiration on existing license is allowed

## 3. Database file is committed

- [ ] `Data/providers.db` exists in the repo (open Explorer, confirm size > 0)
- [ ] After `dotnet run` regenerates it, `git status` should show it modified
- [ ] `git add Data/providers.db && git commit -m "chore: ship pre-built DB"`
- [ ] Verify `.gitignore` does NOT exclude it (it doesn't — comment in the
  file documents this intentionally)

## 4. Documentation is complete

- [ ] `README.md` — read top to bottom; section numbering correct
- [ ] `docs/PR_DESCRIPTION.md` — the text to paste into the PR
- [ ] `Data/Sql/README.md` — present
- [ ] No TODO / FIXME / `<repo-url>` placeholders left

## 5. Git hygiene

- [ ] All work is on a feature branch (NOT `main` — assignment requires this)
- [ ] No accidental commits to `main`
- [ ] No `bin/`, `obj/`, `.vs/`, `.user`, or IDE files committed
- [ ] `git log --oneline` shows clear, conventional-commit-style messages

## 6. Push and open the PR

```bash
git push -u origin <feature-branch>
```

Then on GitHub:

- [ ] Open a PR from `<feature-branch>` → `main`
- [ ] Title: `Take-Home Submission · Provider / License Management`
- [ ] Body: paste the contents of `docs/PR_DESCRIPTION.md`
- [ ] Confirm the diff looks right (no surprises)

## 7. Final submit

- [ ] Copy the PR URL OR the branch URL
- [ ] Send to the hiring contact per the assignment's submission instructions
- [ ] Optional: include a short note saying the README and PR description
  are the best places to start the review

---

## If something fails

| Symptom | Fix |
|---|---|
| `dotnet build` errors | Paste output to me; I'll fix |
| `dotnet test` failures | Paste output to me; the suite is small enough to debug fast |
| App boots but DB is empty | Delete `Data/providers.db` and run again — `EnsureCreated + DbInitializer.SeedAsync` will rebuild it |
| Charts don't render on Dashboard | Check browser console — Chart.js loads from jsDelivr; corporate proxy may block |
