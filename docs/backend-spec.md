# Scalex Backend Specification (v1)

Assembled 2026-10-13 by closing [Assemble the backend spec and implementation backlog (#15)](https://github.com/Astral100/Scalex/issues/15), the destination ticket of the [wayfinder map (#1)](https://github.com/Astral100/Scalex/issues/1). Every decision below was settled on the map; this document gists each one and links the ticket or ADR that holds its full detail. Where this spec and a linked source disagree, the linked source wins.

## Problem Statement

People have videos they care about — old footage, low-resolution clips, compressed uploads — that look bad on modern screens, and no practical way to fix them: consumer upscaling tools are weak, and the strong models (SeedVR2) demand data-center GPUs, deep technical setup, and hours of babysitting per video. The proof of concept (`Astral100/SeedVr`) proves the full pipeline works — rent a GPU, drive ComfyUI + SeedVR2, deliver a result — but it is a single-job console program only its author can run.

## Solution

A production backend that turns that pipeline into a multi-user web service: a user signs in, uploads a video straight to object storage, picks a preset (or tunes raw parameters), and submits a job. The backend queues it, finds or rents a GPU worker from the Vast.ai marketplace, runs the upscale, streams live progress, and hands back a download link — retrying infrastructure failures silently and surfacing honest errors fast. One operator runs the whole thing cheaply: self-hosted core services, a self-managed worker pool that scales with demand, and an observability stack that pages when something needs a human.

## User Stories

### Accounts

1. As a visitor, I want to sign up with e-mail+password or Google, so that I can start upscaling without an approval step.
2. As a user, I want my sign-in handled by a trusted identity provider, so that my credentials are safe and bots are kept out.
3. As a user, I want to request account deletion and have a grace period, so that a mistaken deletion can be fully undone by support.
4. As an operator, I want deleted accounts purged on both sides (our records + identity provider) after the grace period, so that no orphaned personal data lingers.

### Uploading

5. As a user, I want my video to upload directly from my browser to storage, so that uploads are fast and don't bottleneck through the API.
6. As a user, I want the backend to validate my file right after upload (duration, resolution, codec), so that a bad file fails immediately, not after queueing.
7. As a user, I want to re-run a new job against a file I already uploaded, so that I don't re-upload gigabytes to try different settings.
8. As a user, I want files up to 2 GB accepted, so that realistic source videos fit.

### Submitting jobs

9. As a user, I want to pick a named preset and submit, so that I don't need to understand model parameters.
10. As an advanced user, I want to set raw parameters directly (model, precision, CRF, bit depth…), so that I control exactly what runs.
11. As a user, I want the parameters my job actually ran with recorded on the job, so that results are reproducible even after presets change.
12. As a user, I want submitting an identical job twice rejected with a clear signal, so that a double-click doesn't bill two GPU runs.
13. As a user, I want job validation errors returned as stable, documented error codes, so that the frontend can show precise messages.

### Watching and controlling jobs

14. As a user, I want a coarse, honest status (queued / processing / done / failed / cancelled), so that I always know where my job stands.
15. As a user, I want live progress percent and an ETA while processing, so that I can decide to wait or come back later.
16. As a user, I want the ETA to include worker-acquisition time when no worker is free, so that the estimate is honest from the moment I submit.
17. As a user, I want to cancel a job instantly from my view, so that I stop paying attention (and the operator stops paying) for a run I no longer want.
18. As a user, I want a capped list of my recent jobs cheap enough to poll, so that my dashboard stays live without hammering the service.
19. As a user, I want transient infrastructure failures retried automatically and invisibly (up to a limit), so that marketplace flakiness doesn't become my problem.
20. As a user, I want a job that fails for content reasons (broken file, impossible parameters) to fail immediately with the reason, so that I can fix the input instead of waiting out retries.

### Results

21. As a user, I want a download link generated on demand for the finished video, so that I can fetch the result when I'm ready.
22. As a user, I want to share a result link that works for a bounded time, so that I can show the output without sharing my account.
23. As a user, I want my result to survive a worker dying at the last moment (late-completion rescue), so that a finished render is never thrown away.

### Operating the service

24. As an operator, I want a warm floor of ready workers, so that the next job starts without provisioning delay.
25. As an operator, I want the pool to grow on demand and shrink back to the floor after idle minutes, so that GPU spend tracks real usage.
26. As an operator, I want bad marketplace machines remembered and banned with escalating durations, so that the same lemon never wastes money twice.
27. As an operator, I want a quiet-time scouting pass testing cheap candidate machines, so that the floor's quality/price improves on its own.
28. As an operator, I want every worker heartbeating and every attempt tagged, so that a stalled or superseded worker can never corrupt a live job.
29. As an operator, I want logs and metrics pushed to a dedicated telemetry box with alerting (e-mail + channel) and a dead-man's switch, so that I hear about problems before users do.
30. As an operator, I want trunk pushes deployed automatically with migrations applied first, so that shipping a fix is one merge.
31. As an operator, I want all infrastructure (Hetzner, Cloudflare, B2, Vast templates) captured as code, so that rebuilding from zero is a procedure, not an archaeology dig.
32. As an operator, I want nightly-verifiable Postgres PITR with an off-provider second copy, so that the database — the only durable record — survives any single provider failing.
33. As an operator, I want agent updates hot-swapped and image updates applied by re-imaging in place, so that routine releases don't churn the machine pool.
34. As an operator, I want workers to hold no storage credentials, so that a compromised rented GPU can't touch anything beyond its own job.

### Future-proofing

35. As the product owner, I want new job types (picture enhancement, other models) to fit the job abstraction without schema surgery, so that the second product feature is an addition, not a rewrite.
36. As the product owner, I want billing seams (balance check at submit, credits attachment point on the user) already in place, so that payments can be added before launch without re-architecting.
37. As the product owner, I want the serverless dispatcher alternative kept interchangeable behind the dispatcher seam, so that the orchestration bet is reversible.

## Implementation Decisions

Grouped by area. Each line gists the decision; the linked ticket resolution or ADR is the authority.

### Domain model — [#5](https://github.com/Astral100/Scalex/issues/5), [#21](https://github.com/Astral100/Scalex/issues/21)

- One shared `Job` entity (UUIDv7 id used everywhere, owned by a user) + per-type typed parameter tables holding effective values; future job types add a parameter table, not job columns.
- Two-level state: stored fine-grained `JobState` (10 values after the input-file split), derived `UiJobStatus` via a single pure mapping, never stored ([CONTEXT.md](../CONTEXT.md) definitions).
- Input files are their own aggregate with their own lifecycle (validated at upload confirmation, may feed many jobs, bytes expire independently of the record); jobs are born at submit, not at upload.
- Template → request parameter layering with the user-facing vs machine-fit split ([ADR 0002](adr/0002-user-facing-vs-machine-fit-parameters.md)); presets are backend-defined and versioned but resolved client-side — requests always carry raw parameters ([ADR 0001](adr/0001-frontend-resolves-presets.md)).
- Attempts: one delivery of a job to a worker, own identity (`{jobId}-a{n}`), intent-before-action record updated in place; everything a worker reports is attempt-tagged.
- `BatchJob` (parent over N ordinary jobs) is the designed batch path, post-v1.

### API contract — [#10](https://github.com/Astral100/Scalex/issues/10), [`docs/api-contract-v1.md`](api-contract-v1.md)

- The client-facing surface is locked in the contract document: one generic `/jobs` collection for all job types, upload handshake with synchronous ranged-probe validation, record-vs-ticker job representations, capped poll-safe jobs list, on-demand result links, current-versions-only preset catalogue.
- Promise-rule error model: Problem Details + stable registered codes ([ADR 0012](adr/0012-http-promise-rule.md)); constraint-backed 409 on identical active jobs (no idempotency tokens); Postgres outage → friendly 503.
- OpenAPI is generated from code later; the contract document is the v1 authority.

### Job pipeline — [#8](https://github.com/Astral100/Scalex/issues/8)

- Postgres itself is the queue (SKIP LOCKED, no broker) inside a single deployable ([ADR 0007](adr/0007-postgres-as-job-queue.md)); dispatcher and recovery checks run as background services, the dispatcher in its own project.
- Push dispatch; dispatch is a callable idempotent operation that claims from the workers table and triggers the provisioner directly; overlaps designed out, no patrol.
- Liveness: 15 s worker-agent heartbeats; 60 s silence = stall — but silence only counts while the backend was listening; progress is never a stall signal; hard cap max(3× estimate, 30 min).
- Completion = first of done-POST / webhook / R2 existence check, with a recovery check as the final net; two-step cancel with instant-Cancelled UI and Succeeded-wins race.
- Clients poll every 2–3 s off the job row; no worker secret (attempt identity is the credential).

### Failure recovery — [#21](https://github.com/Astral100/Scalex/issues/21)

- Two failure classes: content → immediate Failed; machinery → silent retry at front of queue, cap 3 attempts + 1 h queue age.
- Every non-terminal state has a state timeout (tunable duration, locked action), enforced by recovery checks that are safe to run repeatedly.
- No redundant GPU runs: HEAD the dead attempt's output before any retry; late completions rescue the job.
- General worker-recovery escalation ladder (graceful cancel → agent restarts ComfyUI → instance kill) fired by cancels and failed health probes; broken machines get immediate re-route.

### Worker orchestration — [#6](https://github.com/Astral100/Scalex/issues/6), [#17](https://github.com/Astral100/Scalex/issues/17)

- Self-managed worker pool rented via Vast's instance API is primary ([ADR 0014](adr/0014-self-managed-worker-pool-primary.md), reversing [ADR 0003](adr/0003-serverless-primary-worker-orchestration.md)'s ordering); the serverless endpoint design is the kept, unbuilt alternative behind the dispatcher seam.
- Warm floor of 2 Ready workers; `max_workers` ~30–40 as accident guard; downscale to floor after ~10 idle minutes; On-Demand rentals only; one job per worker.

### Provisioner and pool lifecycle — [#23](https://github.com/Astral100/Scalex/issues/23), [ADR 0016](adr/0016-provisioner-and-worker-pool-lifecycle.md)

- Six-state worker lifecycle on a `workers` table + permanent machine ledger with auto-computed quality scores; admission = readiness gate only (no mini-render — speed is predicted from hardware fingerprint + job history).
- Cheapest-strangers racing with log-parsed download rates and projection-based early kills; budget-capped scouting scaled to the floor; escalating bad-machine bans (14 d, +7 per repeat, redemption resets).
- Two-tier updates: agent binaries hot-swapped in place; image releases re-image instances in place via Vast update+recycle (destroy-and-re-rent is the fallback).
- No offers on the market = immediate top-severity alert; manually rented instances carrying our image are adopted via the reconcile pass; pre-delivery probe only after ~30 min idle; Jupyter stays exposed.

### Worker image — [#17](https://github.com/Astral100/Scalex/issues/17), [ADR 0015](adr/0015-worker-image-contents-and-distribution.md)

- Baked, layered image: latest stable ComfyUI (≥0.30; the POC console app is the upgrade test rig), pinned SeedVR2 nodes, stock wrapper, self-contained single-file .NET worker agent (nothing installed into the image's conda Python).
- ~57 GB model set: 3B fp16; 7B and 7B-sharp each in fp16 and fp8-mixed; precision is an ordinary parameter (preset-picked or advanced-mode file selection); plain-fp8 3B excluded; int8-ConvRot deferred post-v1.
- One byte-stable layer per model file; variants are layer selections — full image for the warm floor, single-model image for urgent bursts, chosen per rental.
- Distribution via self-hosted registry on the Hetzner box, R2 storage backend, presigned-redirect downloads; pull credentials long-lived or anonymous (Vast can't rotate creds on a live instance).

### Storage — [#7](https://github.com/Astral100/Scalex/issues/7), [ADR 0004](adr/0004-cloudflare-r2-object-storage.md), [ADR 0005](adr/0005-workers-hold-no-storage-credentials.md)

- Cloudflare R2 committed (free egress ≈ $60/mo heavy ceiling vs ~$443 S3; realistic ~$3–4/mo); Backblaze B2 is the priced S3-compatible fallback.
- Direct presigned-PUT browser uploads (2 GB single-PUT cap; multipart deferred to [v2-features](v2-features.md)); on-demand presigned GET downloads, 2-day validity; per-env buckets, direction-rooted keys.
- Workers hold no storage credentials: inputs arrive as presigned GET URLs inside the workflow, outputs are uploaded by the worker agent via per-job presigned PUTs (input URL reused across attempts; output keys per-attempt).
- The backend's periodic deletion pass owns deletion; R2 lifecycle rules are a safety net (plus day-one abort-incomplete-multipart); re-run-without-reupload = server-side copy from an existing input file id; no content-hash column.

### Hosting — [#9](https://github.com/Astral100/Scalex/issues/9), [ADR 0008](adr/0008-self-hosted-hetzner-core-services.md), [ADR 0009](adr/0009-k3s-cnpg-runtime-stack.md)

- Full self-ops on one Hetzner CX33 (Falkenstein) after a 5–12× managed premium finding; fallback recorded (App Service B1).
- Runtime stack K3s + Helm + CloudNativePG (Barman Cloud Plugin); no GitOps pull for now; Cloudflare in front as the edge.
- PITR binding: WAL to R2 + monitored second copy at Backblaze B2; restore rehearsed before launch; automated security patching required.

### Auth — [#11](https://github.com/Astral100/Scalex/issues/11), [ADR 0013](adr/0013-clerk-hosted-auth.md)

- Clerk as identity provider (free plan to start): e-mail+password+Google, open signup, Clerk bundles bot check and auth e-mails; API validates JWTs via JWKS; users mirrored into Postgres (webhook upsert + create-from-token).
- Two-sided soft delete with a 30-day tunable grace period; purge by the periodic deletion pass; result links shareable by design.
- Billing seams: credits attachment point on the user record + one always-approve affordability gate at job submission.
- Accepted risks: US-only residency; Clerk outage takes sign-in down. ASP.NET Identity is the verified-exit fallback.

### Observability — [#12](https://github.com/Astral100/Scalex/issues/12), [ADR 0011](adr/0011-dedicated-telemetry-box.md)

- Dedicated Hetzner telemetry box in Helsinki: Caddy + Grafana + Prometheus + Loki as pinned containers; 12-month logs / 13-month metrics; no backups — Postgres is the durable record.
- OpenTelemetry push from backend and worker agents (attempt-tagged live log streams, buffered retry); errors-as-logs.
- Alerts via e-mail + a Telegram/Slack channel; healthchecks.io as the inside-out dead-man's switch; wake-you-up pager deferred.

### CI/CD and infrastructure as code — [#13](https://github.com/Astral100/Scalex/issues/13), [ADR 0010](adr/0010-github-actions-opentofu-cicd.md)

- GitHub Actions trunk-CD to a single prod environment, path-filtered per monorepo area; SSH-tunnel Helm deploys; EF migration bundle runs as a pre-upgrade job; GitHub Environment secrets only, names-only re-issue runbook.
- IaC in three zones: OpenTofu for Hetzner/Cloudflare/B2 (encrypted state in R2), Helm in-cluster, a versioned Python script over Vast's SDK for templates (no IaC provider exists).
- Backend images on GHCR; the worker image goes through the self-hosted R2-backed registry; builds on free runners + an ephemeral Hetzner VM for rare fat model builds.

### Code layout — [#20](https://github.com/Astral100/Scalex/issues/20), [ADR 0017](adr/0017-monorepo-layout-and-solution-structure.md), [ADR 0018](adr/0018-internal-name-scalex-brand-deferred.md)

- Internal name Scalex on all code artifacts; public brand deliberately deferred to launch; brand changes never touch code.
- Monorepo ([ADR 0006](adr/0006-product-monorepo.md)) with lowercase area roots (backend / frontend / gpu-worker / shared / infra / docs / probes), one root solution, project-per-service cut (Api, AppCore, Dispatcher, Provisioner, Maintenance, Contracts, GpuWorker), per-project DI self-registration.
- POC code is copied in directly when implementation starts (no shared libraries); this repo becomes the source of truth.

## Testing Decisions

From the testing-strategy ticket ([#14](https://github.com/Astral100/Scalex/issues/14)):

- **Integration-first** suite against a real PostgreSQL (Testcontainers) covering a curated important-logic list; no coverage percentage target. Good tests exercise external behavior through the API surface, never implementation details.
- **Seams** (everything inside them runs for real):
  - **Dispatcher seam** — a scripted fake stands in for GPU capacity on the outbound side; the test scripts its behavior (accept, fail, stall).
  - **Worker inbound: no seam** — tests play the worker, calling the real heartbeat/progress/completion endpoints, attempt-tagged like the real agent.
  - **Storage** — an in-memory fake behind the storage interface; MinIO is a later option for realism.
  - **Sign-ins** — use the identity provider's standard testing practices; which ones is looked up when implementation reaches it (deliberately not decided at project level).
- Prediction logic is tested by **trace replay** — the POC's curated trace set is carried over as the seed regression net.
- Process: bug fixes land fail-first; important-logic features ship with tests by default (the user may defer or waive); sessions show real test output; CI runs the suite as information, never as a deploy gate; hybrid test projects per [#20](https://github.com/Astral100/Scalex/issues/20) (shared integration-test project + per-project unit tests).
- Manual user-driven live smoke testing with an optional checklist complements the suite.

## Out of Scope

- **Billing / payments / credits** — its own follow-on effort, **required before public launch**; the seams it will attach to are already specified (user record, submit gate, balance-covers-estimate assumption).
- **The frontend website** — only its contract (API v1, presets, polling) is specified here.
- **Changes to the POC repo** (`Astral100/SeedVr`).
- **Multipart / >2 GB uploads, output browsing UI extras** — recorded in [v2-features](v2-features.md).
- **Batch processing** (`BatchJob`), **per-user quotas and rate limiting**, **admin back office** — designed seams exist; deliberately post-v1.
- **The serverless orchestration path** — fully designed, kept as the dispatcher-seam alternative, not built.

## Further Notes

### Implementation opening checklist

Eleven live verifications form the opening of the implementation phase ([#17 resolution](https://github.com/Astral100/Scalex/issues/17), [#23 resolution](https://github.com/Astral100/Scalex/issues/23)); the provisioner/autoscaler is explicitly among the first things built:

1. Programmatic rent: search offers → create instance with our image → Ready.
2. The .NET agent inside the real image, reachable through remapped ports; heartbeats/progress end-to-end.
3. SeedVR2 nodes on latest stable ComfyUI; CRF/bit-depth honored (POC app as rig).
4. Measured cold starts and pull times per image size; same-host GHCR-vs-R2 speed check.
5. **Required, not optional**: fp16-vs-fp8 A/B on our own content (seam artifacts) + wall-clock comparison; the result flips the preset default, not the API.
6. Large-layer push/pull through the R2-backed registry (known equal-chunk-size fix).
7. Live-verify update→recycle semantics (immediate vs deferred apply).
8. Measure recycle pull times / layer-cache survival on a real host.
9. Verify download-progress parsing from instance logs during a boot.
10. Configure registry pulls with long-lived credentials (or anonymous read).
11. Confirm destroyed-machine offer-reappearance behavior (fallback path).

### Post-v1 priorities (explicit user priorities)

- **int8-ConvRot models**: adopt once validated — high priority.
- **Staging environment**: planned fast-follow after launch (v1 ships with a post-release smoke job instead).

### Decided-to-decide-later (carried from the map's fog)

- Port of the POC ETA/progress estimator, including per-machine learned priors and a worker-acquisition phase in estimates — required for v1, detailed design at implementation.
- The preset catalogue contents (which presets exist, their values) — needed before launch; informed by the model-lineup validation below.
- Model-lineup validation before launch: structured side-by-side grid (content classes × models, incl. a fast GAN-class upscaler) to set the launch model set, SeedVR2's tier and pricing.
- Data retention / lifecycle policy values (hooks are all in place: per-file expiry stamps, deletion pass, direction-rooted keys).
- Scale-to-1000s delta review if the 1000s-ready architecture materially raises effort versus the launch design.
- Validation of the job-type abstraction against a concrete second job type.

### Reading order for implementation sessions

1. This spec, then [CONTEXT.md](../CONTEXT.md) (the vocabulary used everywhere).
2. [`docs/api-contract-v1.md`](api-contract-v1.md) for anything client-facing.
3. The ADRs in [`docs/adr/`](adr/) for any area being touched — they are the decision authorities.
4. The linked ticket resolutions for full reasoning when a gist here is not enough.
