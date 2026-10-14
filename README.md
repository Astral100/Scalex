# Scalex

Backend for a multi-user video-upscaling web service: upload a video, pick a preset, and a GPU worker rented from the Vast.ai marketplace runs the upscale and hands back a download link. `Scalex` is the internal project name; the public brand is decided at launch ([ADR 0018](docs/adr/0018-internal-name-scalex-brand-deferred.md)).

## Build and test

```
dotnet build Scalex.sln
dotnet test Scalex.sln
```

CI runs the same two commands on every push, as information only — a red run blocks nothing.

## Layout

One solution at the root spans all .NET projects ([ADR 0017](docs/adr/0017-monorepo-layout-and-solution-structure.md)):

- `backend/` — the deployable service: `Scalex.Api` (web host and composition root), `Scalex.AppCore` (records and rules), `Scalex.Dispatcher` (dispatch seam), `Scalex.Provisioner` (worker-pool lifecycle), `Scalex.Maintenance` (recovery checks, deletion pass), `Scalex.IntegrationTests` (shared cross-project suite).
- `shared/` — `Scalex.Contracts`, the backend↔agent message shapes referenced by both entry points.
- `gpu-worker/` — `Scalex.GpuWorker`, the worker agent, plus (later) the worker image recipe.
- `frontend/` — the future website; out of scope for backend v1.
- `infra/` — OpenTofu, Helm, Vast and telemetry configuration.
- `docs/` — the [backend spec](docs/backend-spec.md), [API contract](docs/api-contract-v1.md) and [ADRs](docs/adr/).
- `probes/` — standalone probe scripts; stays until revisited toward project completion.

The spec in [`docs/backend-spec.md`](docs/backend-spec.md) is the implementation map; each service project self-registers its DI via one `Setup<Name>Di()` extension method.
