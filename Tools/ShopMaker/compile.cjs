// Uses this checkout's Unity response files. Does not write ScriptAssemblies or save any assets.
const fs = require('fs'), cp = require('child_process');
const out = 'Library/ShopMakerVerification';
const unity = 'E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
fs.mkdirSync(out, { recursive: true });
const walk = d => fs.readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(d + '/' + e.name) : [d + '/' + e.name]);
const rspPath = walk('Library/Bee/artifacts').filter(p => p.endsWith('/Assembly-CSharp-Editor.rsp')).sort((a,b) => fs.statSync(b).mtimeMs-fs.statSync(a).mtimeMs)[0];
if (!rspPath) throw new Error('Open this Unity project once to generate compiler response files.');
let rsp = fs.readFileSync(rspPath, 'utf8').replace(/^-out:.*$/m, `-out:"${out}/Assembly-CSharp-Editor.dll"`)
    .replace(/^-refout:.*\r?\n/m, '').replace(/^[-/]analyzer:.*\r?\n/gm, '').replace(/^[-/]additionalfile:.*\r?\n/gm, '')
    .split(/\r?\n/).filter(l => !(l.startsWith('"Assets/') && l.endsWith('.cs"') && !fs.existsSync(l.slice(1, -1)))).join('\n');
for (const file of [...walk('Assets/Editor'), ...walk('Assets/Tests/EditMode/Editor'), ...walk('Assets/Scripts/Authoring/Editor')]
    .filter(p => /\/(Shop|Quest|SectorMaker).*\.cs$/.test(p)))
    if (!rsp.includes('"' + file + '"')) rsp += '\n"' + file + '"';
fs.writeFileSync(out + '/editor.rsp', rsp);
const r = cp.spawnSync(unity + '/NetCoreRuntime/dotnet.exe', [unity + '/DotNetSdkRoslyn/csc.dll', '@' + out + '/editor.rsp'],
    { encoding: 'utf8', maxBuffer: 20 * 1024 * 1024, windowsHide: true });
fs.writeFileSync(out + '/compile.log', (r.stdout || '') + (r.stderr || '') + (r.error || ''));
console.log(r.stdout, r.stderr, r.error || '', 'exit=' + r.status);
process.exit(r.error ? 1 : r.status);
