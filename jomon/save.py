from __future__ import annotations
import json
from pathlib import Path
from .state import GameState, game_state_from_dict
def save_game(state:GameState,path:Path)->Path:
    path=Path(path); path.parent.mkdir(parents=True,exist_ok=True); path.write_text(json.dumps(state.to_dict(),sort_keys=True,indent=2)+"\n",encoding="utf-8"); return path
def load_game(path:Path)->GameState: return game_state_from_dict(json.loads(Path(path).read_text(encoding="utf-8")))
