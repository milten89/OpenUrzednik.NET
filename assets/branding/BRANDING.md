# OpenUrzędnik.NET branding

The spec for the project's icons and logo. `branding.toml` holds every number, color, label and tagline, and `generate.py` builds every asset from it. This file explains the design; when a number here and in `branding.toml` disagree, `branding.toml` wins.

## Idea

The icon is a **queue ticket** (*numerek*), the slip you pull when you enter a Polish government office. The number on it is `200`, the HTTP status for success. It's a small joke: with this library you're served on the first call. The provider icons put the provider's short name where the number is, so the NuGet listing shows which office you are queuing at.

The design is flat on purpose: no gradients, shadows or outlines. It has to read at 64 px on nuget.org.

**Never use the Polish state emblem (the white eagle) or anything resembling it.** Polish law restricts its use, and it would suggest an official project. This project is unofficial.

## Files

| File | Content | Used by |
|---|---|---|
| `icon.svg`, `icon.png` | Base ticket: `OU.NET` / `200` | NuGet icon of Core, Extensions.Logging, Diagnostics |
| `icon-<provider>.svg`, `icon-<provider>.png` | Base ticket with the provider name in place of `200` | NuGet icon of the provider and its integration packages (`icon-nbp` → `Nbp`, `Nbp.DependencyInjection`) |
| `favicon.svg`, `favicon-32.png` | Ticket without text | Anything at 32 px or less |
| `header-light.svg`, `header-dark.svg` | Ticket + wordmark | Root `README.md` / `README.pl.md` |
| `social-preview.svg`, `social-preview.png` | 1280×640 card, Polish tagline | GitHub repository social preview (uploaded by hand) |
| `social-preview-en.svg`, `social-preview-en.png` | 1280×640 card, English tagline | The same, for an English-facing upload |
| `branding.toml` | Every number, color, label and tagline | `generate.py` |
| `generate.py` | Generator script | Builds every SVG and PNG above |

`branding.toml` is the source: `generate.py` writes each SVG from it and renders each PNG from its SVG. Text in every SVG is converted to outlines (paths), so the files never depend on installed fonts.

### Package → icon

| Package | Icon |
|---|---|
| `OpenUrzednik.Core`, `.Extensions.Logging`, `.Diagnostics` | `icon.png` |
| `OpenUrzednik.Nbp`, `OpenUrzednik.Nbp.DependencyInjection` | `icon-nbp.png` |

A provider gets its icon when it becomes ready (ADR-0010), by adding its entry to `branding.toml`.

Rule: shared infrastructure gets the base icon. A provider package and every package built on it (`*.DependencyInjection`, future adapters) get that provider's icon. The root `Directory.Build.props` packs the file named by the `OpenUrzednikPackageIcon` property as `icon.png` (default `icon.png`). A provider csproj and each package built on it set `<OpenUrzednikPackageIcon>icon-<provider>.png</OpenUrzednikPackageIcon>`. When you add a provider icon, also add its PNG to `pathFilters` in `version.json` and to the `paths` filter of `.github/workflows/nuget-pre-release.yml` (ADR-0008).

## Colors

| Token | Hex | Where |
|---|---|---|
| Ticket purple | `#512BD4` | Ticket body, `.NET` in the light wordmark. The .NET brand purple |
| Polish red | `#C8102E` | Strip at the top of the ticket, stripe on the social preview |
| Lilac | `#D9D2FA` | `OU.NET` on the ticket |
| White | `#FFFFFF` | `200` / provider name, perforation |
| Ink | `#1F1F1F` | Wordmark on light backgrounds |
| Grey | `#5F5E5A` | Tagline on the social preview |
| Paper | `#FAFAF7` | Social preview background |
| Dark ink | `#E6EDF3` | Wordmark on dark backgrounds (GitHub dark text color) |
| Dark purple | `#9B87F5` | `.NET` on dark backgrounds (`#512BD4` is too dark on `#0D1117`) |

The ticket itself keeps the same colors in light and dark mode.

## Typography

