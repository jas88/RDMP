#!/usr/bin/env python3
"""Summarise NUnit results and list Inconclusive tests.

Inconclusive tests are not counted in the 'dotnet test' summary line (and are
reported as 'NotExecuted' in TRX, like ignored tests), so a database that
silently fails to connect makes whole test matrices vanish without failing the
build. This reads the NUnit result XML written by the adapter's TestOutputXml
setting, lists Inconclusive tests and, with --fail, fails if there are any.

Usage: inconclusive-report.py [--fail] RESULTS_DIR
"""
import collections
import pathlib
import sys
import xml.etree.ElementTree as ET


def main(argv):
    fail = "--fail" in argv
    dirs = [a for a in argv if a != "--fail"]
    files = sorted(f for d in dirs for f in pathlib.Path(d).glob("*.xml"))
    if not files:
        print(f"::error::No NUnit result files found in {', '.join(dirs)}")
        return 1

    outcomes = collections.Counter()
    inconclusive = []
    for path in files:
        for case in ET.parse(path).getroot().iter("test-case"):
            result = case.get("result", "Unknown")
            label = case.get("label")
            outcomes[f"{result}:{label}" if label else result] += 1
            if result == "Inconclusive":
                message = (case.findtext("reason/message") or "").strip().splitlines()
                inconclusive.append((case.get("fullname"), message[0] if message else "(no message)"))

    print("Test outcomes: " + ", ".join(f"{k}={v}" for k, v in sorted(outcomes.items())))
    print(f"Inconclusive: {len(inconclusive)}")
    for name, message in sorted(inconclusive):
        print(f"  {name}: {message}")

    if fail and inconclusive:
        print(f"::error::{len(inconclusive)} tests were Inconclusive; "
              "check database setup and TestDatabases.txt")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
