# YAAM — Claude Code Guidelines

## Generated files — never edit manually

`frontend/src/app/generated/` is owned by the OpenAPI generator. Never edit any file under this path by hand.

To regenerate after adding or changing backend endpoints:
1. Start the backend: `dotnet run --project backend/Yaam.API`
2. From `frontend/`: `npm run generate:api`

The generator pulls the OpenAPI spec from the backend's live `/openapi/v1.json` endpoint and writes the Angular service + model files to `src/app/generated/api/`. All component imports referencing the generated path must be updated after regeneration if the output structure changes.

## Frontend — use the daisyUI skill

Before writing any frontend HTML or component templates, invoke the `daisyui` skill. daisyUI is the mandatory component library for all UI in this project — never use raw Tailwind utility classes for components that daisyUI covers (buttons, badges, cards, modals, inputs, alerts, etc.).
