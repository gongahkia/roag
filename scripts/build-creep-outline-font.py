#!/usr/bin/env python3
"""Build a Chromium-compatible outline face from Creep's bundled bitmap strike.

Usage:
  uv run --with fonttools scripts/build-creep-outline-font.py

The upstream Creep source is bitmap-only. Chromium Canvas 2D accepts its
embedded bitmap face but paints blank glyphs. This copies the source pixels
into square TrueType outlines at the original 16-pixel grid, without changing
their shapes.
"""

from pathlib import Path

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "public/fonts/Creep.ttf"
OUTPUT = ROOT / "public/fonts/Creep-outline.ttf"
UNITS_PER_EM = 1024
PIXEL_UNITS = UNITS_PER_EM // 16


def source_metrics(font: TTFont) -> dict[str, object]:
    """Collect the shared bitmap metrics used by Creep's 16-pixel strike."""
    result: dict[str, object] = {}
    for subtable in font["EBLC"].strikes[0].indexSubTables:
        metrics = getattr(subtable, "metrics", None)
        if metrics is not None:
            result.update({name: metrics for name in subtable.names})
    return result


def outline_for(bitmap: bytes, metrics: object):
    pen = TTGlyphPen(None)
    width = metrics.width
    height = metrics.height
    bits = "".join(f"{byte:08b}" for byte in bitmap)
    for row in range(height):
        for column in range(width):
            if bits[row * width + column] != "1":
                continue
            left = (metrics.horiBearingX + column) * PIXEL_UNITS
            bottom = (metrics.horiBearingY - row - 1) * PIXEL_UNITS
            right = left + PIXEL_UNITS
            top = bottom + PIXEL_UNITS
            pen.moveTo((left, bottom))
            pen.lineTo((right, bottom))
            pen.lineTo((right, top))
            pen.lineTo((left, top))
            pen.closePath()
    return pen.glyph()


def main() -> None:
    source = TTFont(SOURCE)
    glyph_order = [*source.getGlyphOrder(), "space"]
    strike = source["EBDT"].strikeData[0]
    bitmap_metrics = source_metrics(source)
    glyphs = {}
    for name in source.getGlyphOrder():
        bitmap = strike[name].data
        metrics = bitmap_metrics.get(name)
        glyphs[name] = outline_for(bitmap, metrics) if metrics is not None else TTGlyphPen(None).glyph()
    glyphs["space"] = TTGlyphPen(None).glyph()

    scale = UNITS_PER_EM / source["head"].unitsPerEm
    horizontal_metrics = {
        name: (round(advance * scale), round(bearing * scale))
        for name, (advance, bearing) in source["hmtx"].metrics.items()
    }
    horizontal_metrics["space"] = (round(source["hmtx"]["A"][0] * scale), 0)
    character_map = dict(source.getBestCmap())
    character_map[0x20] = "space"

    builder = FontBuilder(UNITS_PER_EM, isTTF=True)
    builder.setupGlyphOrder(glyph_order)
    builder.setupCharacterMap(character_map)
    builder.setupGlyf(glyphs)
    builder.setupHorizontalMetrics(horizontal_metrics)
    builder.setupHorizontalHeader(ascent=896, descent=-128)
    builder.setupNameTable({
        "familyName": "Creep",
        "styleName": "Regular",
        "uniqueFontIdentifier": "Creep outline browser build 1.1.0",
        "fullName": "Creep",
        "psName": "CreepOutline"
    })
    builder.setupOS2(
        sTypoAscender=896,
        sTypoDescender=-128,
        usWinAscent=896,
        usWinDescent=128,
        xAvgCharWidth=426,
        fsSelection=0x40
    )
    builder.setupPost()
    builder.setupMaxp()
    builder.save(OUTPUT)


if __name__ == "__main__":
    main()
