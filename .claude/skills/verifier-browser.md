# Browser Verifier — YAAM Frontend

Evidence-capture protocol for Angular/browser surface changes. Invoked by the `verify` skill.

## Launch

Start the Angular dev server if not already running:

```bash
cd /Users/uchendu/yaam/frontend && npm start
```

Wait for **"Application bundle generation complete."** The app is at **http://localhost:4200**.

If the change involves API calls, start the backend too:

```bash
dotnet run --project /Users/uchendu/yaam/backend/Yaam.API
```

## Browser automation

Use `playwright-cli` commands. The `playwright-cli` skill is installed — invoke it for the full command reference.

Key commands:
```bash
playwright-cli open http://localhost:4200/<route>
playwright-cli snapshot          # inspect page structure and element refs
playwright-cli screenshot        # capture evidence — use after each meaningful state change
playwright-cli click e15         # click element by ref from snapshot
playwright-cli type "text"       # type into focused input
playwright-cli fill e5 "value"   # fill a specific input
playwright-cli find "text"       # locate text on the page
```

## Session

1. `playwright-cli open http://localhost:4200/<affected-route>`
2. `playwright-cli snapshot` to inspect the page
3. Drive the UI to exercise the changed code path
4. `playwright-cli screenshot` as primary evidence
5. Probe adjacent controls and edge cases for regressions
6. `playwright-cli screenshot` to capture any unexpected state
