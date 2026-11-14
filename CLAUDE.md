@AGENTS.md

## Claude Code only
- Editor work: use the `/unity:*` skills from the Unity plugin and the `unity` CLI (`unity recompile .`, `unity test .`, `unity command`).
- Use plan mode for any change that touches more than one module.
- Use subagents for broad searches across the repo or the GDD; keep the main context for the change itself.
- Repo skills: `/issue-start`, `/verify`, `/issue-finish`, `/review-pr`, `/design-change`, `/gdd-lookup`.
- `.claude/rules/ccgs/` holds general defaults from Claude Code Game Studios; this repo's `AGENTS.md` wins on conflict.
