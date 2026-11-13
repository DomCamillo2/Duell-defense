#!/usr/bin/env python3
"""Launch owned independent 4/6-peer acceptance processes. Never overrides old evidence."""
import argparse
import json
from pathlib import Path
import subprocess
import time
from run_lan import compare_team as compare, exact_start_gold


def main():
    p = argparse.ArgumentParser()
    p.add_argument('player'); p.add_argument('--output', required=True)
    p.add_argument('--format', choices=('2v2', '3v3', '2v2v2'), default='2v2')
    p.add_argument('--map', type=int, choices=range(6), default=0)
    p.add_argument('--income', type=int, choices=(1, 2, 3, 5, 10), default=3)
    p.add_argument('--start-gold', type=exact_start_gold, default='1000')
    p.add_argument('--relay', action='store_true'); p.add_argument('--port', type=int, default=7790)
    p.add_argument('--endless', action='store_true'); p.add_argument('--start-only', action='store_true')
    p.add_argument('--features16', action='store_true', help='Exercise real atomic10/Max, stale batch and both elite paid/research packets; requires10000G.')
    p.add_argument('--timeout', type=int, default=450)
    p.add_argument('--protocol', choices=('auto','9','10'), default='auto', help='Require actual reported protocol; Core/wave-plan binding is checked before dispatch.')
    a = p.parse_args()
    if a.features16 and (a.start_only or int(a.start_gold)<10000):p.error('Feature scenario requires finite gameplay and at least10000G.')
    if a.endless and not a.start_only: p.error('Endless uses explicit --start-only; finite mode proves two completed rounds.')
    if not a.start_only and int(a.start_gold) < 1000: p.error('Required purchase scenario needs at least1000 exact start gold.')
    out = Path(a.output).resolve(); out.mkdir(parents=True, exist_ok=True)
    if list(out.iterdir()): p.error('Use a new empty evidence directory.')
    fmt, count = {'2v2': (1, 4), '3v3': (2, 6), '2v2v2': (3, 6)}[a.format]
    processes, logs, failures = [], [], []
    common = ['-batchmode', '-nographics', '--duel-team-test', '--duel-format', str(fmt), '--duel-map', str(a.map), '--duel-income', str(a.income), '--duel-start-gold', a.start_gold, '--duel-port', str(a.port), '--duel-code-file', str(out/'relay-code.json')]
    if a.features16: common += ['--duel-features16']
    if a.endless: common += ['--duel-endless']
    if a.start_only: common += ['--duel-team-start-only']
    try:
        for index in range(count):
            role = ('--duel-relay-' if a.relay else '--duel-')+('host' if index == 0 else 'client')
            log = open(out/f'peer-{index}.stdout.log', 'w'); logs.append(log)
            command = [str(Path(a.player).resolve()), *common, role, '--duel-peer-index', str(index), '--duel-report', str(out/f'peer-{index}.json'), '-logFile', str(out/f'peer-{index}.unity.log')]
            processes.append(subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT))
            time.sleep(1 if index == 0 else .25)
        deadline = time.monotonic()+a.timeout
        while any(proc.poll() is None for proc in processes) and time.monotonic() < deadline:
            time.sleep(.25)
        for index, proc in enumerate(processes):
            if proc.poll() is None: failures.append(f'peer{index} timeout'); proc.terminate()
            elif proc.returncode: failures.append(f'peer{index} exit{proc.returncode}')
    finally:
        for proc in processes:
            if proc.poll() is None:
                proc.terminate()
                try: proc.wait(timeout=5)
                except subprocess.TimeoutExpired: proc.kill(); proc.wait()
        for log in logs: log.close()
    reports = []
    for index in range(count):
        try: reports.append(json.loads((out/f'peer-{index}.json').read_text()))
        except (OSError, ValueError) as error: failures.append(f'peer{index} report: {error}')
    result = compare(reports, protocol=a.protocol, expected_format=fmt, expected_map=a.map, expected_income=a.income, expected_start_gold=a.start_gold, expected_start_only=a.start_only)
    result['problems'] += failures; result['passed'] = result['passed'] and not failures
    if failures: result['actual_transport_proven'] = False
    (out/'comparison.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2)); return 0 if result['passed'] else 1


if __name__ == '__main__': raise SystemExit(main())