One family everywhere: **Cascadia Mono** (Microsoft, SIL Open Font License 1.1, <https://github.com/microsoft/cascadia-code>). It's a monospace font from the .NET world, and it has Polish glyphs (`ę` in the wordmark). The current files were made from the variable font `CascadiaMono.ttf` 2102.025, instanced at the weight shown below.

| Text | Weight | Size (in its own coordinate system) |
|---|---|---|
| `OU.NET` on the ticket | 700 | 15 |
| `200` / provider name | 700 | 30 (see label rules) |
| Wordmark `OpenUrzędnik.NET` | 700 | 40 (header), 72 (social preview) |
| Tagline | 400 | 30 (social preview) |

Metrics: 2048 units per em, advance width 1200 for every glyph (0.586 em), cap height 1420 (0.693 em). Text has no letter-spacing and no kerning.

The wordmark is written **`OpenUrzędnik.NET`**, with `ę`. Package IDs and code stay ASCII (`OpenUrzednik`). In the wordmark, `OpenUrzędnik` uses the ink color and `.NET` the purple, with no space between them.

## Icon geometry

Coordinate system: `viewBox="0 0 120 120"`, exported at 128×128. Every number below is in viewBox units.

```
 x: 22                    98
y14 ┌──────────────────────┐   red strip   y 14–20  (#C8102E)
    ├──────────────────────┤
    │        OU.NET        │   baseline 44, size 15, centered on x 60 (#D9D2FA)
y50 │                      │
   (  - - - - - - - - - -   )  notches: r 8, centers (22,58) and (98,58)
y66 │                      │   perforation y 58, x 32–88, 2 wide, dash 4 gap 4 (#FFF)
    │         200          │   baseline 94, size 30, centered on x 60 (#FFF)
y106└──────────────────────┘
```

Ticket outline (fill `#512BD4`):

```
M22 14 H98 V50 A8 8 0 0 0 98 66 V106 H22 V66 A8 8 0 0 0 22 50 Z
```

- Ticket: 76 wide, 92 tall, square corners. The two semicircular notches (radius 8) cut into the sides at mid-height (y 50–66).
- Red strip: `rect x=22 y=14 width=76 height=6`, drawn over the ticket.
- Perforation: line from (32, 58) to (88, 58), white, stroke width 2, `stroke-dasharray="4 4"`, which gives 7 dashes.
- Text is centered horizontally on x = 60 by its advance width: `x_start = 60 − (characters × 0.586 × size) / 2`. Monospace, so the advance width equals the box.
- Margins: 22 left and right, 14 top and bottom. Keep them, don't scale the ticket up to the edge. NuGet and GitHub crop or round corners.

### Label rules (bottom half)

The bottom label must stay **at most 56 units wide**, which leaves at least 10 units to each side of the ticket.

| Characters | Size | Width |
|---|---|---|
| 1–3 (`200`, `NBP`, `MF`) | 30 | ≤ 52.7 |
| 4 (e.g. `KSeF`) | 23.5 | 55.1 |
| 5 (e.g. `CEIDG`) | 19 | 55.7 |

Formula: `size = min(30, 56 / (characters × 0.586))`, rounded down to 0.5. The baseline stays at 94 for every size. Uppercase is the default. Use the institution's own casing when it has one (`KSeF`). No more than 5 characters: use the institution's common abbreviation, never a shortened word.

### Favicon (≤ 32 px)

At 32 px and below, text turns to noise, so the favicon is just the shape. It uses a slightly bigger ticket and a thicker strip so it stays readable:

```
viewBox 0 0 120 120
ticket  M14 10 H106 V48 A12 12 0 0 0 106 72 V110 H14 V72 A12 12 0 0 0 14 48 Z   #512BD4
strip   rect x=14 y=10 width=92 height=16                                         #C8102E
```

No perforation, no text. Every package and provider shares the same favicon.

## README header

- Canvas height 96. Width = 104 + wordmark width + 4, rounded up to a whole unit (483 for the current wordmark).
- Ticket: the full icon group scaled by 0.8 at the origin, so it is about 61×74 inside x 17.6–78.4, y 11.2–84.8.
- Wordmark: starts at x 104, baseline 62, size 40, weight 700.
- No background (transparent). Two files: `header-light.svg` (ink `#1F1F1F`, `.NET` `#512BD4`) and `header-dark.svg` (ink `#E6EDF3`, `.NET` `#9B87F5`), switched with `<picture>` and `prefers-color-scheme`.
- No tagline: the header is shared by the Polish and the English README.

## Social preview (1280×640)

- Background `#FAFAF7`. A white band (y 0–16) and a red band (y 16–32) at the top: the Polish flag as a thin stripe.
- Ticket: the base icon group, `translate(64 176) scale(2.4)` (ticket body at x 116.8–299.2, y 209.6–430.4).
- Wordmark: x 392, baseline 322, size 72, weight 700, ink + purple.
- Tagline: `Klienci .NET do polskich API publicznych`, x 392, baseline 392, size 30, weight 400, `#5F5E5A`.
- Two cards, identical except for the tagline: `social-preview` (Polish, above) and `social-preview-en` (`.NET clients for Polish public APIs`, for an English-facing upload). The maintainer chooses which one to upload.
- GitHub crops the edges in some views: keep content at least 40 px from every edge.

## Adding a provider icon

1. Add an entry to `[icons]` in `branding.toml`: `file = "icon-<provider>"` (lowercase) and `label`, following the label rules. The script sizes and centers the label and sets `<title>` to `OpenUrzędnik.NET <LABEL>`.
2. Run `uv run assets/branding/generate.py`. It writes `icon-<provider>.svg` and renders `icon-<provider>.png` at 128×128.
3. Check it at 128, 64 and 32 px next to `icon.png`: the label must not touch the notches.
4. Wire it as described in "Package → icon": `OpenUrzednikPackageIcon` in the csproj, the PNG in `pathFilters` in `version.json` and in the `paths` filter of `nuget-pre-release.yml`, and the row in the table there.

## Generating the assets

```bash
uv run assets/branding/generate.py          # write every SVG and PNG listed in branding.toml
uv run assets/branding/generate.py --check  # generate into a temporary directory; exit 1 and list the files that differ
```

The script is a uv script with pinned dependencies, locked with hashes in `generate.py.lock`: `fonttools` outlines the text and `resvg-py` (resvg) renders the PNGs. It downloads the font archive named in `branding.toml` once into a cache outside the repository (`%LOCALAPPDATA%\openurzednik-branding` on Windows, `$XDG_CACHE_HOME/openurzednik-branding` or `~/.cache/openurzednik-branding` elsewhere), checks its SHA-256, and fails if the font version differs from the one pinned there. Output is deterministic: running it twice gives the same bytes, so `--check` passes on an unchanged tree. Render sizes: icons 128, favicon 32, social preview 1280. SVGs are UTF-8 with LF line endings (`<title>` contains `ę`).

## Don'ts

- No gradients, shadows, glows, rounded ticket corners or outlines around the ticket.
- Don't recolor the ticket per provider. The name is the only difference.
- Don't put live `<text>` in an SVG. Outline it.
- Don't add the state emblem, the flag as a shape, or the logos of the institutions (NBP, GUS, …). Their names as plain text are fine.
- Don't change `200` on the base icon or `OU.NET` on any icon.
