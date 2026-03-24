#!/usr/bin/env python3
import argparse
import functools
import http.server
import mimetypes
import socketserver
from pathlib import Path


class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # Basic CORS for local testing convenience.
        self.send_header("Access-Control-Allow-Origin", "*")
        super().end_headers()

    def guess_type(self, path):
        p = path.lower()
        if p.endswith(".wasm") or p.endswith(".wasm.br") or p.endswith(".wasm.gz"):
            return "application/wasm"
        return super().guess_type(path)

    def send_head(self):
        path = self.translate_path(self.path)
        fpath = Path(path)
        if fpath.is_file():
            ctype = self.guess_type(path)
            try:
                f = open(path, "rb")
            except OSError:
                self.send_error(404, "File not found")
                return None

            self.send_response(200)
            self.send_header("Content-type", ctype)
            self.send_header("Content-Length", str(fpath.stat().st_size))
            self.send_header("Cache-Control", "no-store")

            if path.endswith(".br"):
                self.send_header("Content-Encoding", "br")
            elif path.endswith(".gz"):
                self.send_header("Content-Encoding", "gzip")

            self.end_headers()
            return f

        return super().send_head()


def main():
    parser = argparse.ArgumentParser(description="Serve Unity WebGL build with proper headers.")
    parser.add_argument("--dir", default="Build", help="Directory to serve (default: Build)")
    parser.add_argument("--port", type=int, default=8080, help="Port to bind (default: 8080)")
    args = parser.parse_args()

    root = Path(args.dir).resolve()
    if not root.exists():
        raise SystemExit(f"Directory not found: {root}")

    handler = functools.partial(UnityWebGLHandler, directory=str(root))
    with socketserver.TCPServer(("127.0.0.1", args.port), handler) as httpd:
        print(f"Serving {root} at http://127.0.0.1:{args.port}")
        httpd.serve_forever()


if __name__ == "__main__":
    mimetypes.add_type("application/wasm", ".wasm")
    main()
