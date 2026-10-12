# Internal project name Scalex; public brand deferred

Decided on the code-layout ticket (#20, 2026-10-02) and revised post-close (Vexel → PixUp 2026-10-09 → decoupled and settled 2026-10-12). All code artifacts carry the **internal project name Scalex**: repository `Scalex`, solution `Scalex.sln`, every project `Scalex.*`, local folder `D:\Programming\Scalex` (layout in ADR 0017). The **public brand is deliberately undecided** — it will be chosen closer to launch, and the decoupling makes that choice a zero-cost event for the codebase: a brand change touches the website and this ADR, never the repository, solution, folders, or code. The repository rename and the local move (repo folder relocated to `D:\Programming\Scalex`, emptied `PixUp` parent folder deleted; Claude project-context re-keyed for continuity) run as one bundled activity.

**The brand direction is settled even though the name is not**: brand-over-keyword, per the go-to-market economics research ([`docs/research/naming-gtm-economics.md`](../research/naming-gtm-economics.md)). Disclosed revenue in this market sits entirely on the brand side (Magnific, Topaz); keyword-domain sites show traffic but no money on pricing that doesn't survive per-minute GPU video costs; the one-brand-many-offerings roadmap rules out keyword names. Keyword-style landing pages inside the brand site do the traffic work instead.

**SeedVR2 is launch content, never identity**: a job type, presets, model files, an offering page — nothing infrastructural carries the model's name. The model-name domains are taken by competitors anyway, and the name belongs to ByteDance.

**Brand candidates kept warm** (checked during #20; re-verify collisions before committing to one):

- **PixUp** (`pixup.tv`) — briefly the decided name (2026-10-09 to 2026-10-12); known in-area collision: "Photo Enhancer: PixUp", an existing AI photo enhancer.
- **Vexel** (`vexel.media`) — the original #20 choice; invented, nothing in upscaling uses it, but crowded elsewhere: vexel.com is a crypto platform, a Vexel TTS Mac app exists, and "vexel" is an established digital-art term that pollutes bare-word search.
- **Murro** — verified clean (no software use found 2026-10-02).
- **Scalex** (`scalex.tv`) — promoting the internal name to the brand remains an option.

## Considered Options

- Naming the product now (Vexel, then PixUp were both decided and reverted within ten days) — rejected in favor of decoupling: the brand decision has no dependency on the remaining planning work, and churn was leaking into code naming.
- Keyword/descriptive domain (imgupscaler.ai style) — rejected for the identity (kept for landing pages): keyword-locked, clone-exposed, no trademark protection possible, misfits the umbrella roadmap.
- SeedVR2-derived name/domain — rejected: not ours to own, chained to one model's lifespan, and the good domains are already squatted.
