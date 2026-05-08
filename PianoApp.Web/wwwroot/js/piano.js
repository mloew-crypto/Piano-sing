const WhiteKeyWidth = 28;
const WhiteKeyHeight = 180;
const BlackKeyWidth = 18;
const BlackKeyHeight = 110;
const OctaveCount = 5;
const StartOctave = 1;

const NoteNames = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
const IsBlackKey = [false, true, false, true, false, false, true, false, true, false, true, false];

const KeyLayout = {
  a: 'C',
  w: 'C#',
  s: 'D',
  e: 'D#',
  d: 'E',
  f: 'F',
  t: 'F#',
  g: 'G',
  y: 'G#',
  h: 'A',
  u: 'A#',
  j: 'B',
};

function normalizeNoteKey(key) {
  if (!key || !key.trim()) return key;
  let k = key
    .trim()
    .replace('\uFF03', '#')
    .replace('\u266F', '#');
  if (/[a-zA-Z]/.test(k[0])) k = k[0].toUpperCase() + k.slice(1);
  return k;
}

function flatToSharp(key) {
  if (key.length < 2 || key[1] !== 'b') return null;
  const map = { D: 'C#', E: 'D#', G: 'F#', A: 'G#', B: 'A#' };
  const sharp = map[key[0]];
  return sharp ? sharp + key.slice(2) : null;
}

function buildPianoKeys() {
  const keys = [];
  let x = 0;
  for (let oct = 0; oct < OctaveCount; oct++) {
    const octaveNum = StartOctave + oct;
    let whiteIndex = 0;
    for (let i = 0; i < 12; i++) {
      const noteName = NoteNames[i] + octaveNum;
      const isBlack = IsBlackKey[i];
      let left;
      let top;
      let width;
      let height;
      if (isBlack) {
        width = BlackKeyWidth;
        height = BlackKeyHeight;
        left = x + (whiteIndex - 0.45) * WhiteKeyWidth;
        top = 0;
      } else {
        width = WhiteKeyWidth;
        height = WhiteKeyHeight;
        left = x + whiteIndex * WhiteKeyWidth;
        top = 0;
        whiteIndex++;
      }
      keys.push({ noteName, left, top, width, height, isBlack });
    }
    x += 7 * WhiteKeyWidth;
  }
  keys.push({ noteName: 'C6', left: x, top: 0, width: WhiteKeyWidth, height: WhiteKeyHeight, isBlack: false });
  return keys;
}

function expandMappingEntries(raw) {
  const out = new Map();
  for (const [k, v] of Object.entries(raw)) {
    if (!v || typeof v !== 'string' || !v.trim()) continue;
    const key = k.trim();
    if (!key || key.startsWith('//')) continue;
    if (key.toLowerCase() === '_basepath') continue;
    const normalized = normalizeNoteKey(key);
    const path = v.trim();
    out.set(normalized, path);
    if (normalized.includes('#')) out.set(normalized.replace('#', 's'), path);
    const sharp = flatToSharp(normalized);
    if (sharp) out.set(sharp, path);
  }
  return out;
}

