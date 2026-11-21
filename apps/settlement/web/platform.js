// Browser capabilities only. Landscape rules and saved-world validation live in Dart.
(() => {
  const key = 'roag.settlement.landscape.v1';
  let context, muted = false, stopWatching;
  // repeated resident field names dominate regional saves; dictionary packing
  // retains localStorage's single-key atomic write and keeps exports ordinary JSON.
  function packRegion(text) {
    const root = JSON.parse(text);
    if (root.schema !== 7) return text;
    const keys = [], ids = new Map();
    const pack = value => {
      if (Array.isArray(value)) return value.map(pack);
      if (value && typeof value === 'object') {
        const entries = [];
        for (const [key, child] of Object.entries(value)) {
          if (!ids.has(key)) { ids.set(key, keys.length); keys.push(key); }
          entries.push(ids.get(key), pack(child));
        }
        return {$: entries};
      }
      return value;
    };
    const value = pack(root);
    return JSON.stringify({storageFormat: 'roag-region-dictionary-v1', keys, value});
  }
  function unpackRegion(text) {
    if (!text) return '';
    const root = JSON.parse(text);
    if (root.storageFormat !== 'roag-region-dictionary-v1') return text;
    if (!Array.isArray(root.keys) || root.keys.length > 2048 ||
        root.keys.some(k => typeof k !== 'string' || k.length > 256) || new Set(root.keys).size !== root.keys.length) throw Error('Invalid packed region keys.');
    let count = 0;
    const unpack = (value, depth = 0) => {
      if (++count > 2000000 || depth > 64) throw Error('Packed region is too large.');
      if (Array.isArray(value)) return value.map(v => unpack(v, depth + 1));
      if (value && typeof value === 'object') {
        if (Object.keys(value).length !== 1 || !Array.isArray(value.$) || value.$.length % 2) throw Error('Invalid packed region object.');
        const result = Object.create(null), seen = new Set();
        for (let i = 0; i < value.$.length; i += 2) {
          const id = value.$[i];
          if (!Number.isInteger(id) || id < 0 || id >= root.keys.length || seen.has(id)) throw Error('Invalid packed region field.');
          seen.add(id); result[root.keys[id]] = unpack(value.$[i + 1], depth + 1);
        }
        return result;
      }
      return value;
    };
    return JSON.stringify(unpack(root.value));
  }
  window.settlementPlatform = {
    registerDebug(read, command) {
      window.settlementDebug = Object.freeze({
        snapshot: () => JSON.parse(read()),
        command: value => JSON.parse(command(JSON.stringify(value))),
      });
    },
    unregisterDebug() { delete window.settlementDebug; },
    reportMetrics(text) { window.settlementMetrics = JSON.parse(text); },
    hideLoading() { document.getElementById('loading')?.remove(); },
    watchActivity(callback) {
      stopWatching?.();
      const update=()=>callback(!document.hidden && document.hasFocus());
      const leaving=()=>callback(false);
      window.addEventListener('blur',update);window.addEventListener('focus',update);
      document.addEventListener('visibilitychange',update);
      window.addEventListener('pagehide',leaving);
      stopWatching=()=>{window.removeEventListener('blur',update);window.removeEventListener('focus',update);document.removeEventListener('visibilitychange',update);window.removeEventListener('pagehide',leaving);};
      update();
    },
    unwatchActivity() { stopWatching?.();stopWatching=null; },
    load() { return unpackRegion(localStorage.getItem(key) || ''); },
    backupLegacy(text) {
      const backup = key + '.before-colony-v3';
      if (localStorage.getItem(backup) === null) localStorage.setItem(backup, text);
    },
    backupIncidents(text) {
      const backup = key + '.before-incidents-v5';
      if (localStorage.getItem(backup) === null) localStorage.setItem(backup, text);
    },
    backupStories(text) {
      const backup = key + '.before-stories-v6';
      if (localStorage.getItem(backup) === null) localStorage.setItem(backup, text);
    },
    backupRegion(text) {
      const backup = key + '.before-region-v7';
      if (localStorage.getItem(backup) === null) localStorage.setItem(backup, text);
    },
    backupLiving(text) {
      const backup = key + '.before-living-v4';
      if (localStorage.getItem(backup) === null) localStorage.setItem(backup, text);
    },
    save(text) { localStorage.setItem(key, packRegion(text)); },
    exportFile(text) {
      const url = URL.createObjectURL(new Blob([text], {type:'application/json'}));
      const a = document.createElement('a');
      a.href = url; a.download = 'roag-landscape.json'; a.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    },
    importFile() {
      return new Promise((resolve, reject) => {
        const input = document.createElement('input');
        input.type = 'file'; input.accept = '.json,application/json';
        input.onchange = async () => {
          const file = input.files[0];
          if (!file) return resolve('');
          if (file.size > 16000000) return reject(Error('File exceeds the 16 MB regional import limit.'));
          try { resolve(await file.text()); } catch(e) { reject(e); }
        };
        input.oncancel = () => resolve('');
        input.click();
      });
    },
    mute(value) { muted = value; },
    sound(kind) {
      if (muted) return;
      try {
        context ||= new AudioContext();
        context.resume();
        const t = context.currentTime;
        const osc = context.createOscillator(), gain = context.createGain();
        const notes = {place:[440,660], path:[280,420], remove:[230,110], invalid:[130,100], undo:[320,260]};
        const pair = notes[kind] || notes.place;
        osc.type = 'triangle'; osc.frequency.setValueAtTime(pair[0], t);
        osc.frequency.exponentialRampToValueAtTime(pair[1], t + .065);
        gain.gain.setValueAtTime(.0001,t); gain.gain.linearRampToValueAtTime(.035,t+.008);
        gain.gain.exponentialRampToValueAtTime(.0001,t+.11);
        osc.connect(gain).connect(context.destination); osc.start(t); osc.stop(t+.12);
      } catch (_) { /* audio is optional; never block a landscape edit */ }
    }
  };
  document.addEventListener('contextmenu', e => e.preventDefault());
})();
