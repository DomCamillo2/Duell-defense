from pathlib import Path
import json,hashlib,subprocess,tempfile,os,datetime
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3];OUT=ROOT/'Tools/NetworkTests/Evidence16'/datetime.datetime.now(datetime.timezone.utc).strftime('wire10-%Y%m%d-%H%M%S');OUT.mkdir(parents=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
files=list((ROOT/'Assets/Core').glob('*.cs'))+[ROOT/'Assets/Networking'/n for n in ['WireProtocol.cs','WireSnapshotValidator.cs']]
sources={str(p.relative_to(ROOT)):p.read_bytes() for p in files}
SDK='/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
with tempfile.TemporaryDirectory(prefix='wire10-goldens-') as tmp:
 w=Path(tmp)
 for path,data in sources.items():(w/Path(path).name).write_bytes(data)
 (w/'Generate.cs').write_bytes((HERE/'Generate.cs').read_bytes())
 (w/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 (w/'Goldens.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
 env={**os.environ,'DOTNET_CLI_HOME':'/tmp/defense-host-rules-dotnet','DOTNET_CLI_TELEMETRY_OPTOUT':'1'}
 cmd=[SDK,'build',str(w/'Goldens.csproj'),'--output',str(w/'out'),'-p:UseSharedCompilation=false'];b=subprocess.run(cmd,capture_output=True,text=True,env=env)
 (OUT/'build.log').write_text(b.stdout+b.stderr)
 if b.returncode:print(b.stdout+b.stderr);raise SystemExit(b.returncode)
 dll=w/'out/Goldens.dll';h=sha(dll);env.update(COREHOST_TRACE='1',COREHOST_TRACEFILE=str(OUT/'hosttrace.txt'))
 cmd=[SDK,str(dll),str(OUT/'goldens.json')];r=subprocess.run(cmd,capture_output=True,text=True,env=env);(OUT/'run.log').write_text(r.stdout+r.stderr)
 changed=[p for p,data in sources.items() if (ROOT/p).read_bytes()!=data]
 report={'scope':'Actual current production C# codec pure goldens, no Unity/network','sources':{p:hashlib.sha256(data).hexdigest() for p,data in sources.items()},'sourcesChangedDuringRun':changed,'generatorSha256':sha(HERE/'Generate.cs'),'buildExit':b.returncode,'executionCommand':cmd,'executionExit':r.returncode,'dllSha256Before':h,'dllSha256After':sha(dll),'hostTraceHasExactTarget':str(dll) in (OUT/'hosttrace.txt').read_text()}
 if (OUT/'goldens.json').exists():report['goldensSha256']=sha(OUT/'goldens.json')
 (OUT/'manifest.json').write_text(json.dumps(report,indent=2)+'\n');print(r.stdout+r.stderr);print(OUT)
 if r.returncode==0 and not changed:(HERE/'goldens.json').write_bytes((OUT/'goldens.json').read_bytes())
 raise SystemExit(r.returncode or bool(changed))