function audioUrlFor(relPath, basePath) {
  const rel = relPath.replace(/\\/g, '/').replace(/^\//, '');
  const base = (basePath || '').replace(/\\/g, '/').replace(/\/$/, '');
  const combined = base ? `${base}/${rel}` : rel;
  const url = new URL(combined, window.location.origin);
  return url.pathname;
}

async function loadNotePaths() {
  const res = await fetch('/NoteMapping.json', { cache: 'no-store' });
  if (!res.ok) throw new Error(`NoteMapping.json: ${res.status}`);
  const raw = await res.json();
  const basePath = typeof raw._basePath === 'string' ? raw._basePath.trim() : '';
  const map = expandMappingEntries(raw);
  const noteToUrl = new Map();
  for (const [note, rel] of map) {
    noteToUrl.set(note, audioUrlFor(rel, basePath));
  }
  return noteToUrl;
}

async function checkSampleAudio(url) {
  try {
    const head = await fetch(url, { method: 'HEAD', cache: 'no-store' });
    if (head.ok) return { ok: true, status: head.status };
    const get = await fetch(url, { method: 'GET', cache: 'no-store' });
    return { ok: get.ok, status: get.status };
  } catch {
    return { ok: false, status: 0 };
  }
}

const colors = {
  white: { normal: '#fffaf0', pressed: '#ccc4b4' },
  black: { normal: '#000000', pressed: '#444444' },
};

function draw(canvas, ctx, keys, pressed) {
  const w = canvas.width;
  const h = canvas.height;
  ctx.fillStyle = '#16213e';
  ctx.fillRect(0, 0, w, h);
  for (const key of keys) {
    const on = pressed.has(key.noteName);
    ctx.fillStyle = key.isBlack
      ? on
        ? colors.black.pressed
        : colors.black.normal
      : on
        ? colors.white.pressed
        : colors.white.normal;
    ctx.strokeStyle = '#000';
    ctx.lineWidth = 1;
    ctx.fillRect(key.left, key.top, key.width, key.height);
    ctx.strokeRect(key.left, key.top, key.width, key.height);
  }
}

function hitTest(keys, x, y) {
  for (let i = 0; i < keys.length; i++) {
    const k = keys[i];
    if (x >= k.left && x <= k.left + k.width && y >= k.top && y <= k.top + k.height) return k;
  }
  return null;
}

function scalePoint(canvas, clientX, clientY) {
  const rect = canvas.getBoundingClientRect();
  const scaleX = canvas.width / rect.width;
  const scaleY = canvas.height / rect.height;
  return {
    x: (clientX - rect.left) * scaleX,
    y: (clientY - rect.top) * scaleY,
  };
}

async function main() {
  const canvas = document.getElementById('piano');
  const status = document.getElementById('status');
  const ctx = canvas.getContext('2d');
  const keys = buildPianoKeys();
  const pressed = new Set();
  let noteToUrl = new Map();
  const audioCache = new Map();

  function setStatus(msg) {
    status.textContent = msg;
  }

  const pianoWidth = (OctaveCount * 7 + 1) * WhiteKeyWidth;
  const pianoHeight = WhiteKeyHeight;

  function resize() {
    const maxW = canvas.parentElement?.clientWidth || pianoWidth;
    const scale = Math.min(1, maxW / pianoWidth);
    canvas.width = pianoWidth * scale;
    canvas.height = pianoHeight * scale;
    ctx.setTransform(scale, 0, 0, scale, 0, 0);
    draw(canvas, ctx, keys, pressed);
  }

  try {
    noteToUrl = await loadNotePaths();
    const missing = keys.filter((k) => !noteToUrl.has(k.noteName)).length;
    const sampleUrl =
      noteToUrl.get('C4') ?? (noteToUrl.size > 0 ? [...noteToUrl.values()][0] : null);
    let sampleOk = false;
    if (sampleUrl) {
      const probe = await checkSampleAudio(sampleUrl);
      sampleOk = probe.ok;
      if (!sampleOk) {
        setStatus(
          `Mapping loaded, but no file at ${sampleUrl} (HTTP ${probe.status || 'network'}). ` +
            'Put WAVs in the repo folder Audio (next to PianoApp.csproj) and run dotnet build on PianoApp.Web, ' +
            'or copy them to PianoApp.Web/wwwroot/Audio.',
        );
      }
    } else if (noteToUrl.size === 0) {
      setStatus('NoteMapping.json has no audio paths.');
    }
    if (sampleOk) {
      setStatus(
        missing > 0
          ? `Ready. ${missing} keys have no entry in NoteMapping.json.`
          : 'Ready.',
      );
    }
  } catch (e) {
    setStatus(e instanceof Error ? e.message : String(e));
  }

  function ensureAudio(noteName) {
    const url = noteToUrl.get(noteName);
    if (!url) return null;
    let a = audioCache.get(noteName);
    if (!a) {
      a = new Audio(url);
      a.preload = 'auto';
      a.addEventListener(
        'error',
        () => {
          const code = a.error ? a.error.code : '';
          setStatus(`Could not load audio for ${noteName}: ${url} (code ${code}). Check the file exists under wwwroot/Audio.`);
        },
        { once: true },
      );
      audioCache.set(noteName, a);
    }
    return a;
  }

  function play(noteName) {
    const a = ensureAudio(noteName);
    if (!a) return;
    try {
      a.pause();
      a.currentTime = 0;
      void a.play().catch((err) => {
        setStatus(`Playback blocked or failed (${noteName}): ${err?.message ?? err}`);
      });
    } catch (err) {
      setStatus(`Playback failed (${noteName}): ${err?.message ?? err}`);
    }
  }

  function setPressed(noteName, on) {
    if (on) pressed.add(noteName);
    else pressed.delete(noteName);
    draw(canvas, ctx, keys, pressed);
  }

  function clearPressed() {
    pressed.clear();
    draw(canvas, ctx, keys, pressed);
  }

  let currentOctave = StartOctave;

  function noteFromKeyboard(code) {
    const row = KeyLayout[code];
    if (!row) return null;
    return row + currentOctave;
  }

  window.addEventListener('keydown', (e) => {
    if (e.repeat) return;
    const k = e.key.toLowerCase();
    if (k >= '1' && k <= '6') {
      currentOctave = parseInt(k, 10);
      return;
    }
    const note = noteFromKeyboard(k);
    if (!note) return;
    e.preventDefault();
    const match = keys.find((x) => x.noteName === note);
    if (!match) return;
    setPressed(note, true);
    play(note);
  });

  window.addEventListener('keyup', (e) => {
    const note = noteFromKeyboard(e.key.toLowerCase());
    if (note) setPressed(note, false);
  });

  function onPointerDown(ev) {
    const { x, y } = scalePoint(canvas, ev.clientX, ev.clientY);
    const key = hitTest(keys, x, y);
    clearPressed();
    if (key) {
      setPressed(key.noteName, true);
      play(key.noteName);
    }
  }

  function onPointerUp() {
    clearPressed();
  }

  canvas.addEventListener('pointerdown', onPointerDown);
  canvas.addEventListener('pointerup', onPointerUp);
  canvas.addEventListener('pointerleave', onPointerUp);
  canvas.addEventListener('pointercancel', onPointerUp);

  window.addEventListener('resize', resize);
  resize();
}

main();
