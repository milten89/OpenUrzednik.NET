# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "fonttools==4.67.0",
#     "resvg-py==0.5.0",
# ]
#
# [tool.uv]
# exclude-newer = "2026-10-30T00:00:00Z"
# ///
"""Generate the OpenUrzędnik.NET branding assets from branding.toml.

    uv run assets/branding/generate.py          # write every SVG and PNG
    uv run assets/branding/generate.py --check  # exit 1 if a working-tree file differs

The font archive is downloaded once into a cache outside the repository
(%LOCALAPPDATA%\\openurzednik-branding on Windows, otherwise
$XDG_CACHE_HOME/openurzednik-branding or ~/.cache/openurzednik-branding)
and verified against the SHA-256 pinned in branding.toml.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import math
import os
import sys
import tempfile
import tomllib
import urllib.request
import zipfile
from collections import Counter
from pathlib import Path
from xml.sax.saxutils import escape

import resvg_py
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

HERE = Path(__file__).resolve().parent
CONFIG = HERE / "branding.toml"
SVG_NS = "http://www.w3.org/2000/svg"


def num(value: float) -> str:
    """Format a number with at most two decimals and no trailing zeros."""
    text = f"{value:.2f}".rstrip("0").rstrip(".")
    return "0" if text == "-0" else text


# --- font -------------------------------------------------------------------


def cache_dir() -> Path:
    if os.name == "nt" and os.environ.get("LOCALAPPDATA"):
        base = Path(os.environ["LOCALAPPDATA"])
    else:
        base = Path(os.environ.get("XDG_CACHE_HOME") or Path.home() / ".cache")
    return base / "openurzednik-branding"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def font_archive(cfg: dict) -> Path:
    """Return the cached font archive, downloading it when missing or corrupt."""
    directory = cache_dir()
    archive = directory / cfg["url"].rsplit("/", 1)[-1]
    expected = cfg["sha256"].lower()
    if archive.exists() and sha256(archive) == expected:
        return archive
    directory.mkdir(parents=True, exist_ok=True)
    print(f"Downloading {cfg['url']}", file=sys.stderr)
    partial = archive.with_name(archive.name + ".part")
    request = urllib.request.Request(
        cfg["url"], headers={"User-Agent": "openurzednik-branding"}
    )
    try:
        with (
            urllib.request.urlopen(request, timeout=120) as response,
            partial.open("wb") as out,
        ):
            while chunk := response.read(1 << 20):
                out.write(chunk)
        actual = sha256(partial)
        if actual != expected:
            sys.exit(
                f"SHA-256 mismatch for {cfg['url']}: expected {expected}, got {actual}"
            )
        partial.replace(archive)
    finally:
        partial.unlink(missing_ok=True)
    return archive


class Font:
    """One instance of the variable font, able to turn text into SVG path data."""

    def __init__(self, data: bytes, weight: int) -> None:
        self.ttf = instantiateVariableFont(TTFont(io.BytesIO(data)), {"wght": weight})
        self.glyphs = self.ttf.getGlyphSet()
        self.cmap = self.ttf.getBestCmap()
        self.upm = self.ttf["head"].unitsPerEm

    def _glyph(self, char: str):
        if ord(char) not in self.cmap:
            sys.exit(f"The font has no glyph for {char!r}")
        return self.glyphs[self.cmap[ord(char)]]

    def width(self, text: str, size: float) -> float:
        """Advance width of the text at the given size (no kerning, no letter-spacing)."""
        return sum(self._glyph(c).width for c in text) * size / self.upm

    def path(
        self, text: str, x: float, baseline: float, size: float
    ) -> tuple[str, float]:
        """Outline the text starting at x; return the path data and the x after it."""
        k = size / self.upm
        pen = SVGPathPen(self.glyphs, ntos=num)
        for char in text:
            glyph = self._glyph(char)
            glyph.draw(
                TransformPen(pen, (k, 0, 0, -k, x, baseline))
            )  # font units are y-up
            x += glyph.width * k
        return pen.getCommands(), x


def load_fonts(cfg: dict) -> dict[int, Font]:
    with zipfile.ZipFile(font_archive(cfg)) as archive:
        data = archive.read(cfg["member"])
    fonts = {weight: Font(data, weight) for weight in (cfg["bold"], cfg["regular"])}
    version = f"{fonts[cfg['bold']].ttf['head'].fontRevision:.3f}"
    if version != cfg["version"]:
        sys.exit(
            f"Font version {version} does not match branding.toml ({cfg['version']})"
        )
    return fonts


# --- SVG building -----------------------------------------------------------


def element(name: str, **attrs: str | float) -> str:
    """A self-closing element; attributes keep the order given (trailing _ and _ → -)."""
    parts = [name]
    for key, value in attrs.items():
        key = key.rstrip("_").replace("_", "-")
        parts.append(
            f'{key}="{num(value) if isinstance(value, (int, float)) else value}"'
        )
    return "<" + " ".join(parts) + "/>"


def rect(x: float, y: float, width: float, height: float, fill: str) -> str:
    attrs: dict[str, str | float] = {}
    if x:
        attrs["x"] = x
    if y:
        attrs["y"] = y
    return element("rect", **attrs, width=width, height=height, fill=fill)


def svg(width: float, height: float, viewbox: str, title: str, body: list[str]) -> str:
    head = (
        f'<svg xmlns="{SVG_NS}" width="{num(width)}" height="{num(height)}" '
        f'viewBox="{viewbox}" role="img"><title>{escape(title)}</title>'
    )
    return head + "".join(body) + "</svg>\n"


def ticket_outline(
    x: float, y: float, width: float, height: float, r: float, notch_y: float
) -> str:
    right, bottom, top_notch, bottom_notch = (
        x + width,
        y + height,
        notch_y - r,
        notch_y + r,
    )
    a = f"A{num(r)} {num(r)} 0 0 0"
    return (
        f"M{num(x)} {num(y)}H{num(right)}V{num(top_notch)}{a} {num(right)} {num(bottom_notch)}"
        f"V{num(bottom)}H{num(x)}V{num(bottom_notch)}{a} {num(x)} {num(top_notch)}Z"
    )


class Generator:
    def __init__(self, config: dict, fonts: dict[int, Font]) -> None:
        self.c = config
        self.bold = fonts[config["font"]["bold"]]
        self.regular = fonts[config["font"]["regular"]]

    def color(self, token: str) -> str:
        return self.c["colors"][token]

    def label_size(self, label: str) -> float:
        rule = self.c["ticket"]["label"]
        if not 1 <= len(label) <= rule["max_chars"]:
            sys.exit(f"Label {label!r} must have 1 to {rule['max_chars']} characters")
        size = min(rule["max_size"], rule["max_width"] / self.bold.width(label, 1))
        return math.floor(size / rule["size_step"]) * rule["size_step"]

    def centered(
        self, font: Font, text: str, center_x: float, baseline: float, size: float
    ) -> str:
        return font.path(text, center_x - font.width(text, size) / 2, baseline, size)[0]

    def ticket(self, label: str) -> list[str]:
        t, top, lab = (
            self.c["ticket"],
            self.c["ticket"]["top_text"],
            self.c["ticket"]["label"],
        )
        outline = ticket_outline(
            t["x"], t["y"], t["width"], t["height"], t["notch_radius"], t["notch_y"]
        )
        perforation = f"M{num(t['perforation_x1'])} {num(t['notch_y'])}H{num(t['perforation_x2'])}"
        top_path = self.centered(
            self.bold, top["text"], top["center_x"], top["baseline"], top["size"]
        )
        label_path = self.centered(
            self.bold, label, lab["center_x"], lab["baseline"], self.label_size(label)
        )
        return [
            element("path", fill=self.color(t["fill"]), d=outline),
            rect(
                t["x"],
                t["y"],
                t["width"],
                t["strip_height"],
                self.color(t["strip_fill"]),
            ),
            element(
                "path",
                stroke=self.color(t["perforation_stroke"]),
                stroke_width=t["perforation_width"],
                stroke_dasharray=t["perforation_dash"],
                d=perforation,
            ),
            element("path", fill=self.color(top["color"]), d=top_path),
            element("path", fill=self.color(lab["color"]), d=label_path),
        ]

    def base_ticket(self) -> list[str]:
        """The ticket of the icon marked base = true, reused by headers and cards."""
        base = next(icon for icon in self.c["icons"]["icon"] if icon.get("base"))
        return self.ticket(base["label"])

    def wordmark(
        self, x: float, baseline: float, size: float, ink: str, accent: str
    ) -> tuple[list[str], float]:
        body = []
        for part, token in zip(self.c["brand"]["wordmark"], (ink, accent), strict=True):
            d, x = self.bold.path(part, x, baseline, size)
            body.append(element("path", fill=self.color(token), d=d))
        return body, x

    def files(self) -> dict[str, tuple[str, tuple[str, int] | None]]:
        """Map each SVG file name (no extension) to its content and its PNG (name, width), if any."""
        c, name = self.c, self.c["brand"]["name"]
        box = num(c["ticket"]["viewbox"])
        viewbox = f"0 0 {box} {box}"
        out: dict[str, tuple[str, tuple[str, int] | None]] = {}

        size = c["icons"]["render_size"]
        for icon in c["icons"]["icon"]:
            title = (
                f"{name} {icon['label']}" if icon.get("label_in_title", True) else name
            )
            out[icon["file"]] = (
                svg(size, size, viewbox, title, self.ticket(icon["label"])),
                (icon["file"], size),
            )

        f, t = c["favicon"], c["ticket"]
        favicon = [
            element(
                "path",
                fill=self.color(t["fill"]),
                d=ticket_outline(
                    f["x"],
                    f["y"],
                    f["width"],
                    f["height"],
                    f["notch_radius"],
                    f["notch_y"],
                ),
            ),
            rect(
                f["x"],
                f["y"],
                f["width"],
                f["strip_height"],
                self.color(t["strip_fill"]),
            ),
        ]
        out[f["file"]] = (
            svg(f["render_size"], f["render_size"], viewbox, name, favicon),
            (f["png"], f["render_size"]),
        )

        h = c["header"]
        group = (
            f'<g transform="scale({num(h["icon_scale"])})">'
            + "".join(self.base_ticket())
            + "</g>"
        )
        for variant in h["variant"]:
            words, end = self.wordmark(
                h["wordmark_x"],
                h["wordmark_baseline"],
                h["wordmark_size"],
                variant["ink"],
                variant["accent"],
            )
            width = math.ceil(round(end + h["padding_right"], 6))
            body = [group, *words]
            out[variant["file"]] = (
                svg(width, h["height"], f"0 0 {width} {num(h['height'])}", name, body),
                None,
            )

        s = c["social"]
        tx, ty = s["icon_translate"]
        group = (
            f'<g transform="translate({num(tx)} {num(ty)}) scale({num(s["icon_scale"])})">'
            + "".join(self.base_ticket())
            + "</g>"
        )
        words, _ = self.wordmark(
            s["text_x"],
            s["wordmark_baseline"],
            s["wordmark_size"],
            s["wordmark_ink"],
            s["wordmark_accent"],
        )
        for card in s["card"]:
            tagline, _ = self.regular.path(
                card["tagline"], s["text_x"], s["tagline_baseline"], s["tagline_size"]
            )
            body = [
                rect(0, 0, s["width"], s["height"], self.color(s["background"])),
                *(
                    rect(0, y, s["width"], height, self.color(token))
                    for y, height, token in s["bands"]
                ),
                group,
                *words,
                element("path", fill=self.color(s["tagline_color"]), d=tagline),
            ]
            viewbox_s = f"0 0 {num(s['width'])} {num(s['height'])}"
            out[card["file"]] = (
                svg(s["width"], s["height"], viewbox_s, name, body),
                (card["file"], s["width"]),
            )
        return out


def render_png(svg_text: str, width: int) -> bytes:
    return bytes(resvg_py.svg_to_bytes(svg_string=svg_text, width=width))


def validate(config: dict) -> None:
    """Fail on definitions that can't produce a consistent set of files."""
    icons = config["icons"]["icon"]
    bases = [icon["file"] for icon in icons if icon.get("base")]
    if len(bases) != 1:
        sys.exit(f"Exactly one icon must have base = true, found {len(bases)}: {bases}")
    favicon = config["favicon"]
    names = [
        *(f"{icon['file']}.{ext}" for icon in icons for ext in ("svg", "png")),
        f"{favicon['file']}.svg",
        f"{favicon['png']}.png",
        *(f"{variant['file']}.svg" for variant in config["header"]["variant"]),
        *(
            f"{card['file']}.{e}"
            for card in config["social"]["card"]
            for e in ("svg", "png")
        ),
    ]
    counts = Counter(name.lower() for name in names)
    duplicates = sorted(name for name, count in counts.items() if count > 1)
    if duplicates:
        sys.exit(
            f"Duplicate output file names in branding.toml: {', '.join(duplicates)}"
        )


