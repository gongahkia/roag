"""Read the complete catalog from its tracked gzip or an explicit JSON export."""
import gzip
import json
from pathlib import Path

DEFAULT_CATALOG = Path(__file__).resolve().parents[1] / 'assets/catalog.json.gz'


def load_catalog(path=DEFAULT_CATALOG):
    path = Path(path)
    opener = gzip.open if path.suffix == '.gz' else open
    with opener(path, 'rt', encoding='utf-8') as stream:
        return json.load(stream)
