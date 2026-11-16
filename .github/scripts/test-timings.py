#!/usr/bin/env python3
"""Summarise .trx test results: per-assembly counts and the slowest tests.

Usage: test-timings.py <results-dir> [title] [top-n]

Writes Markdown to $GITHUB_STEP_SUMMARY when set, otherwise to stdout.
"""

import glob
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def parse_duration(text):
    """Convert a trx duration (hh:mm:ss.fffffff) to seconds."""
    if not text:
        return 0.0
    hours, minutes, seconds = text.split(":")
    return int(hours) * 3600 + int(minutes) * 60 + float(seconds)


def load(results_dir):
    """Return (per-assembly outcome counts, list of (seconds, assembly, test name))."""
    counts = defaultdict(lambda: defaultdict(int))
    tests = []
    for path in sorted(glob.glob(os.path.join(results_dir, "**", "*.trx"), recursive=True)):
        root = ET.parse(path).getroot()
        storage = {}
        for definition in root.iterfind("t:TestDefinitions/t:UnitTest", NS):
            method = definition.find("t:TestMethod", NS)
            class_name = method.get("className") if method is not None else ""
            # codeBase keeps the original casing ('storage' is lower-cased); either separator may be used
            path_to_assembly = (method.get("codeBase") if method is not None else None) or definition.get("storage") or ""
            assembly = re.split(r"[\\/]", path_to_assembly)[-1]
            storage[definition.get("id")] = (assembly, class_name)
        for result in root.iterfind("t:Results/t:UnitTestResult", NS):
            assembly, class_name = storage.get(result.get("testId"), ("unknown", ""))
            counts[assembly][result.get("outcome", "Unknown")] += 1
            name = result.get("testName", "")
            short_class = class_name.rsplit(".", 1)[-1]
            label = f"{short_class}.{name}" if short_class else name
            tests.append((parse_duration(result.get("duration")), assembly, label))
    return counts, tests


def render(title, counts, tests, top):
    lines = [f"## {title}", ""]
    if not tests:
        lines.append("No .trx results found.")
        return "\n".join(lines) + "\n"

    outcomes = sorted({o for c in counts.values() for o in c})
    lines.append("| Assembly | Total | " + " | ".join(outcomes) + " | Time (s) |")
    lines.append("|---|---:|" + "---:|" * len(outcomes) + "---:|")
    seconds_by_assembly = defaultdict(float)
    for seconds, assembly, _ in tests:
        seconds_by_assembly[assembly] += seconds
    for assembly in sorted(counts):
        row = counts[assembly]
        cells = " | ".join(str(row.get(o, 0)) for o in outcomes)
        lines.append(
            f"| {assembly} | {sum(row.values())} | {cells} | {seconds_by_assembly[assembly]:.0f} |"
        )

    if any("NotExecuted" in row for row in counts.values()):
        lines += ["", "NotExecuted covers both skipped and inconclusive tests; `dotnet test` only counts the skipped ones."]

    lines += ["", f"### Slowest {top} tests", "", "| # | Seconds | Assembly | Test |", "|---:|---:|---|---|"]
    for rank, (seconds, assembly, label) in enumerate(sorted(tests, reverse=True)[:top], 1):
        label = label.replace("|", "\\|")
        lines.append(f"| {rank} | {seconds:.1f} | {assembly} | {label} |")
    return "\n".join(lines) + "\n"


def main():
    results_dir = sys.argv[1] if len(sys.argv) > 1 else "TestResults"
    title = sys.argv[2] if len(sys.argv) > 2 else "Test results"
    top = int(sys.argv[3]) if len(sys.argv) > 3 else 30
    counts, tests = load(results_dir)
    markdown = render(title, counts, tests, top)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(markdown)
    else:
        print(markdown)


if __name__ == "__main__":
    main()