def build(directory: Path) -> dict[str, bool]:
    """Write every asset into directory; map each file name to whether its bytes changed."""
    with CONFIG.open("rb") as stream:
        config = tomllib.load(stream)
    validate(config)
    generator = Generator(config, load_fonts(config["font"]))
    result = {}
    for stem, (text, png) in generator.files().items():
        outputs = {f"{stem}.svg": text.encode("utf-8")}
        if png is not None:
            outputs[f"{png[0]}.png"] = render_png(text, png[1])
        for file_name, data in outputs.items():
            target = directory / file_name
            result[file_name] = not target.exists() or target.read_bytes() != data
            if result[file_name]:
                target.write_bytes(data)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--check",
        action="store_true",
        help="generate into a temporary directory; fail if a working-tree file differs",
    )
    args = parser.parse_args()
    if not args.check:
        for file_name, changed in build(HERE).items():
            print(f"{'wrote    ' if changed else 'unchanged'} {file_name}")
        return 0
    with tempfile.TemporaryDirectory() as temp:
        names = list(build(Path(temp)))
        changed = [
            n
            for n in names
            if not (HERE / n).exists()
            or (HERE / n).read_bytes() != (Path(temp) / n).read_bytes()
        ]
    for file_name in changed:
        print(f"differs: {file_name}")
    generated = {p.name for p in HERE.iterdir() if p.suffix in (".svg", ".png")}
    for file_name in sorted(generated - set(names)):
        print(f"warning: no entry in branding.toml produces {file_name}")
    print(f"{len(names) - len(changed)} of {len(names)} files up to date")
    return 1 if changed else 0


if __name__ == "__main__":
    sys.exit(main())
