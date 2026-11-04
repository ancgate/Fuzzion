"""Compare two BenchmarkDotNet JSON reports and fail on slowdowns.

Usage:
    python tools/benchmarks/compare.py BASE_DIR HEAD_DIR [--threshold 0.20]

Each directory is a BenchmarkDotNet artifacts folder (or any folder) containing
*-report*.json files, produced with `--exporters json`. Benchmarks are matched by
their full name including parameters; benchmarks that exist on only one side are listed
but never fail the comparison. Exits with 1 when any benchmark's mean time grew by more
than the threshold (20% by default), 0 otherwise.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def load(folder: Path) -> dict[str, float]:
    means: dict[str, float] = {}
    for report in folder.rglob("*-report*.json"):
        data = json.loads(report.read_text(encoding="utf-8"))
        for benchmark in data.get("Benchmarks", []):
            stats = benchmark.get("Statistics") or {}
            if "Mean" in stats:
                # DisplayInfo names the class, method and job; Parameters holds e.g. "Length=64".
                key = f'{benchmark.get("DisplayInfo", "")} {benchmark.get("Parameters", "")}'.strip()
                means[key] = float(stats["Mean"])
    return means


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("base", type=Path)
    parser.add_argument("head", type=Path)
    parser.add_argument("--threshold", type=float, default=0.20, help="allowed slowdown, as a fraction (default 0.20)")
    args = parser.parse_args()

    base, head = load(args.base), load(args.head)
    if not head:
        print(f"No benchmark results found in {args.head}", file=sys.stderr)
        return 1

    failures = 0
    print(f"{'benchmark':<90} {'base (ns)':>14} {'head (ns)':>14} {'change':>8}")
    for name in sorted(head):
        if name not in base:
            print(f"{name:<90} {'-':>14} {head[name]:>14.1f} {'new':>8}")
            continue
        change = head[name] / base[name] - 1
        flag = ""
        if change > args.threshold:
            flag = "  SLOWER"
            failures += 1
        print(f"{name:<90} {base[name]:>14.1f} {head[name]:>14.1f} {change:>+7.1%}{flag}")
    for name in sorted(set(base) - set(head)):
        print(f"{name:<90} {base[name]:>14.1f} {'-':>14} {'removed':>8}")

    if failures:
        print(f"\n{failures} benchmark(s) more than {args.threshold:.0%} slower than the base.", file=sys.stderr)
        return 1
    print(f"\nNo benchmark is more than {args.threshold:.0%} slower than the base.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
