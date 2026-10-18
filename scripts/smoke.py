#!/usr/bin/env python3
"""Graphical, persistence, input, resizing, and memory-pressure smoke checks."""
import argparse
import csv
import ctypes
import ctypes.util
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import time

sys.dont_write_bytecode = True

import benchmark
import build
from ppm_to_png import convert

ROOT = build.ROOT


def run(binary, env, args, expected=0):
    result = subprocess.run([str(binary), *args], env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=30)
    if result.returncode != expected:
        raise AssertionError(f'{args}: exit {result.returncode}\n{result.stdout}')
    return result.stdout


def interactive(binary, env, directory):
    """Exercise actual GLFW input and resize events, isolated on the X server."""
    xlib = ctypes.CDLL(ctypes.util.find_library('X11'))
    xtest_name = ctypes.util.find_library('Xtst')
    if not xtest_name:
        raise RuntimeError('X11 input smoke requires libXtst (libxtst6 on Ubuntu)')
    xtest = ctypes.CDLL(xtest_name)
    ptr, ulong, uint = ctypes.c_void_p, ctypes.c_ulong, ctypes.c_uint
    xlib.XOpenDisplay.argtypes = [ctypes.c_char_p]; xlib.XOpenDisplay.restype = ptr
    xlib.XDefaultRootWindow.argtypes = [ptr]; xlib.XDefaultRootWindow.restype = ulong
    xlib.XQueryTree.argtypes = [ptr, ulong, ctypes.POINTER(ulong), ctypes.POINTER(ulong), ctypes.POINTER(ctypes.POINTER(ulong)), ctypes.POINTER(uint)]
    xlib.XFetchName.argtypes = [ptr, ulong, ctypes.POINTER(ctypes.c_char_p)]
    xlib.XFree.argtypes = [ptr]
    xlib.XSetInputFocus.argtypes = [ptr, ulong, ctypes.c_int, ulong]
    xlib.XResizeWindow.argtypes = [ptr, ulong, uint, uint]
    xlib.XGetImage.argtypes = [ptr, ulong, ctypes.c_int, ctypes.c_int, uint, uint, ulong, ctypes.c_int]
    xlib.XGetImage.restype = ptr
    xlib.XGetPixel.argtypes = [ptr, ctypes.c_int, ctypes.c_int]; xlib.XGetPixel.restype = ulong
    xlib.XDestroyImage.argtypes = [ptr]
    xlib.XSync.argtypes = [ptr, ctypes.c_int]
    xlib.XCloseDisplay.argtypes = [ptr]
    xlib.XKeysymToKeycode.argtypes = [ptr, ulong]; xlib.XKeysymToKeycode.restype = ctypes.c_ubyte
    xtest.XTestFakeKeyEvent.argtypes = [ptr, uint, ctypes.c_int, ulong]
    xtest.XTestFakeButtonEvent.argtypes = [ptr, uint, ctypes.c_int, ulong]
    xtest.XTestFakeMotionEvent.argtypes = [ptr, ctypes.c_int, ctypes.c_int, ctypes.c_int, ulong]
    display = xlib.XOpenDisplay(env['DISPLAY'].encode())
    if not display:
        raise RuntimeError('Cannot connect input test to X server')
    save = directory / 'interactive.vox'
    settings = directory / 'settings.cfg'
    process = subprocess.Popen([str(binary), '--save', str(save), '--settings', str(settings), '--radius', '2', '--seconds', '25', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360'], env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    try:
        root = xlib.XDefaultRootWindow(display)
        window = None
        deadline = time.monotonic() + 10
        while time.monotonic() < deadline:
            root_out, parent_out, count = ulong(), ulong(), uint()
            children = ctypes.POINTER(ulong)()
            xlib.XQueryTree(display, root, ctypes.byref(root_out), ctypes.byref(parent_out), ctypes.byref(children), ctypes.byref(count))
            for index in range(count.value):
                name = ctypes.c_char_p()
                if xlib.XFetchName(display, children[index], ctypes.byref(name)):
                    if name.value and name.value.startswith(b'VOXEL'):
                        window = children[index]
                    xlib.XFree(name)
            if children:
                xlib.XFree(children)
            if window:
                break
            if process.poll() is not None:
                raise AssertionError(process.communicate()[0])
            time.sleep(0.05)
        assert window, 'GLFW window was never created'
        time.sleep(0.4)
        xlib.XSetInputFocus(display, window, 1, 0)
        xlib.XSync(display, 0)

        def key(symbol, duration=0.12):
            code = xlib.XKeysymToKeycode(display, symbol)
            xtest.XTestFakeKeyEvent(display, code, 1, 0); xlib.XSync(display, 0)
            time.sleep(duration)
            xtest.XTestFakeKeyEvent(display, code, 0, 0); xlib.XSync(display, 0)
            time.sleep(0.08)

        def click(button):
            xtest.XTestFakeButtonEvent(display, button, 1, 0); xlib.XSync(display, 0)
            time.sleep(0.10)
            xtest.XTestFakeButtonEvent(display, button, 0, 0); xlib.XSync(display, 0)
            time.sleep(0.15)

        def capture(name, width, height):
            # Read the test window only; never capture the user's desktop.
            image = xlib.XGetImage(display, window, 0, 0, width, height, ulong(-1), 2)
            assert image, 'Cannot capture settings window'
            try:
                rgb = bytearray()
                for y in range(height):
                    for x in range(width):
                        pixel = xlib.XGetPixel(image, x, y)
                        rgb.extend(((pixel >> 16) & 255, (pixel >> 8) & 255, pixel & 255))
                ppm = directory / f'{name}.ppm'
                ppm.write_bytes(f'P6\n{width} {height}\n255\n'.encode() + rgb)
                convert(ppm, ROOT / 'build' / f'{name}.png')
            finally:
                xlib.XDestroyImage(image)

        key(ord('w'), 0.2)
        key(0xffe3, 0.85)  # Left control: descend toward the terrain.
        time.sleep(0.4)
        click(1)
        click(3)
        key(0xffc2)  # F5: save while the process remains alive.
        deadline = time.monotonic() + 3
        while not save.exists() and time.monotonic() < deadline:
            time.sleep(0.05)
        assert save.exists(), 'F5 did not save'
        data = save.read_bytes()
        position = struct.unpack_from('<ddd', data, 20)
        edits = struct.unpack_from('<I', data, 44)[0]
        assert position[0] > 8.5 and position[2] < 7.5, f'Movement failed: {position}'
        assert edits > 0, 'Mouse block editing did not produce an edit'

        key(0xff1b)  # Escape opens settings and releases capture.
        key(0xff53)  # Right: keyboard movement speed 14 -> 17.5.
        key(0xff54); key(0xff53)  # Down/right: mouse sensitivity 1 -> 1.1.
        key(ord('w'), 0.2); click(1); click(3)  # Menu must consume gameplay input.
        key(0xffc2)
        paused = save.read_bytes()
        assert paused == data, 'Menu input moved the player or edited the world'
        key(0xff0d)  # Enter resumes and saves settings immediately.
        assert settings.exists(), 'Settings were not saved on resume'
        saved_settings = settings.read_text()
        assert 'move_speed 17.500000' in saved_settings, saved_settings
        assert 'mouse_sensitivity 1.100000' in saved_settings, saved_settings

        # Actual pointer input at the minimum supported size: row 1 plus button.
        xlib.XResizeWindow(display, window, 320, 240); xlib.XSync(display, 0); time.sleep(0.3)
        key(0xffbe)  # F1 opens settings.
        xtest.XTestFakeMotionEvent(display, -1, 280, 86, 0); xlib.XSync(display, 0)
        click(1)
        capture('settings-small', 320, 240)
        key(0xff0d)
        assert 'mouse_sensitivity 1.200000' in settings.read_text(), settings.read_text()

        xlib.XResizeWindow(display, window, 1024, 768); xlib.XSync(display, 0); time.sleep(0.3)
        xlib.XResizeWindow(display, window, 640, 360); xlib.XSync(display, 0); time.sleep(0.3)
        key(0xff1b)
        capture('settings-menu', 640, 360)
        key(0xff1b)  # Resume before the normal Q shortcut.
        key(ord('q'))
        output = process.communicate(timeout=5)[0]
        assert process.returncode == 0, output
        assert 'OpenGL error' not in output, output
        print('PASS editing, settings keyboard/mouse input, persistence, paused controls, resize, and quit', flush=True)
    finally:
        if process.poll() is None:
            process.kill(); process.communicate()
        xlib.XCloseDisplay(display)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--skip-build', action='store_true')
    parser.add_argument('--skip-input', action='store_true', help='Skip Linux XTest interaction checks')
    args = parser.parse_args()
    os.chdir(ROOT)
    if not args.skip_build:
        subprocess.run([sys.executable, 'scripts/build.py', 'build'], check=True)
    binary = ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    env = build.environment()
    server = None
    if sys.platform.startswith('linux') and shutil.which('Xvfb'):
        # Keep test windows off the user's desktop, even in a normal WSL session.
        number = next(n for n in range(91, 120) if not Path(f'/tmp/.X11-unix/X{n}').exists())
        server = subprocess.Popen(['Xvfb', f':{number}', '-screen', '0', '1024x768x24', '-nolisten', 'tcp'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        env['DISPLAY'] = f':{number}'
        env.pop('WAYLAND_DISPLAY', None)
        env['LIBGL_ALWAYS_SOFTWARE'] = '1'
        time.sleep(0.4)
    try:
        with tempfile.TemporaryDirectory(dir=ROOT / 'build') as temporary:
            directory = Path(temporary)
            ppm = directory / 'smoke.ppm'
            run(binary, env, ['--hidden', '--no-save', '--radius', '2', '--frames', '90', '--screenshot', str(ppm)])
            rgb = ppm.read_bytes().split(b'\n', 3)[3]
            assert len({rgb[i:i+3] for i in range(0, len(rgb), 96)}) > 64, 'Screenshot contains insufficient scene detail'
            convert(ppm, ROOT / 'build' / 'smoke.png')
            print('PASS graphical rendering and screenshot', flush=True)
            save = directory / 'world.vox'
            settings = directory / 'startup-settings.cfg'
            run(binary, env, ['--hidden', '--save', str(save), '--settings', str(settings), '--radius', '1', '--frames', '10'])
            assert save.read_bytes().startswith(b'VOX1')
            output = run(binary, env, ['--hidden', '--save', str(save), '--settings', str(settings), '--seed', '999', '--radius', '1', '--frames', '10'])
            assert 'Loaded' in output
            assert struct.unpack_from('<Q', save.read_bytes(), 4)[0] == 42
            save.write_bytes(b'invalid-existing-save')
            run(binary, env, ['--hidden', '--save', str(save), '--frames', '1'], expected=1)
            assert save.read_bytes() == b'invalid-existing-save'
            print('PASS save/reload and preservation of corrupt saves', flush=True)
            for flags in (['--gpu-mb', '0'], ['--radius', '0'], ['--profile', 'invalid'], ['--workers', '99'], ['--frames', '-1'], ['--fps-limit', '999']):
                run(binary, env, flags, expected=2)
            print('PASS invalid configuration rejection', flush=True)
            if sys.platform.startswith('linux') and not args.skip_input:
                interactive(binary, env, directory)
            # Tiny scene isolates the software limiter from render load. Measured
            # intervals must include its wait and agree with wall-clock duration.
            run(binary, env, ['--hidden', '--benchmark', 'normal', '--seconds', '3', '--radius', '1', '--width', '320', '--height', '240', '--fps-limit', '30'])
            path = ROOT / 'benchmarks' / 'normal.csv'
            with path.open() as file:
                rows = list(csv.DictReader(file))
            assert all(None not in row and None not in row.values() for row in rows), 'Misaligned CSV fields'
            times = [float(row['frame_ms']) for row in rows]
            assert all(ms >= 1000 / 30 - 0.1 for ms in times), 'Measured FPS excluded the cap wait'
            assert any(float(row['pacing_wait_ms']) > 1 for row in rows), 'Limiter never waited'
            assert all(int(row['frame_cap']) == 30 for row in rows)
            wall = float(rows[-1]['elapsed_s'])
            assert abs(sum(times) / 1000 - wall) < 0.01, 'Completed frame intervals disagree with elapsed time'
            steady = [row for row in rows if float(row['elapsed_s']) > 2]
            assert all(0 < float(row['fps']) <= 30.1 for row in steady), 'HUD FPS was not measured at the cap'
            path.replace(path.with_name('smoke-frame-cap.csv'))
            print('PASS measured FPS, 30 FPS pacing, swap timing, and aligned CSV', flush=True)
            run(binary, env, ['--hidden', '--benchmark', 'dense', '--seconds', '5', '--radius', '4', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360', '--upload-kb', '64'])
            path = ROOT / 'benchmarks' / 'dense.csv'
            summary = benchmark.summarize(path, warmup=1)
            assert summary['pressure_events'] > 0, 'Pressure scenario never exercised eviction'
            assert summary['final_radius'] < 4
            target = path.with_name('smoke-pressure.csv'); path.replace(target)
            target.with_suffix('.json').write_text(json.dumps(summary, indent=2) + '\n')
            print('PASS memory pressure, automatic draw-distance reduction, and bounded uploads', flush=True)
    finally:
        if server:
            server.terminate(); server.wait(timeout=5)


if __name__ == '__main__':
    main()
