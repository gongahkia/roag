#!/usr/bin/env python3
"""Exercise the offline atlas through a real browser and inspect downloaded files.

Optional development dependency, separate from normal catalog generation:
  uv run --with playwright python tools/verify_asset_atlas.py
  # Install Chromium once with: uv run --with playwright playwright install chromium
"""
import argparse
import base64
import hashlib
import json
from pathlib import Path
from urllib.parse import urlencode

from playwright.sync_api import sync_playwright

from asset_catalog import Png, neutral_record
from query_assets import matches, parser as query_parser, sort_key

ROOT = Path(__file__).resolve().parents[1]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--browser', type=Path, help='Optional existing Chromium executable.')
    p.add_argument('--output', type=Path, default=ROOT / 'artifacts/atlas-verification')
    args = p.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    catalog = json.loads((ROOT / 'assets/catalog.json').read_text())
    records = {r['id']: r for r in catalog['sprites']}
    checks, errors, requests = [], [], []
    url = (ROOT / 'assets/atlas.html').as_uri()

    with sync_playwright() as pw:
        browser = pw.chromium.launch(executable_path=str(args.browser) if args.browser else None)
        page = browser.new_page(viewport={'width': 1440, 'height': 1000}, accept_downloads=True)
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.on('request', lambda request: requests.append(request.url) if request.url.startswith(('http:', 'https:')) else None)
        page.goto(url)
        page.locator('.tile').first.wait_for()

        def check(ok, label):
            if not ok:
                raise AssertionError(label)
            checks.append(label)

        def expected(argv):
            query = query_parser().parse_args(argv)
            return sorted((r for r in records.values() if matches(r, query)),
                          key=lambda r: sort_key(r, query.sort), reverse=query.descending)

        def download(selector, filename):
            with page.expect_download() as event:
                page.locator(selector).click()
            path = args.output / filename
            event.value.save_as(path)
            return path

        def verify_selection(argv, label):
            wanted = expected(argv)
            actual = json.loads(download('#export-results', 'selection.json').read_text())
            check([r['id'] for r in actual['matches']] == [r['id'] for r in wanted], label)
            check(actual['total'] == len(wanted), label + ' count')
            check(all(r == neutral_record(records[r['id']]) for r in actual['matches']), label + ' evidence')
            tiles = page.locator('.tile').evaluate_all('(nodes) => nodes.map(n => n.dataset.id)')
            check(tiles == [r['id'] for r in wanted[:96]], label + ' displayed first page')
            return wanted

        def jump(sid):
            page.evaluate('(hash) => { location.hash = hash; }', urlencode({'id': sid}))
            page.wait_for_function('(id) => document.querySelector("#sprite-id").textContent === id', arg=sid)

        check('7,115 / 7,115' in page.locator('#count').inner_text(), 'all occupied artwork opens offline')
        check(not page.locator('#game-details').get_attribute('open'), 'game casting starts folded')
        page.locator('#type').select_option('actor')
        page.locator('#color').select_option('green')
        page.locator('#sort').select_option('color')
        wanted = verify_selection(['--type', 'actor', '--color', 'green', '--sort', 'color'], 'actor/color filtering and sorting')
        check(len(wanted) > 96, 'color/type query spans pages')
        page.locator('#next').click()
        check(page.locator('.tile').first.get_attribute('data-id') == wanted[96]['id'], 'pagination follows sorted results')
        page.reload()
        check(page.locator('.tile').first.get_attribute('data-id') == wanted[96]['id'], 'URL restores filters and page')

        page.locator('#reset').click()
        page.locator('#subject').fill('ghost')
        page.locator('#subject-confidence').select_option('high')
        verify_selection(['--subject', 'ghost', '--subject-confidence', 'high'], 'high-confidence ghost subject')
        ghost = 'urizen-onebit-r42-c110'
        check(page.locator('#sprite-id').inner_text() == ghost, 'subject selects actual ghost crop')
        check('id='+ghost in page.locator('#permalink').get_attribute('href'), 'selected sprite permalink')
        metadata = json.loads(download('#metadata', 'ghost.json').read_text())
        check(metadata == neutral_record(records[ghost]), 'individual metadata retains source evidence')
        page.locator('#tint').click()
        check('warm mask' in page.locator('#tint').inner_text(), 'palette mask is a preview control')
        crop = Png(download('#crop-png', 'ghost.png'))
        check(hashlib.sha256(crop.crop([0, 0, crop.width, crop.height])).hexdigest() == records[ghost]['rgba_sha256'],
              'cropped sheet PNG preserves exact RGBA despite preview mask')
        page.locator('#tint').click()
        page.screenshot(path=str(args.output / 'offline-atlas.png'))

        page.locator('#pin').click()
        page.locator('#mode').click()
        board = page.locator('#board')
        box = board.bounding_box()
        page.mouse.click(box['x'] + box['width']/48, box['y'] + box['height']/24)
        scene = json.loads(download('#recipe', 'scene.json').read_text())
        check(scene['format'] == 'ithica-sprite-scene' and scene['layers'][0]['cells'][0] == ghost,
              'scene paints and exports stable ID')
        scene_png = Png(download('#png', 'scene.png'))
        check((scene_png.width, scene_png.height) == (768,384), 'scene PNG dimensions')
        patch = scene_png.crop([0,0,32,32])
        check(any(patch[i:i+3] != bytes.fromhex('252a31') for i in range(0,len(patch),4)), 'scene PNG contains painted art')
        box = board.bounding_box()
        page.mouse.click(box['x'] + box['width']/48, box['y'] + box['height']/24, button='right')
        erased = json.loads(download('#recipe', 'erased-scene.json').read_text())
        check(erased['layers'][0]['cells'][0] is None, 'right-click erases active layer')
        page.locator('#mode').click()

        page.locator('#reset').click()
        page.locator('#search').fill('Interference')
        check('0 / 7,115' in page.locator('#count').inner_text(), 'neutral search excludes game casting')
        check(page.locator('#metadata').is_disabled() and page.locator('#sprite-id').inner_text() == '',
              'empty results clear selection and exports')
        page.locator('#advanced-filters > summary').click()
        page.locator('#game-search').check()
        check(page.locator('.tile').count() > 0, 'optional game search finds casting')

        page.locator('#reset').click()
        page.locator('#type').select_option('fragment')
        page.locator('#layer').select_option('object')
        page.locator('#composition').select_option('fragment_candidate')
        page.locator('#sort').select_option('name')
        page.locator('#descending').check()
        verify_selection(['--type','fragment','--layer','object','--composition','fragment_candidate','--sort','name','--descending'],
                         'fragment/layer/composition query')
        page.locator('#reset').click()
        page.locator('#color').select_option('blue')
        page.locator('#primary').check()
        page.locator('#sort').select_option('name')
        verify_selection(['--primary-color','blue','--sort','name'], 'primary color query')
        page.locator('#reset').click()
        page.locator('#type').select_option('input_prompt')
        page.locator('#color').select_option('white')
        page.locator('#coverage').fill('30')
        verify_selection(['--type','input_prompt','--color','white','--min-color-coverage','.3'], 'measured color coverage threshold')
        page.locator('#reset').click()
        page.locator('#hex').fill('#feffff')
        verify_selection(['--hex','#feffff'], 'exact foreground RGB query')
        page.locator('#reset').click()
        page.locator('#subject').fill('goblin_like')
        page.locator('#subject-confidence').select_option('high')
        check('0 / 7,115' in page.locator('#count').inner_text(), 'uncertain goblin resemblance is not confirmed species')
        page.locator('#subject-confidence').select_option('low')
        check(page.locator('.tile').count() > 0, 'uncertain resemblance remains discoverable')

        embedded = json.loads(page.locator('#crops').text_content())
        occupied = {sid for sid,r in records.items() if r['kind'] == 'sprite'}
        check(set(embedded) == occupied, 'every occupied sprite has an exact downloadable PNG')
        for sid, encoded in embedded.items():
            im = Png(base64.b64decode(encoded))
            if hashlib.sha256(im.rgba).hexdigest() != records[sid]['rgba_sha256']:
                raise AssertionError('Embedded PNG differs from frozen RGBA: '+sid)
        check(True, 'all 7115 embedded crop PNGs match frozen RGBA hashes')
        # Canvas normalization must never alter transparent/partly transparent art.
        sensitive = []
        for item in catalog['images']:
            source = ROOT / 'assets' / item['path']
            im = Png(source)
            if any((im.rgba[i+3] not in (0,255)) or (im.rgba[i+3] == 0 and any(im.rgba[i:i+3]))
                   for i in range(0,len(im.rgba),4)):
                sensitive.append(item['path'])
        check(len(sensitive) == 13, 'sensitive source transparency inventory')
        for source in sensitive:
            sprites = [r for r in records.values() if r['kind'] == 'sprite' and r['source'] == source]
            r = sprites[0]
            jump(r['id'])
            exported = download('#crop-png', 'sensitive-icon.png')
            check(exported.read_bytes() == base64.b64decode(embedded[r['id']]), 'exact crop download: '+r['id'])
            if len(sprites) == 1:
                check(exported.read_bytes() == (ROOT / 'assets' / source).read_bytes(),
                      'original whole-image PNG bytes retained: '+r['id'])

        jump(ghost)
        page.set_viewport_size({'width': 390, 'height': 844})
        page.locator('#details').scroll_into_view_if_needed()
        check(page.evaluate('document.documentElement.scrollWidth <= innerWidth'), 'mobile viewport has no horizontal overflow')
        page.screenshot(path=str(args.output / 'offline-atlas-mobile.png'))
        check(not errors, 'no browser script errors: '+str(errors))
        check(not requests, 'no network requests: '+str(requests))
        browser.close()

    report = {'result': 'passed', 'checks': checks, 'count': len(checks), 'output': str(args.output)}
    (args.output / 'report.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
