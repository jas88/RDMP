#!/usr/bin/env python3
"""Summarise test outcomes from TRX files and list Inconclusive tests.

Inconclusive tests are not counted in the 'dotnet test' summary line, so a
database that silently fails to connect makes whole test matrices vanish
without failing the build. This script makes them visible and, with --fail,
fails the build if any are present.

Usage: inconclusive-report.py [--fail] file.trx [file.trx ...]
"""
import collections
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def main(argv):
    fail = "--fail" in argv
    files = [a for a in argv if a != "--fail"]
    if not files:
        print("::error::No TRX files found")
        return 1

    outcomes = collections.Counter()
    inconclusive = []
    for path in files:
        root = ET.parse(path).getroot()
        for result in root.iterfind(".//t:UnitTestResult", NS):
            outcome = result.get("outcome", "Unknown")
            outcomes[outcome] += 1
            if outcome == "Inconclusive":
                message = result.findtext(".//t:Message", default="", namespaces=NS)
                inconclusive.append((result.get("testName"), message.strip().splitlines()[:1]))

    print("Test outcomes: " + ", ".join(f"{k}={v}" for k, v in sorted(outcomes.items())))
    print(f"Inconclusive: {len(inconclusive)}")
    for name, message in sorted(inconclusive):
        print(f"  {name}: {message[0] if message else '(no message)'}")

    if fail and inconclusive:
        print(f"::error::{len(inconclusive)} tests were Inconclusive; "
              "check database setup and TestDatabases.txt")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
