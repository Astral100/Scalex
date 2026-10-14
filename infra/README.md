# infra

All operational/deployment files centralize here per [ADR 0017](../docs/adr/0017-monorepo-layout-and-solution-structure.md): `tofu/`, `helm/`, `vast/`, `telemetry/` ([ADR 0010](../docs/adr/0010-github-actions-opentofu-cicd.md) holds the tool split). The worker image recipe is the exception — it lives in `gpu-worker/` beside the agent source.
