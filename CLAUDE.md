@AGENTS.md

## Claude Code only
- Editor work: use the `/unity:*` skills from the Unity plugin and the `unity` CLI (`unity recompile .`, `unity command test run --mode editor`, `unity command`).
- Use plan mode for any change that touches more than one module.
- Use subagents for broad searches across the repo or the GDD; keep the main context for the change itself.
- Repo skills: `/issue-start`, `/verify`, `/issue-finish`, `/review-pr`, `/design-change`, `/gdd-lookup`.
