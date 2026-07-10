# YAAM Frontend

Angular 22 app. Requires Node 24.

## Start

```bash
npm install
npm start        # http://localhost:4200
```

The app expects the backend at `https://localhost:7131`. See the [root README](../README.md) for backend setup.

## Build

```bash
npm run build                              # development
npx ng build --configuration production   # production (uses environment.prod.ts)
```

Output goes to `dist/`.

## Formatting — Prettier

```bash
npm run format         # fix
npm run format:check   # check only (used in CI)
```

Config: `.prettierrc`. Covers `src/**/*.{ts,html,scss,json}`.

## Linting — ESLint

```bash
npm run lint
```

Config: `eslint.config.js` (flat config, angular-eslint + typescript-eslint).

## Testing

```bash
npm test           # unit tests via Vitest (watch mode)
npx ng e2e         # e2e — no framework configured yet
```

Unit tests live alongside their source files (`*.spec.ts`). No e2e framework is set up yet — add one (e.g. Playwright) when the first user-facing flow is ready.

## Scaffolding

Use Angular CLI to generate new code:

```bash
npx ng generate component features/applications/components/my-component
npx ng generate service core/http/my-service
```

Always place generated files in the right feature folder — see the [architecture guidelines](../docs/architecture/guidelines.md).
