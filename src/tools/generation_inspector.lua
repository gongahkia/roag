-- Compatibility import: level-facing developer UI now lives in level_editor/
-- beside its standalone LÖVE entry point. Keep this public module path for
-- headless tooling/tests and the legacy `love . --generation-inspector` mode.
return require("level_editor.generation_inspector")
