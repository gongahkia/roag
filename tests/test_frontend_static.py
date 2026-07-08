from __future__ import annotations

import unittest

from kenjaku.frontend_static import (
    FRONTEND_STATIC_HELPERS_VERSION,
    assert_no_browser_network,
    badge,
    html_document,
    line_chart_svg,
    sortable_header,
    sortable_table_script,
    static_base_css,
)


class FrontendStaticTests(unittest.TestCase):
    def test_html_document_writes_local_self_contained_shell(self) -> None:
        html = html_document(
            title="Fixture <Title>",
            body_html="<main>ok</main>",
            stylesheets=("styles.css",),
            scripts=("demo.js",),
        )

        self.assertTrue(html.startswith("<!doctype html>"))
        self.assertIn(FRONTEND_STATIC_HELPERS_VERSION, html)
        self.assertIn("<title>Fixture &lt;Title&gt;</title>", html)
        self.assertIn('href="styles.css"', html)
        self.assertIn('src="demo.js"', html)
        self.assertNotIn("http://", html)
        self.assertNotIn("https://", html)

    def test_html_document_rejects_remote_references_and_inline_network_tokens(self) -> None:
        with self.assertRaisesRegex(ValueError, "must be local"):
            html_document(title="bad", body_html="", stylesheets=("https://example.test/a.css",))
        with self.assertRaisesRegex(ValueError, "browser network token"):
            html_document(title="bad", body_html="", inline_css=("@import 'x.css';",))
        with self.assertRaisesRegex(ValueError, "browser network token"):
            html_document(title="bad", body_html='<img src="https://example.test/a.png">')

    def test_static_css_exposes_theme_and_helper_contracts(self) -> None:
        css = static_base_css(include_theme=True)
        self.assertIn("--kj-table-felt", css)
        self.assertIn(".kj-static-wrap", css)
        self.assertIn(".kj-badge", css)
        self.assertIn(".kj-sort-button", css)

    def test_sortable_table_helpers_escape_labels_and_target_table(self) -> None:
        header = sortable_header(2, "Eval <Score>", "number")
        script = sortable_table_script("fixture-table")

        self.assertIn("Eval &lt;Score&gt;", header)
        self.assertIn('data-sort-column="2"', header)
        self.assertIn('"fixture-table"', script)
        self.assertIn("compareStaticNumber", script)

    def test_badge_and_line_chart_are_static_markup(self) -> None:
        self.assertEqual(
            badge("Passed <ok>", tone="success"),
            '<span class="kj-badge kj-badge--success">Passed &lt;ok&gt;</span>',
        )
        chart = line_chart_svg([(1.0, 0.25), (2.0, 0.5)], label="eval chart")
        self.assertIn('role="img"', chart)
        self.assertIn('aria-label="eval chart"', chart)
        self.assertIn("<polyline", chart)
        assert_no_browser_network(chart)


if __name__ == "__main__":
    unittest.main()
