#!/usr/bin/env python3
"""A bounded, seeded UDP impairment proxy for actual NGO/UTP process tests.

Every forwarded datagram receives one configured one-way delay. Both directions
are independently dropped by a seeded PRNG. This is real packet impairment; it
does not claim to simulate mobile radio, NAT, Relay or device performance.
"""
import argparse
import asyncio
import json
from pathlib import Path
import random
import signal
import time


class Proxy(asyncio.DatagramProtocol):
    def __init__(self, args):
        self.args = args
        self.server = ("127.0.0.1", args.server_port)
        self.client = None
        self.transport = None
        self.random = random.Random(args.seed)
        self.pending = set()
        self.counts = {"client_to_host": {"received": 0, "dropped": 0, "forwarded": 0, "bytes": 0},
                       "host_to_client": {"received": 0, "dropped": 0, "forwarded": 0, "bytes": 0}}
        self.started = time.time()

    def connection_made(self, transport):
        self.transport = transport

    def datagram_received(self, data, address):
        if address == self.server:
            direction, destination = "host_to_client", self.client
        else:
            self.client = address
            direction, destination = "client_to_host", self.server
        stats = self.counts[direction]
        stats["received"] += 1
        stats["bytes"] += len(data)
        if destination is None or len(self.pending) >= 10000 or self.random.random() < self.args.loss_percent / 100:
            stats["dropped"] += 1
            return
        delay = max(0, self.args.delay_ms + self.random.uniform(-self.args.jitter_ms, self.args.jitter_ms)) / 1000
        handle = None

        def forward():
            self.pending.discard(handle)
            if self.transport is not None and not self.transport.is_closing():
                self.transport.sendto(data, destination)
                stats["forwarded"] += 1

        handle = asyncio.get_running_loop().call_later(delay, forward)
        self.pending.add(handle)

    def report(self):
        result = {"method": "actual UDP datagram forwarding with deterministic PRNG",
                  "listen_port": self.args.listen_port, "server_port": self.args.server_port,
                  "delay_ms_each_direction": self.args.delay_ms,
                  "additional_rtt_ms_nominal": 2 * self.args.delay_ms,
                  "jitter_ms_each_direction": self.args.jitter_ms,
                  "drop_percent_each_direction": self.args.loss_percent, "seed": self.args.seed,
                  "duration_seconds": round(time.time() - self.started, 3), "traffic": self.counts,
                  "not_tested": ["Relay", "Android", "NAT", "thermal load"]}
        Path(self.args.report).parent.mkdir(parents=True, exist_ok=True)
        Path(self.args.report).write_text(json.dumps(result, indent=2) + "\n")


async def main(args):
    stopped = asyncio.Event()
    loop = asyncio.get_running_loop()
    for sig in (signal.SIGINT, signal.SIGTERM):
        loop.add_signal_handler(sig, stopped.set)
    transport, proxy = await loop.create_datagram_endpoint(lambda: Proxy(args), local_addr=("127.0.0.1", args.listen_port))
    print(f"UDP proxy ready 127.0.0.1:{args.listen_port} -> :{args.server_port}", flush=True)
    try:
        await stopped.wait()
    finally:
        proxy.report()
        for handle in proxy.pending:
            handle.cancel()
        transport.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--listen-port", type=int, default=7778)
    parser.add_argument("--server-port", type=int, default=7777)
    parser.add_argument("--delay-ms", type=float, default=75)
    parser.add_argument("--jitter-ms", type=float, default=15)
    parser.add_argument("--loss-percent", type=float, default=2)
    parser.add_argument("--seed", type=int, default=20261110)
    parser.add_argument("--report", default="TestResults/lan-proxy.json")
    args = parser.parse_args()
    if not 0 <= args.loss_percent <= 100 or not 0 <= args.delay_ms <= 5000 or not 0 <= args.jitter_ms <= 5000:
        parser.error("Impairment parameters are out of bounds.")
    asyncio.run(main(args))
