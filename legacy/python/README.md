# Archived terminal prototype

The active game is MonoGame/C# at the repository root. This directory preserves the original Python simulation and terminal interface for comparison. It does not load the bitmap assets and has its own save format.

Requires Python 3.10+ with curses on macOS/Linux. Run these commands **from this directory**:

```sh
./play.sh
./play.sh --load
./play.sh --headless 700 --demo
python3 -m unittest discover -v
```

Its default save remains `~/.regolith/colony.json`; the C# game uses `~/.regolith/colony-csharp.json`. There is no conversion between them.
