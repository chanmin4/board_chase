// Compile the observer alone, without replacing Unity's loaded assemblies.
const fs = require('fs'), cp = require('child_process');
const walk = dir => fs.readdirSync(dir, {withFileTypes: true}).flatMap(entry =>
  entry.isDirectory() ? walk(dir + '/' + entry.name) : [dir + '/' + entry.name]);
const source = walk('Library/Bee/artifacts').filter(path => path.endsWith('/Assembly-CSharp-Editor.rsp'))
  .sort((a, b) => fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs)[0];
if (!source) throw Error('No current Unity Editor compiler response file.');
const folder = 'Library/CameraDiagnosis';
fs.mkdirSync(folder, {recursive: true});
const lines = fs.readFileSync(source, 'utf8').split(/\r?\n/).filter(line =>
  /^[-/]/.test(line) && !/^[-/](out|refout|analyzer|additionalfile):/.test(line));
lines.push('-out:"' + folder + '/CameraRuntimeTrace.dll"', '"Assets/Editor/CameraRuntimeTrace.cs"');
fs.writeFileSync(folder + '/trace.rsp', lines.join('\n'));
const unity = 'E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
const result = cp.spawnSync(unity + '/NetCoreRuntime/dotnet.exe',
  [unity + '/DotNetSdkRoslyn/csc.dll', '@' + folder + '/trace.rsp'],
  {encoding: 'utf8', windowsHide: true, maxBuffer: 4 * 1024 * 1024});
fs.writeFileSync(folder + '/compile.log', (result.stdout || '') + (result.stderr || '') + (result.error || ''));
console.log(result.stdout || '', result.stderr || '', result.error || '');
process.exit(result.status === 0 ? 0 : 1);
