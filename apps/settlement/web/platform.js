// Browser capabilities only. Landscape rules and saved-world validation live in Dart.
(() => {
  window.addEventListener('flutter-first-frame', () => document.getElementById('loading')?.remove());
  const key = 'roag.settlement.landscape.v1';
  let context, muted = false, stopWatching;
  window.settlementPlatform = {
    hideLoading() { document.getElementById('loading')?.remove(); },
    watchActivity(callback) {
      stopWatching?.();
      const update=()=>callback(!document.hidden && document.hasFocus());
      window.addEventListener('blur',update);window.addEventListener('focus',update);
      document.addEventListener('visibilitychange',update);
      stopWatching=()=>{window.removeEventListener('blur',update);window.removeEventListener('focus',update);document.removeEventListener('visibilitychange',update);};
      update();
    },
    unwatchActivity() { stopWatching?.();stopWatching=null; },
    load() { return localStorage.getItem(key) || ''; },
    save(text) { localStorage.setItem(key, text); },
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
          if (file.size > 2000000) return reject(Error('File is too large.'));
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
