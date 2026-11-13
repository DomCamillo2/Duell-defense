#!/usr/bin/env python3
"""Run actual anonymous-auth + Unity Relay DTLS in two desktop player processes.

Requires an explicitly configured Unity Cloud project with Relay available.
Creates real Relay allocations; never enables billing, invents cloud success,
or claims the two local processes are two independent internet connections.
"""
import argparse
import json
from pathlib import Path
import subprocess
import time
from run_lan import compare, add_version_arguments, rules_arguments


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("player", help="Built player executable")
    parser.add_argument("--output", default="TestResults/relay-economy-v11")
    parser.add_argument("--cycles", type=int, default=1)
    parser.add_argument("--complete-matches", action="store_true", help="Play two full production wave matches with mutual rematch and shared speed consent.")
    parser.add_argument("--rapid-buy", action="store_true", help="Verify immediate repeated troop purchases, ACKs and authoritative paid FIFO state.")
    parser.add_argument("--seconds", type=float, default=18)
    parser.add_argument("--map", type=int, choices=range(6), default=1)
    parser.add_argument("--speed-vote", action="store_true")
    add_version_arguments(parser)
    args = parser.parse_args()
    configured = rules_arguments(args, parser)
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any((output / name).exists() for name in ("host.json", "client.json", "comparison.json")):
        parser.error("Use a new evidence directory; existing reports are preserved.")
    code_file = output / "relay-code.json"
    if code_file.exists():
        code_file.unlink()
    binary = str(Path(args.player).resolve())
    processes = []
    try:
        for role in ("host", "client"):
            command = [binary, "-batchmode", "-nographics", "-logFile", str(output / (role + ".log")),
                       "--duel-relay-" + role, "--duel-cycles", str(args.cycles), "--duel-seconds", str(args.seconds),
                       "--duel-code-file", str(code_file), "--duel-report", str(output / (role + ".json"))]
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
                raise TimeoutError("Actual Relay test timed out; inspect reports/logs for the cloud blocker.")
            # A failed host cannot produce a valid join code; avoid waiting for a client timeout.
            if processes[0].poll() not in (None, 0) and processes[1].poll() is None:
                processes[1].terminate()
                processes[1].wait(timeout=5)
            time.sleep(.25)
        reports = []
        for role in ("host", "client"):
            path = output / (role + ".json")
            reports.append(json.loads(path.read_text()) if path.exists() else {"passed": False, "cycles": []})
        result = compare(*reports, require_complete=args.complete_matches, require_rapid=args.rapid_buy, require_economy11=True, protocol=args.protocol, require_host_rules=args.host_rules, expected_waves=args.waves, expected_start_gold=args.start_gold, expected_start_only=args.host_rules_start_only)
        result["exit_codes"] = [process.returncode for process in processes]
        result["scope"] = "Actual Unity Relay DTLS through two desktop processes on this machine; two separate internet access paths and Android devices NOT tested."
        if any(result["exit_codes"]):
            result["passed"] = False
            result["problems"].append("At least one real Relay player failed or was stopped after its peer failed.")
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


if __name__ == "__main__":
    raise SystemExit(main())
