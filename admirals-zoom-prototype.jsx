import { useEffect, useRef, useCallback } from "react";

// ═══ RENDER CONSTANTS ═══
const RW = 384, RH = 288;
const SCALE = 2.0;
const TILT = 0.42;
const MIN_ZOOM = 0.9, MAX_ZOOM = 2200;
const PITCH_RAD = 12 * Math.PI / 180;
const SPIRAL_B = 1 / Math.tan(PITCH_RAD);
const BAR_ANGLE = 0.77;
const GALAXY_R = 50;

// ═══ HELPERS ═══
const srand = (seed) => {
  let s = seed;
  return () => { s = (s * 16807) % 2147483647; return (s - 1) / 2147483646; };
};
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
const lerp = (a, b, t) => a + (b - a) * t;

// 4x4 Bayer ordered dithering
const BAYER = [0,8,2,10, 12,4,14,6, 3,11,1,9, 15,7,13,5];
const ditherOk = (x, y, intensity) => intensity > BAYER[((y & 3) << 2) + (x & 3)] / 16;

const px = (ctx, x, y, w, h, col) => {
  ctx.fillStyle = col;
  ctx.fillRect(Math.floor(x), Math.floor(y), w, h);
};

// Dithered circular glow (pure pixel, no canvas gradients)
function ditherGlow(ctx, cx, cy, radius, cr, cg, cb, peak) {
  const r = Math.ceil(radius);
  const x0 = Math.floor(cx - r), y0 = Math.floor(cy - r);
  const x1 = Math.floor(cx + r), y1 = Math.floor(cy + r);
  const colStr = `rgb(${cr},${cg},${cb})`;
  ctx.fillStyle = colStr;
  for (let py = y0; py <= y1; py++) {
    for (let ppx = x0; ppx <= x1; ppx++) {
      const dx = ppx - cx, dy = py - cy;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist > radius) continue;
      const intensity = (1 - dist / radius) * peak;
      if (ditherOk(ppx, py, intensity)) {
        ctx.fillRect(ppx, py, 1, 1);
      }
    }
  }
}

// Elliptical dithered glow
function ditherEllipse(ctx, cx, cy, rx, ry, cr, cg, cb, peak) {
  const x0 = Math.floor(cx - rx), y0 = Math.floor(cy - ry);
  const x1 = Math.ceil(cx + rx), y1 = Math.ceil(cy + ry);
  const colStr = `rgb(${cr},${cg},${cb})`;
  ctx.fillStyle = colStr;
  for (let py = y0; py <= y1; py++) {
    for (let ppx = x0; ppx <= x1; ppx++) {
      const dx = (ppx - cx) / rx, dy = (py - cy) / ry;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist > 1) continue;
      const intensity = (1 - dist) * peak;
      if (ditherOk(ppx, py, intensity)) {
        ctx.fillRect(ppx, py, 1, 1);
      }
    }
  }
}

// ═══ SPIRAL ARM DEFINITIONS ═══
const ARMS = [
  { start: BAR_ANGLE, a: 3.0 },
  { start: BAR_ANGLE + Math.PI, a: 3.0 },
  { start: BAR_ANGLE + 0.55, a: 3.2 },
  { start: BAR_ANGLE + Math.PI + 0.55, a: 3.2 },
];
const SPURS = [
  { start: BAR_ANGLE + 1.1, a: 6.0 },
  { start: BAR_ANGLE + Math.PI + 1.1, a: 6.0 },
];

// ═══ TARGET SYSTEM (what we zoom into) ═══
const ARC_THETA = 1.8;
const ARC_ARM = ARMS[1];
const ARC_R = ARC_ARM.a * Math.exp(ARC_THETA / SPIRAL_B);
const ARC_ANG = ARC_ARM.start + ARC_THETA;
const ARC_X = Math.cos(ARC_ANG) * ARC_R;
const ARC_Y = Math.sin(ARC_ANG) * ARC_R * TILT;

// System nodes for sector view
const SYS_NODES = [
  { x: ARC_X, y: ARC_Y, name: "ARCTURUS PRIME", faction: "sov", sz: 2, pop: "12.4B" },
  { x: ARC_X + 6, y: ARC_Y - 3, name: "VEGA STATION", faction: "sov", sz: 1.5, pop: "3.1B" },
  { x: ARC_X - 4, y: ARC_Y + 5, name: "KESSLER DRIFT", faction: "cov", sz: 1.2, pop: "800M" },
  { x: ARC_X + 10, y: ARC_Y + 3, name: "NEW SHANGHAI", faction: "cov", sz: 1.5, pop: "8.7B" },
  { x: ARC_X + 3, y: ARC_Y + 7, name: "GHOST NEBULA", faction: "syn", sz: 1, pop: "—" },
  { x: ARC_X - 7, y: ARC_Y - 2, name: "PORT DEFIANCE", faction: "sov", sz: 1.2, pop: "2.2B" },
  { x: ARC_X + 12, y: ARC_Y + 8, name: "FREEHOLD", faction: "neu", sz: 0.8, pop: "400M" },
  { x: ARC_X + 5, y: ARC_Y - 6, name: "IRON GATE", faction: "sov", sz: 1.5, pop: "5.6B" },
  { x: ARC_X + 14, y: ARC_Y - 2, name: "SYNDEX HUB", faction: "syn", sz: 1.2, pop: "1.8B" },
  { x: ARC_X - 2, y: ARC_Y + 10, name: "BASTION", faction: "cov", sz: 2, pop: "15.1B" },
];
const LANES = [[0,1],[0,2],[1,3],[1,7],[2,3],[2,9],[3,4],[4,6],[5,0],[7,8],[3,8],[4,9],[6,9]];
const FC = {
  sov: [74, 111, 165], cov: [196, 112, 58], syn: [58, 154, 122], neu: [100, 100, 100],
};

// Planets around Arcturus Prime (offsets in kly, exaggerated for gameplay)
const PLANETS = [
  { dx: 0.08, dy: 0.03, r: 2, color: [138, 96, 64], name: "SCORCH", speed: 0.0008 },
  { dx: 0.16, dy: 0.06, r: 3, color: [74, 122, 181], name: "NEW EDEN", speed: 0.0005, ring: true, hasFleet: true },
  { dx: 0.25, dy: 0.09, r: 2.5, color: [122, 90, 138], name: "UMBRA", speed: 0.0003 },
  { dx: 0.33, dy: 0.12, r: 1.5, color: [90, 138, 106], name: "FARPOST", speed: 0.0002 },
];

// Fleet ships relative to New Eden
const FLEET_CENTER = { dx: 0.19, dy: 0.04 };

