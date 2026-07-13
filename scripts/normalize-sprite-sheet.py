#!/usr/bin/env python3
import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--columns', type=int, required=True)
    parser.add_argument('--rows', type=int, required=True)
    parser.add_argument('--colors', type=int, default=8)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert('RGBA')
    sheet = Image.new('RGBA', (args.columns * 16, args.rows * 16))
    for row in range(args.rows):
        for column in range(args.columns):
            left = round(column * source.width / args.columns)
            top = round(row * source.height / args.rows)
            right = round((column + 1) * source.width / args.columns)
            bottom = round((row + 1) * source.height / args.rows)
            cell = source.crop((left, top, right, bottom)).resize((16, 16), Image.Resampling.NEAREST)
            cell = cell.quantize(colors=args.colors, method=Image.Quantize.FASTOCTREE).convert('RGBA')
            sheet.alpha_composite(cell, (column * 16, row * 16))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.output, optimize=True)


if __name__ == '__main__':
    main()
