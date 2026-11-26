"""Turns the test run into a job summary and two shields.io badge files.

Usage: python3 test_report.py <results dir> <badges dir>
Reads every *.trx (test counts) and coverage.cobertura.xml (line coverage) under <results dir>,
prints a Markdown summary to stdout (CI appends it to $GITHUB_STEP_SUMMARY) and writes
<badges dir>/tests.json and <badges dir>/coverage.json in the shields.io "endpoint" format.
Standard library only, so CI needs no extra packages or third-party actions.
"""
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET
from datetime import datetime

TRX = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def parse_time(s):
    """TRX times have 7 fraction digits (e.g. 2026-11-26T14:01:02.1234567+02:00); Python takes at most 6."""
    m = re.match(r"(.*T\d\d:\d\d:\d\d)(?:\.(\d+))?(Z|[+-]\d\d:\d\d)?$", s)
    if not m:
        raise ValueError(s)
    base, frac, tz = m.groups()
    return datetime.fromisoformat(base + "." + (frac or "0")[:6].ljust(6, "0") + ("+00:00" if tz in (None, "Z") else tz))


sys.stdout.reconfigure(encoding="utf-8")  # the summary has emoji; Windows consoles default to cp1252
results_dir, badges_dir =(pathlib.Path(p) for p in sys.argv[1:3])
badges_dir.mkdir(parents=True, exist_ok=True)

# ---- tests (TRX) ----
total = passed = failed = skipped = 0
seconds = 0.0
failures = []
for trx in sorted(results_dir.rglob("*.trx")):
    root = ET.parse(trx).getroot()
    c = root.find(f"{TRX}ResultSummary/{TRX}Counters")
    total += int(c.get("total", 0))
    passed += int(c.get("passed", 0))
    failed += int(c.get("failed", 0)) + int(c.get("error", 0)) + int(c.get("timeout", 0)) + int(c.get("aborted", 0))
    skipped += int(c.get("notExecuted", 0))
    times = root.find(f"{TRX}Times")
    if times is not None and times.get("start") and times.get("finish"):
        try:
            start, finish = (parse_time(times.get(k)) for k in ("start", "finish"))
            seconds += (finish - start).total_seconds()
        except ValueError:
            pass
    for r in root.iter(f"{TRX}UnitTestResult"):
        if r.get("outcome") not in ("Passed", "NotExecuted"):
            msg = r.find(f"{TRX}Output/{TRX}ErrorInfo/{TRX}Message")
            failures.append((r.get("testName"), (msg.text or "").strip().splitlines()[0] if msg is not None and msg.text else ""))

if failed:
    tests_badge = {"message": f"{failed} failed, {passed} passed", "color": "red"}
elif total:
    tests_badge = {"message": f"{passed} passed" + (f", {skipped} skipped" if skipped else ""), "color": "brightgreen"}
else:
    tests_badge = {"message": "no tests", "color": "lightgrey"}

# ---- coverage (Cobertura from coverlet) ----
# vstest also copies the file next to the TRX, so lines are merged by (file, line) rather than summed per file.
hits = {}  # assembly -> {(file, line): hit?}
for xml in sorted(results_dir.rglob("coverage.cobertura.xml")):
    for p in ET.parse(xml).getroot().iter("package"):
        lines = hits.setdefault(p.get("name"), {})
        for cls in p.iter("class"):
            for line in cls.iter("line"):
                key = (cls.get("filename"), line.get("number"))
                lines[key] = lines.get(key, False) or int(line.get("hits", "0")) > 0
packages = {name: (sum(lines.values()), len(lines)) for name, lines in hits.items()}

lines_covered = sum(c for c, _ in packages.values())
lines_valid = sum(v for _, v in packages.values())
coverage = 100 * lines_covered / lines_valid if lines_valid else None
if coverage is None:
    coverage_badge = {"message": "unknown", "color": "lightgrey"}
else:
    color = "brightgreen" if coverage >= 80 else "green" if coverage >= 70 else "yellow" if coverage >= 60 else "orange" if coverage >= 50 else "red"
    coverage_badge = {"message": f"{coverage:.0f}%", "color": color}

for name, label, badge in (("tests", "tests", tests_badge), ("coverage", "coverage", coverage_badge)):
    (badges_dir / f"{name}.json").write_text(json.dumps({"schemaVersion": 1, "label": label, **badge}) + "\n", encoding="utf-8")

# ---- job summary ----
icon = "❌" if failed else "✅"
out = [
    f"## {icon} Tests",
    "",
    "| Total | Passed | Failed | Skipped | Duration |",
    "|---:|---:|---:|---:|---:|",
    f"| {total} | {passed} | {failed} | {skipped} | {seconds:.1f}s |",
    "",
]
if failures:
    out += ["### Failed tests", ""] + [f"- `{n}`" + (f": {m}" if m else "") for n, m in failures] + [""]
if coverage is not None:
    out += [f"## Line coverage: {coverage:.1f}%", "", "| Assembly | Lines | Coverage |", "|---|---:|---:|"]
    out += [f"| {n} | {c}/{v} | {100 * c / v:.1f}% |" for n, (c, v) in sorted(packages.items()) if v]
print("\n".join(out))
