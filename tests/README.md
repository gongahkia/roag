# Tests

Run the headless deterministic suite from the repository root with the command
luajit tests/run.lua.

The suite intentionally depends only on LuaJIT. It exercises the simulation,
generation, and RNG modules without creating a LÖVE window.
