from __future__ import annotations

import contextlib
import io
import re
import unittest
from pathlib import Path

DOC = Path("docs/api-cookbook.md")
PYTHON_BLOCK = re.compile(r"```python\n(.*?)\n```", re.DOTALL)


class DocsSnippetsTests(unittest.TestCase):
    def test_api_cookbook_snippets_execute(self) -> None:
        blocks = PYTHON_BLOCK.findall(DOC.read_text(encoding="utf-8"))

        self.assertGreaterEqual(len(blocks), 5)
        self.assertLessEqual(len(blocks), 8)
        for index, code in enumerate(blocks, start=1):
            with self.subTest(snippet=index):
                namespace = {
                    "__file__": str(DOC),
                    "__name__": f"api_cookbook_snippet_{index}",
                }
                with contextlib.redirect_stdout(io.StringIO()):
                    exec(compile(code, f"{DOC}#snippet-{index}", "exec"), namespace)


if __name__ == "__main__":
    unittest.main()
