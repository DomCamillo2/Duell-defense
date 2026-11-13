#!/usr/bin/env python3
"""Reproducible pure production-Core/Wire9 fixture generator; never launches Unity or claims transport."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET


def nested(source, name):
    marker = 'public sealed class '+name
    start = source.index(marker)
    brace = source.index('{', start)
    depth, end = 1, brace+1
    while depth:
        if source[end] == '{': depth += 1
        elif source[end] == '}': depth -= 1
        end += 1
    return source[start:end]


def main():
    p = argparse.ArgumentParser(); p.add_argument('--dotnet', required=True); p.add_argument('--output', required=True)
    a = p.parse_args(); root = Path(__file__).resolve().parents[2]; output = Path(a.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    if list(output.iterdir()): p.error('Use an empty new model evidence directory.')
    driver = root/'Assets/Networking/NetworkTestDriver.cs'; team = root/'Assets/Networking/TeamNetworkTestDriver.cs'
    # Exact report DTO declarations extracted from current runtime source; simulation and codec compiled directly.
    dto = 'using System;using System.Collections.Generic;using DefenseDuell.Core;namespace DefenseDuell.Networking{public static class NetworkTestDriver{'
    for name in ('Checkpoint','Frame','TowerSample','Ack','CommandProof','AuthorityProof'): dto += nested(driver.read_text(), name)
    dto += '}public static class TeamNetworkTestDriver{'+nested(team.read_text(),'Report')+nested(team.read_text(),'Round')+'}}'
    (output/'EvidenceTypes.cs').write_text(dto)
    (output/'Program.cs').write_text('class Program{static void Main(string[] args){TeamEvidenceModel15.Run(args[0]);}}')
    files = sorted((root/'Assets/Core').glob('*.cs'))+[root/p for p in ('Assets/Networking/WireProtocol.cs','Assets/Networking/WireSnapshotValidator.cs','Assets/Tests/EditMode/Networking/WireFixtures.cs','Tools/NetworkTests/TeamEvidenceModel15.cs')]
    bound = files+[driver,team,Path(__file__).resolve()]
    manifest = {str(f.relative_to(root)):hashlib.sha256(f.read_bytes()).hexdigest() for f in bound}
    (output/'source-manifest.json').write_text(json.dumps(manifest,indent=2))
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk'); props = ET.SubElement(project,'PropertyGroup')
    for key,value in {'OutputType':'Exe','TargetFramework':'net8.0','EnableDefaultCompileItems':'false','Nullable':'disable'}.items(): ET.SubElement(props,key).text=value
    group = ET.SubElement(project,'ItemGroup')
    for f in files+[output/'EvidenceTypes.cs',output/'Program.cs']: ET.SubElement(group,'Compile',Include=str(f))
    ET.ElementTree(project).write(output/'TeamModel.csproj')
    env = dict(os.environ,DOTNET_CLI_HOME=str(output/'dotnet-home'))
    command = [a.dotnet,'build',str(output/'TeamModel.csproj'),'-o',str(output/'bin')]
    built = subprocess.run(command,env=env,capture_output=True,text=True);(output/'build.log').write_text(built.stdout+built.stderr)
    if built.returncode: print(built.stdout+built.stderr);return built.returncode
    current = {str(f.relative_to(root)):hashlib.sha256(f.read_bytes()).hexdigest() for f in bound}
    if current != manifest: raise RuntimeError('Sources changed during model compilation')
    command = [a.dotnet,str(output/'bin/TeamModel.dll'),str(output/'reports')]
    run = subprocess.run(command,env=env,capture_output=True,text=True);(output/'run.log').write_text(run.stdout+run.stderr)
    print(run.stdout+run.stderr)
    if run.returncode:return run.returncode
    from run_lan import compare_team as compare
    results={}
    for f in range(1,4):
        for endless in ('False','True'):
            reports=[json.loads(path.read_text()) for path in sorted((output/'reports').glob(f'format{f}-endless{endless}-peer*.json'))]
            result=compare(reports,allow_model=True);result['rejected_as_actual_transport']=not compare(reports)['passed'];results[f'{f}-{endless}']=result
    (output/'comparison-model-only.json').write_text(json.dumps(results,indent=2))
    return 0 if all(r['passed'] and r['rejected_as_actual_transport'] for r in results.values()) else 1


if __name__=='__main__':sys.exit(main())
