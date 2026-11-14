#!/usr/bin/env python3
"""Serve the release at its real deployment prefix, not a file URL."""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse
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
print(f'ROAG settlement: http://127.0.0.1:{args.port}/settlement/', flush=True)
ThreadingHTTPServer(('127.0.0.1', args.port), Handler).serve_forever()
