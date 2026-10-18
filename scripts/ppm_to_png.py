#!/usr/bin/env python3
"""Convert the sandbox's RGB PPM screenshots to PNG using only Python's stdlib."""
from pathlib import Path
import struct
import sys
import zlib


def convert(source, target):
    with open(source, 'rb') as file:
        if file.readline() != b'P6\n':
            raise ValueError('Expected a binary RGB PPM')
        width, height = map(int, file.readline().split())
        if file.readline() != b'255\n':
            raise ValueError('Expected 8-bit pixels')
        pixels = file.read()
    if len(pixels) != width * height * 3:
        raise ValueError('Invalid image size')
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
    raw = b''.join(b'\0' + pixels[y * width * 3:(y + 1) * width * 3] for y in range(height))
    output = b'\x89PNG\r\n\x1a\n'
    output += chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))
    output += chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b'')
    Path(target).write_bytes(output)


if __name__ == '__main__':
    if len(sys.argv) != 3:
        raise SystemExit('Usage: ppm_to_png.py INPUT.ppm OUTPUT.png')
    convert(*sys.argv[1:])
