#!/usr/bin/env python3
"""Serve the release at its real deployment prefix, not a file URL."""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse
import errno
import os

parser = argparse.ArgumentParser()
parser.add_argument('--port', type=int, default=8765)
args = parser.parse_args()
build = Path(__file__).resolve().parents[1] / 'build/web'
if not (build/'index.html').exists():
    raise SystemExit('Build first: flutter build web --release --base-href /settlement/ --no-web-resources-cdn')


class Handler(SimpleHTTPRequestHandler):
    def route(self):
        if self.path == '/':
            self.send_response(302)
            self.send_header('Location', '/settlement/')
            self.end_headers()
            return False
        if not self.path.startswith('/settlement/'):
            self.send_error(404)
            return False
        self.path = self.path[len('/settlement'):]
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
        f'open http://127.0.0.1:{args.port}/settlement/\n'
        'To start another copy on a free port, use --port 0.'
    ) from None

with server:
    print(f'ROAG settlement: http://127.0.0.1:{server.server_port}/settlement/', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