// ═══ PRE-GENERATE GALAXY STARS ═══
function genStars() {
  const stars = [];
  // Halo
  const rng = srand(42);
  for (let i = 0; i < 500; i++) {
    const x = (rng() - 0.5) * 140;
    const y = (rng() - 0.5) * 140 * TILT;
    const b = 20 + rng() * 35;
    const t = rng();
    stars.push({ x, y, r: t > 0.7 ? b + 12 : b, g: b, b: t < 0.3 ? b + 16 : b + 5, fl: rng() * 6.28 });
  }
  // Arms
  const ar = srand(77);
  for (let i = 0; i < 6000; i++) {
    const isMaj = ar() > 0.18;
    const defs = isMaj ? ARMS : SPURS;
    const arm = defs[Math.floor(ar() * defs.length)];
    const theta = ar() * 4.5;
    const rKly = arm.a * Math.exp(theta / SPIRAL_B);
    if (rKly > GALAXY_R) continue;
    const scatter = (ar() - 0.5) * (isMaj ? 3.2 : 1.8) * (1 + rKly / 45);
    const aAngle = arm.start + theta;
    const pAngle = aAngle + Math.PI / 2;
    const wx = Math.cos(aAngle) * rKly + Math.cos(pAngle) * scatter;
    const wy = (Math.sin(aAngle) * rKly + Math.sin(pAngle) * scatter) * TILT;
    const isYoung = ar() < (isMaj ? 0.38 : 0.22);
    let cr, cg, cb;
    if (isYoung) {
      const br = 95 + ar() * 100;
      cr = br * 0.72; cg = br * 0.84; cb = br;
    } else {
      const br = 60 + ar() * 75;
      const tmp = ar();
      if (tmp < 0.4) { cr = br; cg = br * 0.88; cb = br * 0.58; }
      else if (tmp < 0.7) { cr = br; cg = br * 0.73; cb = br * 0.52; }
      else { cr = br * 0.88; cg = br * 0.84; cb = br * 0.78; }
    }
    const wa = Math.atan2(wy / TILT, wx);
    const na = ((wa % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
    if (na < Math.PI * 0.67) { cr *= 0.92; cb *= 1.06; }
    else if (na < Math.PI * 1.33) { cr *= 1.06; cb *= 0.92; }
    else { cg *= 1.05; cb *= 1.03; }
    stars.push({ x: wx, y: wy, r: clamp(Math.floor(cr), 0, 255), g: clamp(Math.floor(cg), 0, 255), b: clamp(Math.floor(cb), 0, 255), fl: ar() * 6.28 });
  }
  // Disk
  const dr = srand(200);
  for (let i = 0; i < 2500; i++) {
    const u = dr();
    const rKly = -10 * Math.log(1 - u * (1 - Math.exp(-5)));
    const angle = dr() * Math.PI * 2;
    const wx = Math.cos(angle) * rKly;
    const wy = Math.sin(angle) * rKly * TILT;
    const br = 35 + dr() * 50;
    stars.push({ x: wx, y: wy, r: Math.floor(br), g: Math.floor(br * 0.86), b: Math.floor(br * 0.6), fl: dr() * 6.28 });
  }
  return stars;
}

// ═══ DUST LANE PARTICLES ═══
function genDust() {
  const dust = [];
  const dr = srand(555);
  for (let i = 0; i < 600; i++) {
    const arm = ARMS[Math.floor(dr() * ARMS.length)];
    const theta = 0.3 + dr() * 4.0;
    const rKly = arm.a * Math.exp(theta / SPIRAL_B);
    if (rKly > GALAXY_R) continue;
    const scatter = -1.2 - dr() * 2.0;
    const aAngle = arm.start + theta;
    const pAngle = aAngle + Math.PI / 2;
    const wx = Math.cos(aAngle) * rKly + Math.cos(pAngle) * scatter;
    const wy = (Math.sin(aAngle) * rKly + Math.sin(pAngle) * scatter) * TILT;
    dust.push({ x: wx, y: wy, sz: 1 + dr() * 1.5 });
  }
  return dust;
}

// ═══ HII REGIONS ═══
function genHII() {
  const hii = [];
  const hr = srand(333);
  for (let i = 0; i < 30; i++) {
    const arm = ARMS[Math.floor(hr() * ARMS.length)];
    const theta = 0.5 + hr() * 3.5;
    const rKly = arm.a * Math.exp(theta / SPIRAL_B);
    if (rKly > GALAXY_R * 0.85) continue;
    const scatter = (hr() - 0.5) * 2.5;
    const aAngle = arm.start + theta;
    const pAngle = aAngle + Math.PI / 2;
    hii.push({
      x: Math.cos(aAngle) * rKly + Math.cos(pAngle) * scatter,
      y: (Math.sin(aAngle) * rKly + Math.sin(pAngle) * scatter) * TILT,
      sz: 1.5 + hr() * 2.5,
      phase: hr() * 6.28,
    });
  }
  return hii;
}

// ═══ PIXEL SHIP SPRITES ═══
function drawDreadnought(ctx, sx, sy, scale, highlight, t) {
  const s = Math.max(1, Math.round(scale));
  const p = (dx, dy, w, h, c) => px(ctx, sx + dx * s, sy + dy * s, w * s, h * s, c);
  const c1 = highlight ? "#8ab0e0" : "#4a6fa5";
  const c2 = highlight ? "#a0c8f0" : "#6a8fc5";
  p(-20, -6, 40, 12, "#2a3a55");
  p(-19, -7, 38, 14, c1);
  p(-17, -5, 34, 10, c2);
  p(14, -4, 7, 8, "#3a5a85");
  p(15, -3, 5, 6, "#8ac0ff");
  const eg = Math.sin(t * 0.01) * 0.3 + 0.7;
  p(-23, -3, 4, 3, `rgba(80,160,255,${eg})`);
  p(-23, 1, 4, 3, `rgba(80,160,255,${eg})`);
  p(-7, -9, 4, 3, "#556"); p(3, -9, 4, 3, "#556");
  p(-7, 7, 4, 3, "#556"); p(3, 7, 4, 3, "#556");
  p(-16, -1, 28, 2, "rgba(255,255,255,0.4)");
}

function drawCruiser(ctx, sx, sy, scale, t) {
  const s = Math.max(1, Math.round(scale));
  const p = (dx, dy, w, h, c) => px(ctx, sx + dx * s, sy + dy * s, w * s, h * s, c);
  p(-14, -4, 28, 8, "#2a3a55");
  p(-13, -5, 26, 10, "#4a6fa5");
  p(-11, -3, 22, 6, "#5a7fb5");
  p(11, -2, 5, 4, "#8ac0ff");
  const eg = Math.sin(t * 0.01 + 1) * 0.3 + 0.7;
  p(-16, -2, 3, 4, `rgba(100,180,255,${eg})`);
  p(-4, -7, 3, 2, "#556"); p(-4, 5, 3, 2, "#556");
}

function drawDestroyer(ctx, sx, sy, scale, t) {
  const s = Math.max(1, Math.round(scale));
  const p = (dx, dy, w, h, c) => px(ctx, sx + dx * s, sy + dy * s, w * s, h * s, c);
  p(-8, -3, 16, 6, "#3a5a85");
  p(-7, -4, 14, 8, "#4a6fa5");
  p(6, -1, 3, 3, "#8ac0ff");
  const eg = Math.sin(t * 0.01 + 2) * 0.3 + 0.7;
  p(-10, -1, 2, 3, `rgba(100,180,255,${eg})`);
}

function drawCorvette(ctx, sx, sy, scale, t) {
  const s = Math.max(1, Math.round(scale));
  const p = (dx, dy, w, h, c) => px(ctx, sx + dx * s, sy + dy * s, w * s, h * s, c);
  p(-5, -2, 10, 5, "#3a5a85");
  p(-4, -3, 8, 7, "#4a7fb5");
  p(3, -1, 3, 2, "#8ac0ff");
  const eg = Math.sin(t * 0.01 + 3) * 0.3 + 0.7;
  p(-6, 0, 2, 1, `rgba(100,180,255,${eg})`);
}

function drawCarrier(ctx, sx, sy, scale, t) {
  const s = Math.max(1, Math.round(scale));
  const p = (dx, dy, w, h, c) => px(ctx, sx + dx * s, sy + dy * s, w * s, h * s, c);
  p(-18, -6, 36, 12, "#2a3a55");
  p(-17, -7, 34, 14, "#3a5580");
  p(-14, -4, 28, 8, "#4a6590");
  p(-8, -2, 18, 5, "#1a2540");
  const eg = Math.sin(t * 0.01 + 4) * 0.3 + 0.7;
  p(-20, -3, 3, 3, `rgba(100,180,255,${eg})`);
  p(-20, 1, 3, 3, `rgba(100,180,255,${eg})`);
}

// Admiral pixel portrait
function drawAdmiral(ctx, x, y) {
  const p = (dx, dy, c) => { ctx.fillStyle = c; ctx.fillRect(x + dx, y + dy, 1, 1); };
  for (let dx = 2; dx < 8; dx++) for (let dy = 1; dy < 9; dy++) p(dx, dy, "#c49a6c");
  for (let dx = 1; dx < 9; dx++) for (let dy = 0; dy < 3; dy++) p(dx, dy, "#1a1a2a");
  for (let dy = 2; dy < 5; dy++) { p(1, dy, "#1a1a2a"); p(8, dy, "#1a1a2a"); }
  p(3, 4, "#fff"); p(4, 4, "#2a3a55"); p(6, 4, "#fff"); p(7, 4, "#2a3a55");
  p(3, 3, "#1a1a2a"); p(4, 3, "#1a1a2a"); p(6, 3, "#1a1a2a"); p(7, 3, "#1a1a2a");
  p(4, 7, "#a07050"); p(5, 7, "#a07050"); p(6, 7, "#a07050");
  for (let dx = 2; dx < 8; dx++) for (let dy = 9; dy < 12; dy++) p(dx, dy, "#3a5580");
  for (let dx = 3; dx < 7; dx++) p(dx, 9, "#ffdd66");
}

// ═══ MAIN COMPONENT ═══
export default function AdmiralsZoom() {
  const displayRef = useRef(null);
  const offscreenRef = useRef(null);
  const starsRef = useRef(null);
  const dustRef = useRef(null);
  const hiiRef = useRef(null);
  const frameRef = useRef(null);

  // Camera: { x, y } in kly, zoom = pixels per kly multiplier
  const camRef = useRef({ x: 0, y: 0, zoom: 1 });
  const targetCamRef = useRef({ x: 0, y: 0, zoom: 1 });
  const dragRef = useRef({ active: false, sx: 0, sy: 0, cx: 0, cy: 0 });

  // Init
  useEffect(() => {
    offscreenRef.current = document.createElement("canvas");
    offscreenRef.current.width = RW;
    offscreenRef.current.height = RH;
    starsRef.current = genStars();
    dustRef.current = genDust();
    hiiRef.current = genHII();
  }, []);

  // World to screen
  const w2s = useCallback((wx, wy, cam) => {
    return [
      RW / 2 + (wx - cam.x) * cam.zoom * SCALE,
      RH / 2 + (wy - cam.y) * cam.zoom * SCALE,
    ];
  }, []);

  // Zoom tier label
  const getTierLabel = (zoom) => {
    if (zoom < 3) return ["TIER 1 — GALAXY", "The full war at a glance"];
    if (zoom < 12) return ["TIER 2 — SECTOR", "Theater of operations"];
    if (zoom < 80) return ["TIER 3 — SYSTEM", "Arcturus Prime"];
    if (zoom < 500) return ["TIER 4 — FLEET", "3rd Fleet — Sovereignty"];
    return ["TIER 5 — SHIP", "ISS Ironclad — Flagship"];
  };

  // ═══ RENDER ═══
  const render = useCallback((timestamp) => {
    const display = displayRef.current;
    const offscreen = offscreenRef.current;
    if (!display || !offscreen || !starsRef.current) {
      frameRef.current = requestAnimationFrame(render);
      return;
    }

    const dCtx = display.getContext("2d");
    const ctx = offscreen.getContext("2d");
    ctx.imageSmoothingEnabled = false;
    dCtx.imageSmoothingEnabled = false;

    // Smooth camera
    const cam = camRef.current;
    const tgt = targetCamRef.current;
    cam.x = lerp(cam.x, tgt.x, 0.1);
    cam.y = lerp(cam.y, tgt.y, 0.1);
    cam.zoom = lerp(cam.zoom, tgt.zoom, 0.1);
    if (Math.abs(cam.zoom - tgt.zoom) < 0.01) cam.zoom = tgt.zoom;

    const zoom = cam.zoom;
    const t = timestamp;

    // Clear
    ctx.fillStyle = "#03040a";
    ctx.fillRect(0, 0, RW, RH);

    // Viewport bounds in world kly
    const vpHalfW = (RW / 2) / (zoom * SCALE);
    const vpHalfH = (RH / 2) / (zoom * SCALE);
    const vx0 = cam.x - vpHalfW, vx1 = cam.x + vpHalfW;
    const vy0 = cam.y - vpHalfH, vy1 = cam.y + vpHalfH;

    // ══════════════ LAYER: GALAXY STARS ══════════════
    if (zoom < 60) {
      const stars = starsRef.current;
      const fade = zoom < 30 ? 1 : 1 - (zoom - 30) / 30;
      for (let i = 0; i < stars.length; i++) {
        const s = stars[i];
        if (s.x < vx0 - 2 || s.x > vx1 + 2 || s.y < vy0 - 2 || s.y > vy1 + 2) continue;
        const [sx, sy] = w2s(s.x, s.y, cam);
        if (sx < -1 || sx > RW + 1 || sy < -1 || sy > RH + 1) continue;
        const flicker = Math.sin(t * 0.0015 + s.fl) * 10;
        const fr = clamp(Math.floor(s.r + flicker), 0, 255);
        const fg = clamp(Math.floor(s.g + flicker * 0.6), 0, 255);
        const fb = clamp(Math.floor(s.b + flicker * 0.4), 0, 255);
        if (fade < 1 && !ditherOk(Math.floor(sx), Math.floor(sy), fade)) continue;
        ctx.fillStyle = `rgb(${fr},${fg},${fb})`;
        ctx.fillRect(Math.floor(sx), Math.floor(sy), 1, 1);
      }

      // Dust lanes
      if (zoom < 25) {
        const dust = dustRef.current;
        const dustFade = zoom < 15 ? 1 : 1 - (zoom - 15) / 10;
        for (let i = 0; i < dust.length; i++) {
          const d = dust[i];
          if (d.x < vx0 - 3 || d.x > vx1 + 3 || d.y < vy0 - 3 || d.y > vy1 + 3) continue;
          const [sx, sy] = w2s(d.x, d.y, cam);
          const sz = Math.max(1, Math.round(d.sz * zoom * SCALE * 0.3));
          if (dustFade < 1 && !ditherOk(Math.floor(sx), Math.floor(sy), dustFade)) continue;
          ctx.fillStyle = "rgb(4,3,2)";
          ctx.fillRect(Math.floor(sx), Math.floor(sy), sz, Math.max(1, Math.round(sz * 0.6)));
        }
      }

      // HII regions (dithered pink glows)
      if (zoom < 30) {
        const hii = hiiRef.current;
        for (let i = 0; i < hii.length; i++) {
          const h = hii[i];
          const [sx, sy] = w2s(h.x, h.y, cam);
          const sr = h.sz * zoom * SCALE * 0.5;
          if (sr < 1 || sx + sr < 0 || sx - sr > RW || sy + sr < 0 || sy - sr > RH) continue;
          const pulse = Math.sin(t * 0.0008 + h.phase) * 0.02;
          ditherGlow(ctx, sx, sy, Math.min(sr, 30), 200, 70, 120, 0.25 + pulse);
        }
      }

      // Bulge glow
      if (zoom < 20) {
        const [bx, by] = w2s(0, 0, cam);
        const br = 5 * zoom * SCALE;
        if (br > 2 && bx + br > 0 && bx - br < RW && by + br > 0 && by - br < RH) {
          ditherEllipse(ctx, bx, by, Math.min(br, 80), Math.min(br * TILT, 40), 255, 230, 170, 0.5);
        }
      }

      // Bar
      if (zoom < 10) {
        const barLen = 13.5 * zoom * SCALE;
        if (barLen > 3) {
          const [bx, by] = w2s(0, 0, cam);
          const cos = Math.cos(BAR_ANGLE), sin = Math.sin(BAR_ANGLE);
          for (let i = -barLen; i <= barLen; i += 1) {
            const frac = Math.abs(i) / barLen;
            const intensity = (1 - frac) * 0.3;
            const wid = (1 - frac * 0.6) * 3.5 * zoom * SCALE * TILT;
            for (let j = -wid; j <= wid; j += 1) {
              const px2 = bx + cos * i - sin * j * TILT;
              const py2 = by + (sin * i + cos * j) * TILT;
              if (px2 < 0 || px2 >= RW || py2 < 0 || py2 >= RH) continue;
              const jFrac = Math.abs(j) / wid;
              const total = intensity * (1 - jFrac);
              if (ditherOk(Math.floor(px2), Math.floor(py2), total)) {
                ctx.fillStyle = "rgb(220,180,110)";
                ctx.fillRect(Math.floor(px2), Math.floor(py2), 1, 1);
              }
            }
          }
        }
      }

      // Sol marker
      if (zoom < 15) {
        const solAngle = BAR_ANGLE + 1.1 + 1.1;
        const solGx = Math.cos(solAngle) * 8.2;
        const solGy = Math.sin(solAngle) * 8.2 * TILT;
        const [sx, sy] = w2s(solGx, solGy, cam);
        if (sx > 0 && sx < RW && sy > 0 && sy < RH) {
          if (Math.sin(t * 0.003) > -0.3) {
            ctx.fillStyle = "#ffff66";
            ctx.fillRect(Math.floor(sx), Math.floor(sy), 1, 1);
          }
          if (zoom > 2) {
            ctx.font = "5px monospace"; ctx.fillStyle = "rgba(255,255,100,0.5)"; ctx.textAlign = "left";
            ctx.fillText("SOL", Math.floor(sx) + 2, Math.floor(sy) + 1);
          }
        }
      }
    }

    // ══════════════ LAYER: BACKGROUND STAR FIELD (when zoomed past galaxy) ══════════════
    if (zoom >= 20) {
      const bgFade = zoom < 40 ? (zoom - 20) / 20 : 1;
      const bgRng = srand(Math.floor(cam.x * 7 + cam.y * 13) + 1000);
      for (let i = 0; i < 200; i++) {
        const sx = bgRng() * RW;
        const sy = bgRng() * RH;
        const b = 20 + bgRng() * 40;
        if (bgFade < 1 && !ditherOk(Math.floor(sx), Math.floor(sy), bgFade)) continue;
        ctx.fillStyle = `rgb(${Math.floor(b)},${Math.floor(b)},${Math.floor(b + 10)})`;
        ctx.fillRect(Math.floor(sx), Math.floor(sy), 1, 1);
      }
    }

    // ══════════════ LAYER: SECTOR NODES ══════════════
    if (zoom > 2 && zoom < 80) {
      const fadeIn = clamp((zoom - 2) / 3, 0, 1);
      const fadeOut = clamp(1 - (zoom - 40) / 40, 0, 1);
      const vis = Math.min(fadeIn, fadeOut);

      // Hyperlanes
      LANES.forEach(([a, b]) => {
        const sa = SYS_NODES[a], sb = SYS_NODES[b];
        const [ax, ay] = w2s(sa.x, sa.y, cam);
        const [bx2, by2] = w2s(sb.x, sb.y, cam);
        if ((ax < -20 && bx2 < -20) || (ax > RW + 20 && bx2 > RW + 20)) return;
        // Dithered line
        const dist = Math.sqrt((bx2 - ax) ** 2 + (by2 - ay) ** 2);
        const steps = Math.floor(dist);
        ctx.fillStyle = "rgb(50,60,90)";
        for (let i = 0; i < steps; i += 2) {
          const frac = i / steps;
          const lx = Math.floor(ax + (bx2 - ax) * frac);
          const ly = Math.floor(ay + (by2 - ay) * frac);
          if (lx < 0 || lx >= RW || ly < 0 || ly >= RH) continue;
          if (ditherOk(lx, ly, vis * 0.6)) ctx.fillRect(lx, ly, 1, 1);
        }
        // Traffic dot
        if (a + b < 8 && vis > 0.5) {
          const p = ((t * 0.001 + a * 0.5) % 1);
          const dx = Math.floor(ax + (bx2 - ax) * p);
          const dy = Math.floor(ay + (by2 - ay) * p);
          ctx.fillStyle = "rgb(255,255,200)";
          ctx.fillRect(dx, dy, 1, 1);
        }
      });

      // System dots
      SYS_NODES.forEach((sys, i) => {
        const [sx, sy] = w2s(sys.x, sys.y, cam);
        if (sx < -20 || sx > RW + 20 || sy < -20 || sy > RH + 20) return;
        const fc = FC[sys.faction];
        const dotSz = Math.max(1, Math.round(sys.sz * zoom * 0.2));

        // Glow
        if (dotSz > 2 && vis > 0.5) {
          ditherGlow(ctx, sx, sy, dotSz + 3, fc[0], fc[1], fc[2], 0.2 * vis);
        }

        // Dot
        if (vis > 0.3 || ditherOk(Math.floor(sx), Math.floor(sy), vis)) {
          const cs = `rgb(${Math.min(255, fc[0] + 60)},${Math.min(255, fc[1] + 60)},${Math.min(255, fc[2] + 60)})`;
          ctx.fillStyle = cs;
          const half = Math.floor(dotSz / 2);
          for (let dy = -half; dy <= half; dy++) {
            for (let dx = -half; dx <= half; dx++) {
              if (dx * dx + dy * dy <= half * half + 1) {
                ctx.fillRect(Math.floor(sx + dx), Math.floor(sy + dy), 1, 1);
              }
            }
          }
        }

        // Label
        if (zoom > 4 && vis > 0.4) {
          ctx.font = "5px monospace"; ctx.textAlign = "center";
          ctx.fillStyle = `rgb(${Math.min(255, fc[0] + 80)},${Math.min(255, fc[1] + 80)},${Math.min(255, fc[2] + 80)})`;
          ctx.fillText(sys.name, Math.floor(sx), Math.floor(sy) + dotSz + 7);
          if (zoom > 6) {
            ctx.fillStyle = "rgb(120,120,140)";
            ctx.fillText("POP: " + sys.pop, Math.floor(sx), Math.floor(sy) + dotSz + 14);
          }
        }
      });

      // Faction labels (galaxy-wide)
      if (zoom < 8) {
        const labelAlpha = vis;
        if (labelAlpha > 0.3) {
          ctx.font = "bold 6px monospace"; ctx.textAlign = "center";
          const [lx1, ly1] = w2s(ARC_X - 10, ARC_Y - 8, cam);
          const [lx2, ly2] = w2s(ARC_X + 12, ARC_Y + 6, cam);
          const [lx3, ly3] = w2s(ARC_X - 3, ARC_Y + 12, cam);
          ctx.fillStyle = "rgba(100,130,200,0.5)"; ctx.fillText("SOVEREIGNTY", lx1, ly1);
          ctx.fillStyle = "rgba(200,130,70,0.5)"; ctx.fillText("COVENANT", lx2, ly2);
          ctx.fillStyle = "rgba(80,180,140,0.5)"; ctx.fillText("SYNDICATE", lx3, ly3);
        }
      }
    }

    // Target marker on Arcturus
    if (zoom > 2 && zoom < 25) {
      const [tx, ty] = w2s(ARC_X, ARC_Y, cam);
      if (Math.sin(t * 0.004) > 0) {
        ctx.strokeStyle = "rgb(200,200,80)"; ctx.lineWidth = 1;
        const sz = Math.max(4, Math.round(8 / (zoom * 0.3)));
        ctx.strokeRect(Math.floor(tx) - sz, Math.floor(ty) - sz, sz * 2, sz * 2);
      }
    }

    // ══════════════ LAYER: SYSTEM DETAIL ══════════════
    if (zoom > 12) {
      const sysFade = clamp((zoom - 12) / 10, 0, 1);
      const [starSx, starSy] = w2s(ARC_X, ARC_Y, cam);

      // Central star
      const starScreenR = Math.max(1, Math.round(0.04 * zoom * SCALE));
      if (starScreenR > 1 && starSx + starScreenR * 2 > 0 && starSx - starScreenR * 2 < RW) {
        const pulse = Math.sin(t * 0.002) * 0.05;
        const glowR = Math.min(starScreenR * 3, 60);
        if (sysFade > 0.5 || ditherOk(Math.floor(starSx), Math.floor(starSy), sysFade)) {
          ditherGlow(ctx, starSx, starSy, glowR, 255, 220, 130, (0.35 + pulse) * sysFade);
          ditherGlow(ctx, starSx, starSy, Math.min(starScreenR * 1.5, 30), 255, 240, 180, 0.7 * sysFade);
          // Core pixels
          const coreR = Math.min(starScreenR, 8);
          for (let dy = -coreR; dy <= coreR; dy++) {
            for (let dx = -coreR; dx <= coreR; dx++) {
              if (dx * dx + dy * dy <= coreR * coreR) {
                ctx.fillStyle = "rgb(255,245,210)";
                ctx.fillRect(Math.floor(starSx + dx), Math.floor(starSy + dy), 1, 1);
              }
            }
          }
        }

        if (zoom > 18) {
          ctx.font = "5px monospace"; ctx.textAlign = "center";
          ctx.fillStyle = "rgb(255,220,120)";
          ctx.fillText("ARCTURUS", Math.floor(starSx), Math.floor(starSy) + Math.min(starScreenR, 12) + 8);
        }
      }

      // Planets
      if (zoom > 18) {
        const pFade = clamp((zoom - 18) / 8, 0, 1);
        PLANETS.forEach((pl, pi) => {
          const angle = t * pl.speed;
          const orbitR = Math.sqrt(pl.dx * pl.dx + pl.dy * pl.dy);
          const pwx = ARC_X + Math.cos(angle + pi * 1.5) * orbitR;
          const pwy = ARC_Y + Math.sin(angle + pi * 1.5) * orbitR * 0.5;
          const [psx, psy] = w2s(pwx, pwy, cam);

          if (psx < -10 || psx > RW + 10 || psy < -10 || psy > RH + 10) return;

          // Orbit path (dithered ellipse outline)
          if (zoom > 25 && zoom < 200) {
            const orSx = orbitR * zoom * SCALE;
            const orSy = orSx * 0.5;
            if (orSx > 5 && orSx < 200) {
              ctx.fillStyle = "rgb(40,50,70)";
              for (let a = 0; a < Math.PI * 2; a += 0.04) {
                const ox = Math.floor(starSx + Math.cos(a) * orSx);
                const oy = Math.floor(starSy + Math.sin(a) * orSy);
                if (ox >= 0 && ox < RW && oy >= 0 && oy < RH) {
                  if (ditherOk(ox, oy, 0.35 * pFade)) ctx.fillRect(ox, oy, 1, 1);
                }
              }
            }
          }

          // Planet body
          const pScreenR = Math.max(1, Math.round(pl.r * zoom * 0.01));
          if (pFade > 0.5 || ditherOk(Math.floor(psx), Math.floor(psy), pFade)) {
            const [cr, cg, cb] = pl.color;
            for (let dy = -pScreenR; dy <= pScreenR; dy++) {
              for (let dx = -pScreenR; dx <= pScreenR; dx++) {
                if (dx * dx + dy * dy <= pScreenR * pScreenR) {
                  ctx.fillStyle = `rgb(${cr},${cg},${cb})`;
                  ctx.fillRect(Math.floor(psx + dx), Math.floor(psy + dy), 1, 1);
                }
              }
            }
            // Ring
            if (pl.ring && pScreenR > 2) {
              ctx.fillStyle = "rgb(150,180,220)";
              for (let dx = -pScreenR - 3; dx <= pScreenR + 3; dx++) {
                ctx.fillRect(Math.floor(psx + dx), Math.floor(psy), 1, 1);
              }
            }
          }

          // Planet label
          if (zoom > 30 && pFade > 0.5) {
            ctx.font = "5px monospace"; ctx.textAlign = "center";
            ctx.fillStyle = "rgb(150,160,190)";
            ctx.fillText(pl.name, Math.floor(psx), Math.floor(psy) + pScreenR + 7);
          }
        });
      }

      // System info panel
      if (zoom > 20 && zoom < 200) {
        const panelFade = clamp((zoom - 20) / 10, 0, 1) * clamp(1 - (zoom - 120) / 80, 0, 1);
        if (panelFade > 0.3) {
          px(ctx, 4, 4, 80, 38, "rgb(8,12,24)");
          px(ctx, 4, 4, 80, 1, "rgb(74,111,165)");
          ctx.font = "bold 5px monospace"; ctx.textAlign = "left";
          ctx.fillStyle = "rgb(122,159,213)"; ctx.fillText("ARCTURUS PRIME", 8, 14);
          ctx.font = "5px monospace"; ctx.fillStyle = "rgb(100,100,120)";
          ctx.fillText("SOVEREIGNTY CORE", 8, 22);
          ctx.fillText("POP: 12.4B", 8, 30);
          ctx.fillText("IND: ████████░░", 8, 38);
        }
      }
    }

    // ══════════════ LAYER: FLEET ══════════════
    if (zoom > 60) {
      const fleetFade = clamp((zoom - 60) / 40, 0, 1);
      const fwx = ARC_X + FLEET_CENTER.dx;
      const fwy = ARC_Y + FLEET_CENTER.dy;
      const [fsx, fsy] = w2s(fwx, fwy, cam);

      if (fsx > -100 && fsx < RW + 100 && fsy > -100 && fsy < RH + 100) {
        const shipScale = Math.max(0.3, zoom * 0.003);

        // Fleet label
        if (zoom > 80 && zoom < 600) {
          ctx.font = "bold 5px monospace"; ctx.textAlign = "left"; ctx.fillStyle = "rgb(122,159,213)";
          ctx.fillText("3RD FLEET — SOVEREIGNTY", Math.floor(fsx) - 40, Math.floor(fsy) - 30);
          if (zoom > 100) {
            ctx.font = "5px monospace"; ctx.fillStyle = "rgb(80,90,120)";
            ctx.fillText("ADM. CHEN WEIMING", Math.floor(fsx) - 40, Math.floor(fsy) - 22);
            ctx.fillText("DOCTRINE: AGGRESSIVE", Math.floor(fsx) - 40, Math.floor(fsy) - 14);
          }
        }

        // Draw ships when they'd be at least a few pixels
        if (shipScale > 0.5 && fleetFade > 0.3) {
          const spread = Math.min(zoom * 0.015, 25);
          // Flagship dreadnought
          drawDreadnought(ctx, Math.floor(fsx), Math.floor(fsy), shipScale, true, t);
          if (zoom > 120) {
            ctx.font = "bold 4px monospace"; ctx.textAlign = "center"; ctx.fillStyle = "rgb(255,221,102)";
            ctx.fillText("★ ISS IRONCLAD", Math.floor(fsx), Math.floor(fsy) - Math.round(8 * shipScale) - 3);
          }
          // Escorts
          drawCruiser(ctx, Math.floor(fsx - spread * 2.5), Math.floor(fsy - spread * 1.5), shipScale * 0.7, t);
          drawCruiser(ctx, Math.floor(fsx - spread * 2.5), Math.floor(fsy + spread * 1.5), shipScale * 0.7, t);
          drawCruiser(ctx, Math.floor(fsx + spread * 2.5), Math.floor(fsy - spread * 1.5), shipScale * 0.7, t);
          drawCruiser(ctx, Math.floor(fsx + spread * 2.5), Math.floor(fsy + spread * 1.5), shipScale * 0.7, t);
          drawDestroyer(ctx, Math.floor(fsx - spread * 4), Math.floor(fsy - spread), shipScale * 0.6, t);
          drawDestroyer(ctx, Math.floor(fsx - spread * 4), Math.floor(fsy + spread), shipScale * 0.6, t);
          drawDestroyer(ctx, Math.floor(fsx + spread * 4), Math.floor(fsy - spread), shipScale * 0.6, t);
          drawDestroyer(ctx, Math.floor(fsx + spread * 4), Math.floor(fsy + spread), shipScale * 0.6, t);
          drawCorvette(ctx, Math.floor(fsx - spread * 5.5), Math.floor(fsy - spread * 0.3), shipScale * 0.5, t);
          drawCorvette(ctx, Math.floor(fsx - spread * 5.5), Math.floor(fsy + spread * 0.3), shipScale * 0.5, t);
          drawCorvette(ctx, Math.floor(fsx + spread * 5.5), Math.floor(fsy - spread * 0.3), shipScale * 0.5, t);
          drawCorvette(ctx, Math.floor(fsx + spread * 5.5), Math.floor(fsy + spread * 0.3), shipScale * 0.5, t);
          drawCarrier(ctx, Math.floor(fsx), Math.floor(fsy + spread * 3), shipScale * 0.8, t);
        } else if (fleetFade > 0.3) {
          // Small fleet marker
          ctx.fillStyle = "rgb(74,111,165)";
          ctx.fillRect(Math.floor(fsx) - 2, Math.floor(fsy) - 1, 4, 3);
          ctx.fillStyle = "rgb(122,159,213)";
          ctx.fillRect(Math.floor(fsx) - 1, Math.floor(fsy), 2, 1);
        }

        // Fleet roster panel
        if (zoom > 150 && zoom < 700) {
          const rp = clamp((zoom - 150) / 50, 0, 1) * clamp(1 - (zoom - 500) / 200, 0, 1);
          if (rp > 0.3) {
            px(ctx, RW - 90, 4, 86, 66, "rgb(8,12,24)");
            px(ctx, RW - 90, 4, 86, 1, "rgb(74,111,165)");
            ctx.font = "bold 5px monospace"; ctx.textAlign = "left"; ctx.fillStyle = "rgb(122,159,213)";
            ctx.fillText("FLEET ROSTER", RW - 86, 14);
            ctx.font = "4px monospace"; ctx.fillStyle = "rgb(120,130,160)";
            ["DREADNOUGHT x1 ★", "CRUISER     x4", "DESTROYER   x4", "CORVETTE    x4", "CARRIER     x1"].forEach((l, i) => {
              ctx.fillText(l, RW - 86, 24 + i * 8);
            });
            ctx.fillStyle = "rgb(80,90,110)"; ctx.fillText("TOTAL: 14 SHIPS", RW - 86, 64);
          }
        }
      }
    }

    // ══════════════ LAYER: SHIP DETAIL ══════════════
    if (zoom > 500) {
      const shipFade = clamp((zoom - 500) / 200, 0, 1);

      // Admiral panel
      if (shipFade > 0.3) {
        px(ctx, 4, 4, 100, 90, "rgb(8,12,24)");
        px(ctx, 4, 4, 100, 1, "rgb(255,221,102)");
        drawAdmiral(ctx, 8, 10);
        ctx.font = "bold 5px monospace"; ctx.textAlign = "left"; ctx.fillStyle = "rgb(255,221,102)";
        ctx.fillText("ADM. CHEN WEIMING", 22, 14);
        ctx.font = "4px monospace"; ctx.fillStyle = "rgb(120,130,160)";
        ctx.fillText("RANK: GRAND ADMIRAL", 22, 22);
        ctx.fillText("ABILITY: IRON WALL", 22, 30);
        ctx.fillStyle = "rgb(100,140,190)";
        ctx.fillText("TAC ████████░░ 82", 8, 42);
        ctx.fillText("CMD █████████░ 91", 8, 50);
        ctx.fillText("CHA █████░░░░░ 54", 8, 58);
        ctx.fillText("INT ██████░░░░ 63", 8, 66);
        ctx.fillText("LOY █████████░ 95", 8, 74);

        // Module panel
        px(ctx, RW - 100, 4, 96, 80, "rgb(8,12,24)");
        px(ctx, RW - 100, 4, 96, 1, "rgb(74,111,165)");
        ctx.font = "bold 4px monospace"; ctx.fillStyle = "rgb(122,159,213)";
        ctx.fillText("MODULES [4/4]", RW - 96, 13);
        ctx.font = "4px monospace";
        [
          { name: "REINFORCED ARMOR III", c: "rgb(170,102,34)", desc: "+40% HULL" },
          { name: "ECM SUITE II", c: "rgb(34,170,102)", desc: "NULL TARGETING" },
          { name: "ADV TARGETING IV", c: "rgb(170,34,68)", desc: "+35% ACCURACY" },
          { name: "WARP DISRUPTOR I", c: "rgb(102,34,170)", desc: "NO RETREAT" },
        ].forEach((m, i) => {
          px(ctx, RW - 96, 17 + i * 16, 4, 4, m.c);
          ctx.fillStyle = "rgb(180,190,210)"; ctx.fillText(m.name, RW - 90, 21 + i * 16);
          ctx.fillStyle = "rgb(80,90,110)"; ctx.fillText(m.desc, RW - 90, 28 + i * 16);
        });

        // Ship status
        px(ctx, RW - 100, 88, 96, 36, "rgb(8,12,24)");
        px(ctx, RW - 100, 88, 96, 1, "rgb(74,111,165)");
        ctx.font = "bold 4px monospace"; ctx.fillStyle = "rgb(122,159,213)";
        ctx.fillText("SHIP STATUS", RW - 96, 97);
        ctx.font = "4px monospace";
        ctx.fillStyle = "rgb(74,138,74)"; ctx.fillText("HULL   ████████████░░ 88%", RW - 96, 106);
        ctx.fillStyle = "rgb(90,122,181)"; ctx.fillText("SHIELD ██████████████ 100%", RW - 96, 114);
        ctx.fillStyle = "rgb(170,138,34)"; ctx.fillText("FUEL   ██████████░░░░ 74%", RW - 96, 122);
      }

      // Ship name
      if (shipFade > 0.5) {
        const [fsx2, fsy2] = w2s(ARC_X + FLEET_CENTER.dx, ARC_Y + FLEET_CENTER.dy, cam);
        ctx.font = "bold 5px monospace"; ctx.textAlign = "center"; ctx.fillStyle = "rgb(255,221,102)";
        ctx.fillText("★ ISS IRONCLAD — DREADNOUGHT CLASS", Math.floor(fsx2), Math.min(RH - 30, Math.floor(fsy2) + 40));
        ctx.font = "4px monospace"; ctx.fillStyle = "rgb(100,140,190)";
        ctx.fillText("THE SOVEREIGNTY • 3RD FLEET FLAGSHIP", Math.floor(fsx2), Math.min(RH - 22, Math.floor(fsy2) + 48));
      }
    }

    // ══════════════ UI OVERLAY ══════════════
    const [tierLabel, tierDesc] = getTierLabel(zoom);

    // Top bar
    px(ctx, 0, 0, RW, 12, "rgb(4,6,14)");
    px(ctx, 0, 11, RW, 1, "rgb(20,30,50)");
    ctx.font = "bold 5px monospace"; ctx.textAlign = "left";
    ctx.fillStyle = "rgb(122,159,213)";
    ctx.fillText("ADMIRALS OF THE VOID", 4, 8);
    ctx.textAlign = "right"; ctx.fillStyle = "rgb(200,180,80)";
    ctx.fillText(tierLabel, RW - 4, 8);

    // Bottom bar
    px(ctx, 0, RH - 10, RW, 10, "rgb(4,6,14)");
    px(ctx, 0, RH - 10, RW, 1, "rgb(20,30,50)");
    ctx.font = "4px monospace"; ctx.textAlign = "left";
    ctx.fillStyle = "rgb(60,70,90)";
    ctx.fillText(tierDesc, 4, RH - 3);
    ctx.textAlign = "right";
    ctx.fillText(`ZOOM: ${zoom.toFixed(1)}x`, RW - 4, RH - 3);

    // Zoom rail (right side)
    const railX = RW - 8, railY = 20, railH = RH - 40;
    px(ctx, railX, railY, 3, railH, "rgb(10,15,25)");
    const logPos = (Math.log(zoom) - Math.log(MIN_ZOOM)) / (Math.log(MAX_ZOOM) - Math.log(MIN_ZOOM));
    const thumbY = railY + logPos * (railH - 6);
    px(ctx, railX - 1, Math.floor(thumbY), 5, 5, "rgb(74,111,165)");
    px(ctx, railX, Math.floor(thumbY) + 1, 3, 3, "rgb(122,159,213)");
    // Tier markers on rail
    [1, 3, 12, 80, 500].forEach((tz) => {
      const tp = (Math.log(tz) - Math.log(MIN_ZOOM)) / (Math.log(MAX_ZOOM) - Math.log(MIN_ZOOM));
      px(ctx, railX - 2, Math.floor(railY + tp * (railH - 2)), 7, 1, "rgb(30,40,60)");
    });

    // Minimap (when zoomed in)
    if (zoom > 4) {
      const mmW = 40, mmH = 24;
      const mmX = 4, mmY = RH - mmH - 14;
      px(ctx, mmX - 1, mmY - 1, mmW + 2, mmH + 2, "rgb(15,20,35)");
      px(ctx, mmX, mmY, mmW, mmH, "rgb(6,8,16)");
      // Mini galaxy dots
      const mmStars = starsRef.current;
      const mmScale = mmW / 120;
      for (let i = 0; i < mmStars.length; i += 6) {
        const s = mmStars[i];
        const mx = mmX + mmW / 2 + s.x * mmScale;
        const my = mmY + mmH / 2 + s.y * mmScale;
        if (mx >= mmX && mx < mmX + mmW && my >= mmY && my < mmY + mmH) {
          ctx.fillStyle = `rgb(${Math.floor(s.r * 0.5)},${Math.floor(s.g * 0.5)},${Math.floor(s.b * 0.5)})`;
          ctx.fillRect(Math.floor(mx), Math.floor(my), 1, 1);
        }
      }
      // Camera position marker
      const cmx = mmX + mmW / 2 + cam.x * mmScale;
      const cmy = mmY + mmH / 2 + cam.y * mmScale;
      if (Math.sin(t * 0.005) > -0.3) {
        ctx.fillStyle = "rgb(255,255,100)";
        ctx.fillRect(Math.floor(cmx), Math.floor(cmy), 1, 1);
      }
    }

    // Scale bar
    if (zoom < 30) {
      const scaleKly = zoom < 3 ? 10 : zoom < 10 ? 5 : 1;
      const scalePx = scaleKly * zoom * SCALE;
      if (scalePx > 10 && scalePx < 150) {
        const sbY = RH - 16;
        const sbX = RW / 2 - scalePx / 2;
        ctx.fillStyle = "rgb(60,70,90)";
        px(ctx, Math.floor(sbX), sbY, Math.floor(scalePx), 1, "rgb(60,70,90)");
        px(ctx, Math.floor(sbX), sbY - 2, 1, 5, "rgb(60,70,90)");
        px(ctx, Math.floor(sbX + scalePx), sbY - 2, 1, 5, "rgb(60,70,90)");
        ctx.font = "4px monospace"; ctx.textAlign = "center"; ctx.fillStyle = "rgb(60,70,90)";
        ctx.fillText(`${scaleKly} kly`, Math.floor(sbX + scalePx / 2), sbY - 4);
      }
    }

    // ═══ BLIT TO DISPLAY ═══
    dCtx.drawImage(offscreen, 0, 0, display.width, display.height);

    frameRef.current = requestAnimationFrame(render);
  }, [w2s, getTierLabel]);

  // Start loop
  useEffect(() => {
    frameRef.current = requestAnimationFrame(render);
    return () => { if (frameRef.current) cancelAnimationFrame(frameRef.current); };
  }, [render]);

  // ═══ INPUT HANDLERS ═══
  const screenToRender = useCallback((clientX, clientY) => {
    const rect = displayRef.current.getBoundingClientRect();
    return [
      (clientX - rect.left) / rect.width * RW,
      (clientY - rect.top) / rect.height * RH,
    ];
  }, []);

  // Wheel zoom toward cursor
  useEffect(() => {
    const el = displayRef.current;
    if (!el) return;
    const onWheel = (e) => {
      e.preventDefault();
      const [mx, my] = screenToRender(e.clientX, e.clientY);
      const cam = targetCamRef.current;
      // World coords under mouse
      const wx = cam.x + (mx - RW / 2) / (cam.zoom * SCALE);
      const wy = cam.y + (my - RH / 2) / (cam.zoom * SCALE);
      // Zoom
      const factor = e.deltaY > 0 ? 1.12 : 1 / 1.12;
      const newZoom = clamp(cam.zoom * factor, MIN_ZOOM, MAX_ZOOM);
      // Adjust camera so world point stays under mouse
      targetCamRef.current = {
        x: wx - (mx - RW / 2) / (newZoom * SCALE),
        y: wy - (my - RH / 2) / (newZoom * SCALE),
        zoom: newZoom,
      };
    };
    el.addEventListener("wheel", onWheel, { passive: false });
    return () => el.removeEventListener("wheel", onWheel);
  }, [screenToRender]);

  // Drag to pan
  useEffect(() => {
    const el = displayRef.current;
    if (!el) return;
    const onDown = (e) => {
      const [mx, my] = screenToRender(e.clientX, e.clientY);
      dragRef.current = { active: true, sx: mx, sy: my, cx: targetCamRef.current.x, cy: targetCamRef.current.y };
    };
    const onMove = (e) => {
      if (!dragRef.current.active) return;
      const [mx, my] = screenToRender(e.clientX, e.clientY);
      const d = dragRef.current;
      const zoom = targetCamRef.current.zoom;
      targetCamRef.current = {
        ...targetCamRef.current,
        x: d.cx - (mx - d.sx) / (zoom * SCALE),
        y: d.cy - (my - d.sy) / (zoom * SCALE),
      };
    };
    const onUp = () => { dragRef.current.active = false; };
    el.addEventListener("mousedown", onDown);
    el.addEventListener("touchstart", (e) => {
      if (e.touches.length === 1) {
        onDown({ clientX: e.touches[0].clientX, clientY: e.touches[0].clientY });
      }
    });
    window.addEventListener("mousemove", onMove);
    window.addEventListener("touchmove", (e) => {
      if (e.touches.length === 1) {
        onMove({ clientX: e.touches[0].clientX, clientY: e.touches[0].clientY });
      }
    });
    window.addEventListener("mouseup", onUp);
    window.addEventListener("touchend", onUp);
    return () => {
      el.removeEventListener("mousedown", onDown);
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
    };
  }, [screenToRender]);

  // Double-click to zoom into Arcturus
  const handleDblClick = useCallback(() => {
    const cam = targetCamRef.current;
    if (cam.zoom < 5) {
      targetCamRef.current = { x: ARC_X, y: ARC_Y, zoom: 8 };
    } else if (cam.zoom < 30) {
      targetCamRef.current = { x: ARC_X, y: ARC_Y, zoom: 60 };
    } else if (cam.zoom < 150) {
      targetCamRef.current = { x: ARC_X + FLEET_CENTER.dx, y: ARC_Y + FLEET_CENTER.dy, zoom: 300 };
    } else if (cam.zoom < 600) {
      targetCamRef.current = { x: ARC_X + FLEET_CENTER.dx, y: ARC_Y + FLEET_CENTER.dy, zoom: 1200 };
    } else {
      targetCamRef.current = { x: 0, y: 0, zoom: 1 };
    }
  }, []);

  return (
    <div style={{
      width: "100%", height: "100vh", background: "#020308",
      display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center",
      overflow: "hidden", position: "relative",
    }}>
      <canvas
        ref={displayRef}
        width={RW * 2}
        height={RH * 2}
        onDoubleClick={handleDblClick}
        style={{
          width: "100%",
          maxWidth: RW * 3,
          aspectRatio: `${RW}/${RH}`,
          imageRendering: "pixelated",
          cursor: dragRef.current?.active ? "grabbing" : "grab",
        }}
      />
      <div style={{
        position: "absolute", bottom: 8, left: "50%", transform: "translateX(-50%)",
        fontFamily: "'Courier New', monospace", fontSize: 10, color: "#2a3a55",
        letterSpacing: 1, pointerEvents: "none", textAlign: "center",
      }}>
        SCROLL TO ZOOM • DRAG TO PAN • DOUBLE-CLICK TO JUMP
      </div>
    </div>
  );
}
