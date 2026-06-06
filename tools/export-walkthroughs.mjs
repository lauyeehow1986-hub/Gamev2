import { readFile, writeFile, mkdir, rm } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

// gamev2 root = parent of this tools/ dir.
const HERE = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'));
const ROOT = path.resolve(HERE, '..');
const SRC = path.join(ROOT, 'Game-source', 'src', 'lib');
const OUT = path.join(ROOT, 'UnityProject', 'Assets', 'SGPathway', 'Content', 'Walkthroughs');

async function loadConst(tsFile, exportName) {
  const raw = await readFile(tsFile, 'utf8');
  const js = raw
    .replace(/^\s*import\s+type[\s\S]*?;\s*$/m, '')                 // drop type-only import
    .replace(new RegExp(`export const ${exportName}\\s*:\\s*Walkthrough`), `export const ${exportName}`)
    .replace(/\bas const\b/g, '')                                   // strip any "as const"
    .replace(/ satisfies [A-Za-z0-9_<>\[\]]+/g, '');               // strip any "satisfies T"
  const tmp = tsFile + '.tmp.mjs';
  await writeFile(tmp, js, 'utf8');
  try { return (await import(pathToFileURL(tmp).href))[exportName]; }
  finally { await rm(tmp, { force: true }); }
}

const str = (v) => (v == null ? '' : String(v));

function beatDTO(b) {
  const sp = b.showpiece;
  return {
    at: b.at,
    actorRef: b.actorId,
    action: str(b.action),
    hasPos: !!b.pos,
    posX: b.pos ? b.pos.x : 0,
    posY: b.pos ? b.pos.y : 0,
    direction: str(b.direction || 'S'),
    walking: !!b.walking,
    pose: str(b.pose || 'stand'),
    expression: str(b.expression || 'neutral'),
    showpieceKind: sp ? sp.kind : '',
    showpieceSvgId: sp && sp.kind === 'svg' ? sp.id : '',
  };
}

function chapterDTO(key, c) {
  const bp = c.branchPoint;
  return {
    key,
    id: str(c.id),
    title: str(c.title),
    scene: str(c.scene || ''),
    durationSec: c.durationSec,
    timeOfDay: str(c.timeOfDay),
    location: str(c.location),
    hasDefaultNext: !!c.defaultNextChapterId,
    defaultNextChapterRef: str(c.defaultNextChapterId),
    hasBranchPoint: !!bp,
    branchPoint: bp ? {
      prompt: str(bp.prompt),
      options: bp.options.map(o => ({ label: str(o.label), hint: str(o.hint), nextChapterRef: str(o.nextChapterId) })),
    } : { prompt: '', options: [] },
    beats: c.beats.map(beatDTO),
  };
}

function toDTO(w) {
  return {
    id: w.id,
    title: str(w.title),
    startChapterRef: str(w.startChapterId),
    actors: Object.entries(w.actors).map(([key, a]) => ({
      key, id: str(a.id), role: str(a.role), team: str(a.team), bio: str(a.bio), swatch: str(a.swatch || '#ffffff'),
    })),
    chapters: Object.entries(w.chapters).map(([key, c]) => chapterDTO(key, c)),
  };
}

async function run() {
  const jobs = [
    ['walkthrough-stemi.ts', 'stemiWalkthrough', 'Stemi'],
    ['walkthrough-stroke.ts', 'strokeWalkthrough', 'Stroke'],
  ];
  for (const [file, name, folder] of jobs) {
    const w = await loadConst(path.join(SRC, file), name);
    const dto = toDTO(w);
    const dir = path.join(OUT, folder);
    await mkdir(dir, { recursive: true });
    await writeFile(path.join(dir, '_source.json'), JSON.stringify(dto, null, 2), 'utf8');
    console.log(`${folder}: ${dto.actors.length} actors, ${dto.chapters.length} chapters -> ${path.join(dir, '_source.json')}`);
  }
}
run().catch(e => { console.error(e); process.exit(1); });
