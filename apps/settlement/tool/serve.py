#!/usr/bin/env python3
"""Serve the release at its real deployment prefix, not a file URL."""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse
import errno
import os

parser = argparse.ArgumentParser()
parser.add_argument('--port', type=int, default=8765)
parser.add_argument('--base-path', default='/settlement/')
parser.add_argument('--build-dir', type=Path)
args = parser.parse_args()
if not args.base_path.startswith('/') or not args.base_path.endswith('/'):
    raise SystemExit('--base-path must start and end with /')
build = (args.build_dir or (Path(__file__).resolve().parents[1] / 'build/web')).resolve()
if not (build/'index.html').exists():
    raise SystemExit('Build first: flutter build web --release --base-href /settlement/ --no-web-resources-cdn')


class Handler(SimpleHTTPRequestHandler):
    def route(self):
        if self.path == '/':
            self.send_response(302)
            self.send_header('Location', args.base_path)
            self.end_headers()
            return False
        if not self.path.startswith(args.base_path):
            self.send_error(404)
            return False
        self.path = self.path[len(args.base_path)-1:]
        return True

    def do_GET(self):
        if self.route():
            super().do_GET()

    def do_HEAD(self):
        if self.route():
            super().do_HEAD()


os.chdir(build)
try:
    server = ThreadingHTTPServer(('127.0.0.1', args.port), Handler)
except OSError as error:
    if error.errno != errno.EADDRINUSE:
        raise
    raise SystemExit(
        f'Port {args.port} is already in use. If the settlement server is already running, '
        f'open http://127.0.0.1:{args.port}{args.base_path}\n'
        'To start another copy on a free port, use --port 0.'
    ) from None

with server:
    print(f'ROAG settlement: http://127.0.0.1:{server.server_port}{args.base_path}', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
