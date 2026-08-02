# Documentation index

Subify OS keeps **two layers** of docs on purpose:

1. **Public / English** — for GitHub visitors, recruiters, and self-hosters (start here).  
2. **Maintainer memory** — detailed product & backlog notes (often **Turkish**). Nothing is deleted; it is just not the front door.

---

## Start here (English)

| Doc | Description |
| --- | ----------- |
| [../README.md](../README.md) | Project showcase, features, screenshots, quick start |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Clean Architecture, request flow, auth, jobs |
| [OPS.md](./OPS.md) | Install, backup/restore, upgrade, troubleshooting |
| [../docker/README.md](../docker/README.md) | Compose, env vars, healthchecks |
| [screenshots/](./screenshots/) | Live UI captures used in the root README |
| [API_CONTRACTS.md](./API_CONTRACTS.md) | API payload notes |
| [DATA_MODEL.md](./DATA_MODEL.md) | Data model notes |
| [TESTING_STRATEGY.md](./TESTING_STRATEGY.md) | How we think about tests |
| [LOGGING_MONITORING.md](./LOGGING_MONITORING.md) | Logging / monitoring notes |
| [ERROR_CODES.md](./ERROR_CODES.md) · [ERROR_CODES_OS.md](./ERROR_CODES_OS.md) | Error code references |
| [diagrams/](./diagrams/) | Component, deployment, ERD, sequences |

---

## Maintainer memory (mostly Turkish)

> These files are the **project brain**: decisions, backlog, future SaaS path.  
> They stay in the repo for continuity. They are **not** linked from the root README so the public surface stays English and scannable.

| Doc | Language | Role |
| --- | -------- | ---- |
| [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md) | TR | Product constitution (self-host, no freemium) |
| [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md) | TR | OS product requirements |
| [SUBIFY_OS_TASK_LIST.md](./SUBIFY_OS_TASK_LIST.md) | TR | Implementation checklist / sprint memory |
| [SUBIFY_SAAS_TRANSITION_PRD.md](./SUBIFY_SAAS_TRANSITION_PRD.md) | TR | Future Cloud product PRD (not OS runtime) |
| [SUBIFY_SAAS_TRANSITION_TASK_LIST.md](./SUBIFY_SAAS_TRANSITION_TASK_LIST.md) | TR | Cloud tasks `S0`–`S9` |
| [SEED_DATA.md](./SEED_DATA.md) | TR/mixed | Seed notes |
| [UI_MOCKUPS.md](./UI_MOCKUPS.md) | mixed | Mockup index (legacy design refs) |
| [ADR.md](./ADR.md) | mixed | Architecture decision records |
| [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md) | TR/EN | Payment config — **OS has no payments**; Cloud reference |

### Legacy (keep for history — do not treat as current OS source of truth)

| Doc | Note |
| --- | ---- |
| [Subify.Web.Uygulamasi.v2.PRD.md](./Subify.Web.Uygulamasi.v2.PRD.md) | Old freemium SaaS PRD |
| [SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md](./SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md) | Old task list |
| [mockups/](./mockups/) | Early design images (README uses **live** [screenshots/](./screenshots/) instead) |

### Local-only / prefer not to highlight

| File | Note |
| ---- | ---- |
| `UsedCommand.txt` | Personal command scrapbook — avoid relying on it in PRs; do not treat as product docs |

---

## Language policy (for humans and agents)

| Audience | Language |
| -------- | -------- |
| Root README, ARCHITECTURE, public “how to run” | **English** |
| Day-to-day product memory, task lists, SaaS planning | **Turkish OK** (do not delete) |
| New **public-facing** pages | Prefer English |
| Full dual-language copies of every PRD | **Not required** unless external contributors demand it |

**Conflict order (OS product):**

1. [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md)  
2. [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md)  
3. [SUBIFY_OS_TASK_LIST.md](./SUBIFY_OS_TASK_LIST.md)  
4. Other docs  

**Cloud / billing work:** use SaaS transition docs — do not add paywalls to the OS task list.

---

## For recruiters / portfolio readers

You do not need the Turkish backlog. Read:

1. Root [README](../README.md) (what was built + screenshots)  
2. [ARCHITECTURE.md](./ARCHITECTURE.md) (how it is structured)  
3. Skim `api/` and `web/` layout + tests under `api/*Tests/`  

That is enough to evaluate the engineering signal of this repository.
