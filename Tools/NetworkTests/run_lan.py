#!/usr/bin/env python3
"""Launch only owned headless processes; verify versioned command/state evidence without rewriting historical reports."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
import time
from compare_v7 import compare as compare_v7
import re


def select_report_comparator(reports, *, team=False, protocol="auto"):
    """Choose solely from agreeing actual report version metadata; no byte/header rewriting."""
    import importlib
    if not isinstance(reports,(list,tuple)) or not reports or (not team and len(reports)!=2):
        raise ValueError('Independent reports required.')
    if team and len(reports) not in (4,6):raise ValueError('Four or six team reports required.')
    actual=reports[0].get('protocol')
    allowed=(9,10) if team else (7,8,9,10)
    if type(actual) is not int or actual not in allowed or any(type(r.get('protocol')) is not int or r['protocol']!=actual for r in reports):
        raise ValueError('Missing, mismatched or unexpected report protocol.')
    if protocol!='auto':
        try:expected=int(protocol)
        except (ValueError,TypeError):raise ValueError('Invalid protocol selector.')
        if actual!=expected:raise ValueError('Requested protocol differs from actual reports.')
    if actual in (9,10):
        core=reports[0].get('rulesVersion');plan=reports[0].get('wavePlanVersion')
        supported=('defense-duell-core-v2.4','defense-duell-core-v2.4.1') if actual==9 else ('defense-duell-core-v2.5',)
        expected_plan='authored-then-exact-endless-v3' if actual==9 else 'authored-then-counter-profiles-boss10-v4'
        if core not in supported or plan!=expected_plan or any(r.get('rulesVersion')!=core or r.get('wavePlanVersion')!=plan for r in reports):
            raise ValueError('Report protocol/Core/wave-plan versions disagree or are unsupported.')
    module=('compare_team_v'+str(actual)) if team else ('compare_duel_v'+str(actual) if actual>=9 else 'compare_v'+str(actual))
    return importlib.import_module(module).compare


def _dispatch_failure(reports,error):
    actual=reports[0].get('protocol') if reports and isinstance(reports[0],dict) else None
    return dict(passed=False,protocol=actual,problems=[str(error)],rounds=[],actual_transport_proven=False,startup_only_proven=False,last_wave_proven=False)


def compare(host, client, *args, protocol="auto", require_host_rules=False, expected_waves=None, expected_start_gold=None, expected_start_only=None, **kwargs):
    reports=[host,client]
    try:chosen=select_report_comparator(reports,protocol=protocol)
    except (ValueError,TypeError,AttributeError) as error:return _dispatch_failure(reports,error)
    if host['protocol']==7:
        if require_host_rules or any(v is not None for v in (expected_waves,expected_start_gold,expected_start_only)):
            return _dispatch_failure(reports,'Configured host rules require actual Wire8/9/10 evidence.')
        return chosen(host,client,*args,**kwargs)
    return chosen(host,client,*args,require_host_rules=require_host_rules,expected_waves=expected_waves,expected_start_gold=expected_start_gold,expected_start_only=expected_start_only,**kwargs)


def compare_team(reports, *, protocol="auto", **kwargs):
    try:chosen=select_report_comparator(reports,team=True,protocol=protocol)
    except (ValueError,TypeError,AttributeError) as error:return _dispatch_failure(reports,error)
    return chosen(reports,**kwargs)


def exact_start_gold(value):
    # Bound textual work before conversion. Never send this exact currency through float.
    if len(value) > 157800 or re.fullmatch(r"0|[1-9][0-9]*", value, flags=re.ASCII) is None:
        raise argparse.ArgumentTypeError("Start gold must be a canonical nonnegative decimal integer within the rules payload limit.")
    if hasattr(sys, "set_int_max_str_digits"):
        sys.set_int_max_str_digits(0)
    if int(value).bit_length() > 65523 * 8:
        raise argparse.ArgumentTypeError("Start gold exceeds the 65523-byte unsigned rules limit.")
    return value


def add_version_arguments(parser):
    parser.add_argument("--protocol", choices=("auto", "7", "8", "9", "10"), default="auto", help="Select comparison from actual report protocol, or require a specific wire version.")
    parser.add_argument("--host-rules", action="store_true", help="Run the configured two-player rules/start/final/rematch scenario.")
    parser.add_argument("--host-rules-start-only", action="store_true", help="Verify configured rules and start/shutdown only; does not prove a completed match or final wave.")
    parser.add_argument("--waves", type=int, choices=range(1, 201), default=None, help="Configured wave count (1–200 with Core2.3/2.4/2.5); implies --host-rules.")
    parser.add_argument("--start-gold", type=exact_start_gold, default=None, help="Exact initial gold per player; implies --host-rules.")


def rules_arguments(args, parser):
    args.host_rules = args.host_rules or args.host_rules_start_only or args.waves is not None or args.start_gold is not None
    if args.host_rules_start_only and (getattr(args,"complete_matches",False) or getattr(args,"rapid_buy",False)):
        parser.error("Startup-only cannot be combined with complete-match or rapid-buy acceptance.")
    if not args.host_rules:
        return []
    if args.protocol == "7":
        parser.error("Host-rule scenarios require Wire8/9/10; --protocol 7 is incompatible.")
    args.waves = 1 if args.waves is None else args.waves
    args.start_gold = "180" if args.start_gold is None else args.start_gold
    return ["--duel-host-rules", "--duel-waves", str(args.waves), "--duel-start-gold", args.start_gold] + (["--duel-host-rules-start-only"] if args.host_rules_start_only else [])



def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("player", help="Built player executable; on macOS: .app/Contents/MacOS/Defense Duell")
    parser.add_argument("--output", default="TestResults/lan-economy-v11")
    parser.add_argument("--cycles", type=int, default=None)
    parser.add_argument("--complete-matches", action="store_true", help="Play two full production wave matches with mutual rematch and shared speed consent.")
    parser.add_argument("--rapid-buy", action="store_true", help="Buy several identical troops within one frame, verify all ACKs and paid FIFO/gold state.")
    parser.add_argument("--seconds", type=float, default=18)
    parser.add_argument("--port", type=int, default=7777)
    parser.add_argument("--map", type=int, choices=range(6), default=0)
    parser.add_argument("--impaired", action="store_true")
    parser.add_argument("--delay-ms", type=float, default=75)
    parser.add_argument("--jitter-ms", type=float, default=15)
    parser.add_argument("--loss-percent", type=float, default=2)
    parser.add_argument("--speed-vote", action="store_true")
    add_version_arguments(parser)
    args = parser.parse_args()
    configured = rules_arguments(args, parser)
    if args.cycles is None:
        args.cycles = 1 if args.complete_matches or args.host_rules else 2
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any((output / name).exists() for name in ("host.json", "client.json", "comparison.json")):
        parser.error("Use a new evidence directory; existing reports are preserved.")
    binary = str(Path(args.player).resolve())
    processes = []
    proxy = None
    try:
        join_port = args.port
        if args.impaired:
            join_port = args.port + 1
            proxy = subprocess.Popen([sys.executable, str(Path(__file__).with_name("udp_proxy.py")),
                                      "--listen-port", str(join_port), "--server-port", str(args.port),
                                      "--delay-ms", str(args.delay_ms), "--jitter-ms", str(args.jitter_ms),
                                      "--loss-percent", str(args.loss_percent), "--report", str(output / "proxy.json")])
            time.sleep(.5)
        for role, port in (("host", args.port), ("client", join_port)):
            command = [binary, "-batchmode", "-nographics", "-logFile", str(output / (role + ".log")),
                       "--duel-" + role, "--duel-port", str(port), "--duel-cycles", str(args.cycles),
                       "--duel-seconds", str(args.seconds), "--duel-report", str(output / (role + ".json"))]
            command.extend(configured)
            if role == "host":
                command.extend(["--duel-map", str(args.map)])
            if args.complete_matches:
                command.append("--duel-complete-matches")
            if args.speed_vote:
                command.append("--duel-speed-vote")
            if args.rapid_buy:
                command.append("--duel-rapid-buy")
            processes.append(subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT))
            time.sleep(.5)
        per_cycle_timeout = 1860 if args.complete_matches or args.host_rules else 420
        deadline = time.monotonic() + args.cycles * per_cycle_timeout + 60
        while any(process.poll() is None for process in processes):
            if time.monotonic() > deadline:
                raise TimeoutError("Two-process integration timed out; inspect host/client logs.")
            time.sleep(.25)
        host = json.loads((output / "host.json").read_text())
        client = json.loads((output / "client.json").read_text())
        result = compare(host, client, require_complete=args.complete_matches, require_rapid=args.rapid_buy, require_economy11=True, protocol=args.protocol, require_host_rules=args.host_rules, expected_waves=args.waves, expected_start_gold=args.start_gold, expected_start_only=args.host_rules_start_only)
        result["exit_codes"] = [process.returncode for process in processes]
        result["impairment_requested"] = args.impaired
        if any(result["exit_codes"]):
            result["passed"] = False
            result["problems"].append("Non-zero player exit status.")
        (output / "comparison.json").write_text(json.dumps(result, indent=2) + "\n")
        print(json.dumps(result, indent=2))
        return 0 if result["passed"] else 1
    finally:
        for process in processes:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
        if proxy is not None:
            proxy.terminate()
            proxy.wait(timeout=5)


if __name__ == "__main__":
    raise SystemExit(main())
