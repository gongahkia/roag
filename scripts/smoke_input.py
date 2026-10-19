#!/usr/bin/env python3
"""Isolated X11 input fixtures for the native graphical smoke suite."""
import ctypes
import ctypes.util
import struct
import subprocess
import time

from build import ROOT
from ppm_to_png import convert


def interactive(binary, env, directory, collisions=False):
    """Exercise actual GLFW input and resize events, isolated on the X server."""
    xlib = ctypes.CDLL(ctypes.util.find_library('X11'))
    xtest_name = ctypes.util.find_library('Xtst')
    if not xtest_name:
        raise RuntimeError('X11 input smoke requires libXtst (libxtst6 on Ubuntu)')
    xtest = ctypes.CDLL(xtest_name)
    xfixes_name = ctypes.util.find_library('Xfixes')
    if not xfixes_name:
        raise RuntimeError('Cursor smoke requires libXfixes (libxfixes3 on Ubuntu)')
    xfixes = ctypes.CDLL(xfixes_name)
    ptr, ulong, uint = ctypes.c_void_p, ctypes.c_ulong, ctypes.c_uint

    class CursorImage(ctypes.Structure):
        _fields_ = [('x', ctypes.c_short), ('y', ctypes.c_short),
                    ('width', ctypes.c_ushort), ('height', ctypes.c_ushort),
                    ('xhot', ctypes.c_ushort), ('yhot', ctypes.c_ushort),
                    ('serial', ulong), ('pixels', ctypes.POINTER(ulong)),
                    ('atom', ulong), ('name', ctypes.c_char_p)]

    xfixes.XFixesGetCursorImage.argtypes = [ptr]
    xfixes.XFixesGetCursorImage.restype = ctypes.POINTER(CursorImage)
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
    save = directory / ('collision.vox' if collisions else 'interactive.vox')
    settings = directory / ('collision-settings.cfg' if collisions else 'settings.cfg')
    if collisions:
        blocks = {(x, 180, z): 3 for x in range(-12, 13) for z in range(-12, 13)}
        blocks.update({(2, y, z): 3 for y in range(181, 185) for z in range(-12, 13)})
        blocks.update({(x, y, -4): 3 for y in range(181, 185) for x in range(-12, 13)})
        body = b''.join(struct.pack('<iiiB', *position, block) for position, block in blocks.items())
        save.write_bytes(struct.pack('<4sQQdddII', b'VOX2', 42, len(blocks), .5, 182.62, .5, len(blocks), 2) + body)
    command = [str(binary), '--save', str(save), '--settings', str(settings), '--radius', '2', '--seconds', '32', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360']
    if not collisions:
        command.append('--spectator')  # Existing free-flight/editing coverage.
    command += ['--generator', '2']  # Preserve the established input fixture's terrain.
    process = subprocess.Popen(command, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
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

        def cursor_is_visible():
            # Window screenshots omit the OS pointer. Inspect its actual ARGB
            # image instead, so a captured arrow cannot pass as a crosshair.
            cursor = xfixes.XFixesGetCursorImage(display)
            assert cursor, 'Cannot inspect the native cursor'
            try:
                image = cursor.contents
                return any(image.pixels[i] >> 24 & 255 for i in range(image.width * image.height))
            finally:
                xlib.XFree(cursor)

        def focus(target):
            xlib.XSetInputFocus(display, target, 1, 0)
            xlib.XSync(display, 0)
            time.sleep(.2)

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

        if collisions:
            def saved_position():
                key(0xffc2)
                return struct.unpack_from('<ddd', save.read_bytes(), 20)

            initial = saved_position()
            assert abs(initial[1] - 182.62) < .001, initial
            key(ord('w'), 1.4)
            wall = saved_position()
            assert 1.69 < wall[0] <= 1.7001 and wall[2] < -.5, f'Wall collision/slide failed: {wall}'
            assert abs(wall[1] - 182.62) < .001, f'Floor collision failed: {wall}'
            before = struct.unpack_from('<I', save.read_bytes(), 44)[0]
            click(3); saved_position()
            assert struct.unpack_from('<I', save.read_bytes(), 44)[0] == before, 'Placed a solid block inside the walking player'
            key(0x20, .12)  # Space: jump, then inspect a save before landing.
            jumping = saved_position()
            assert jumping[1] > 183.1, f'Jump failed: {jumping}'
            time.sleep(.9)
            landed = saved_position()
            assert abs(landed[1] - 182.62) < .001, f'Landing failed: {landed}'
            key(0xffc1)  # F4: spectator.
            key(0xffe3, .35)  # Ctrl passes down through the platform.
            below = saved_position()
            assert below[1] < 180, f'Spectator did not noclip: {below}'
            key(0x20, (180.7 - below[1]) / 14)  # Move into the platform, then leave spectator.
            key(0xffc1)
            recovered = saved_position()
            assert recovered[1] >= 182.619, f'Leaving spectator did not clear the solid floor: {recovered}'
            capture('collision-walk', 640, 360)
            key(ord('q'))
            output = process.communicate(timeout=5)[0]
            assert process.returncode == 0 and 'OpenGL error' not in output, output
            print('PASS default collisions, wall sliding, jump/landing, safe placement, and spectator toggling', flush=True)
            return

        time.sleep(.2)
        assert not cursor_is_visible(), 'OS cursor is visible during gameplay'
        # Move in both directions while looking, allowing GLFW to recenter in
        # between. This keeps the existing editing fixture's view unchanged.
        for x, y in ((340, 190), (300, 170)):
            xtest.XTestFakeMotionEvent(display, -1, x, y, 0); xlib.XSync(display, 0)
            time.sleep(.15)
            assert not cursor_is_visible(), 'OS cursor appeared during mouse look'
        key(0xffc2)
        before_focus = save.read_bytes()
        focus(root)
        xtest.XTestFakeMotionEvent(display, -1, 100, 100, 0); xlib.XSync(display, 0)
        time.sleep(.15)
        assert cursor_is_visible(), 'Focus loss did not restore the OS cursor'
        key(ord('w'), .2)
        focus(window)
        assert not cursor_is_visible(), 'Focus regain did not hide the OS cursor'
        key(0xffc2)
        assert save.read_bytes() == before_focus, 'Unfocused input moved the player or edited the world'

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
        assert cursor_is_visible(), 'Settings did not restore the OS cursor'
        focus(root); focus(window)
        assert cursor_is_visible(), 'Settings recaptured the pointer after focus regain'
        key(0xff53)  # Right: keyboard movement speed 14 -> 17.5.
        key(0xff54); key(0xff53)  # Down/right: mouse sensitivity 1 -> 1.1.
        key(ord('w'), 0.2); click(1); click(3)  # Menu must consume gameplay input.
        key(0xffc2)
        paused = save.read_bytes()
        assert paused == data, 'Menu input moved the player or edited the world'
        key(0xff0d)  # Enter resumes and saves settings immediately.
        assert not cursor_is_visible(), 'Resuming left the OS cursor visible'
        assert settings.exists(), 'Settings were not saved on resume'
        saved_settings = settings.read_text()
        assert 'move_speed 17.500000' in saved_settings, saved_settings
        assert 'mouse_sensitivity 1.100000' in saved_settings, saved_settings

        # Actual pointer input at the minimum supported size: row 1 plus button.
        xlib.XResizeWindow(display, window, 320, 240); xlib.XSync(display, 0); time.sleep(0.3)
        key(0xffbe)  # F1 opens settings.
        xtest.XTestFakeMotionEvent(display, -1, 280, 86, 0); xlib.XSync(display, 0)
        click(1)
        assert cursor_is_visible(), 'Settings clicks hid the OS cursor'
        capture('settings-small', 320, 240)
        key(0xff0d)
        assert not cursor_is_visible(), 'F1/resume left the OS cursor visible'
        assert 'mouse_sensitivity 1.200000' in settings.read_text(), settings.read_text()

        xlib.XResizeWindow(display, window, 1024, 768); xlib.XSync(display, 0); time.sleep(0.3)
        xlib.XResizeWindow(display, window, 640, 360); xlib.XSync(display, 0); time.sleep(0.3)
        key(0xff1b)
        capture('settings-menu', 640, 360)
        key(0xff1b)  # Resume before the normal Q shortcut.
        key(0xff09); key(ord('1')); click(3); key(0xffc2)  # Tab, coal ore, place, save.
        ore_save = save.read_bytes()
        ore_blocks = [ore_save[i + 12] for i in range(52, len(ore_save), 13)]
        assert 8 in ore_blocks, 'The second palette page did not place coal ore'
        for _ in range(4):
            key(0xff09)
        key(ord('7')); click(3); time.sleep(0.5); key(0xffc2)
        water_save = save.read_bytes()
        water_blocks = [water_save[i + 12] for i in range(52, len(water_save), 13)]
        assert 16 in water_blocks, 'The water slot did not place a persistent source'
        # Reach page five and place material 55: exercises the sixth material bit.
        for _ in range(4):
            key(0xff09)
        key(ord('6')); click(3); key(0xffc2)
        biome_save = save.read_bytes()
        assert 55 in [biome_save[i + 12] for i in range(52, len(biome_save), 13)], 'Last palette page failed to place swamp grass'
        key(0xff09)  # Wrap page five back to one.
        key(ord('q'))
        output = process.communicate(timeout=5)[0]
        assert process.returncode == 0, output
        assert 'OpenGL error' not in output, output
        assert cursor_is_visible(), 'Exit left the OS cursor hidden'
        print('PASS native cursor hiding during look, focus loss/regain, menus, resume, and exit', flush=True)
        print('PASS editing, ore/water palette, settings input, persistence, paused controls, resize, and quit', flush=True)
    finally:
        if process.poll() is None:
            process.kill(); process.communicate()
        xlib.XCloseDisplay(display)
