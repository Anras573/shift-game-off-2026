"""Discrete Gravity Shift solver for level design.

Usage:  python3 tools/level_solver.py            # every level in LevelCatalog.cs
        python3 tools/level_solver.py "Express Post"

It is a grid approximation of the real physics (see docs/LEVEL_DESIGN.md): use it to find
the minimum shift count and unintended shortcuts, then prove the level with a real-physics
play-through in tests/GravityShift.Tests/LevelSolutionTests.cs.


State = (row, col, gravity) while resting. Moves:
  - Q/E/W shift (cost 1): fall in the new direction until blocked; X = death, G = win.
    Mid-air: after falling >= MIDAIR_MIN cells, may shift again (cost 1) at any cell.
  - walk one cell along the floor (cost 0); stepping off a ledge falls.
Prints min shifts, whether mid-air is required, and a solution.
"""
import heapq, sys

DIRS = {"D": (1, 0), "L": (0, -1), "U": (-1, 0), "R": (0, 1)}
ORDER = ["D", "L", "U", "R"]
MIDAIR_MIN = 4  # cells travelled during the 0.3 s cooldown, from rest

def shift(g, key):
    i = ORDER.index(g)
    return ORDER[{"Q": (i + 1) % 4, "E": (i + 3) % 4, "W": (i + 2) % 4}[key]]

def screen_right(g):
    dr, dc = DIRS[g]          # gravity in grid terms (row down)
    # world g=(dc,-dr); screen-right world = (-gy, gx) = (dr, dc) -> grid (row=-dc, col=dr)
    return (-dc, dr)

class Level:
    def __init__(self, rows):
        self.rows = rows; self.h = len(rows); self.w = len(rows[0])
        assert all(len(r) == self.w for r in rows), "ragged"
        for r, line in enumerate(rows):
            for c, ch in enumerate(line):
                if ch == "P": self.start = (r, c)
    def at(self, r, c):
        if not (0 <= r < self.h and 0 <= c < self.w): return "#"
        ch = self.rows[r][c]
        return "." if ch == "P" else ch

def fall(lv, r, c, g, allow_mid):
    """Yields ('rest'|'win'|'dead', r, c, travelled) plus ('mid', r, c) points."""
    dr, dc = DIRS[g]; n = 0
    while True:
        nr, nc = r + dr, c + dc
        t = lv.at(nr, nc)
        if t == "#": 
            yield ("rest", r, c, n); return
        r, c, n = nr, nc, n + 1
        if t == "X": yield ("dead", r, c, n); return
        if t == "G": yield ("win", r, c, n); return
        if allow_mid and n >= MIDAIR_MIN: yield ("mid", r, c, n)

def goal_adjacent(lv, r, c, g):
    dr, dc = DIRS[g]
    return lv.at(r + dr, c + dc) == "G"

def solve(lv, allow_mid=True):
    """Dijkstra over resting states; cost = number of shifts. Wins are queued, not returned early."""
    WIN = ("WIN",)
    pq = []
    for kind, r, c, _ in fall(lv, lv.start[0], lv.start[1], "D", False):
        if kind == "rest": heapq.heappush(pq, (0, 0, (r, c, "D"), []))
        elif kind == "win": return 0, ["WIN"]
    seen = set(); tie = 1
    def push(cost, state, path):
        nonlocal tie
        heapq.heappush(pq, (cost, tie, state, path)); tie += 1
    while pq:
        cost, _, state, path = heapq.heappop(pq)
        if state == WIN: return cost, path
        if state in seen: continue
        seen.add(state)
        r, c, g = state
        mid_starts = []
        if goal_adjacent(lv, r, c, g): return cost, path + ["WIN"]
        sr = screen_right(g)
        for key, (wr, wc) in (("walkR", sr), ("walkL", (-sr[0], -sr[1]))):
            nr, nc = r + wr, c + wc
            t = lv.at(nr, nc)
            if t in "#X": continue
            if t == "G": push(cost, WIN, path + [f"{key}->WIN"]); continue
            dr, dc = DIRS[g]
            if lv.at(nr + dr, nc + dc) == "#":
                push(cost, (nr, nc, g), path + [key]); continue
            # walked off a ledge: fall, with mid-air shifts allowed on the way down
            for kind, fr, fc, _ in fall(lv, nr, nc, g, allow_mid):
                if kind == "rest": push(cost, (fr, fc, g), path + [f"{key}(off ledge)"])
                elif kind == "win": push(cost, WIN, path + [f"{key}->fall->WIN"])
                elif kind == "dead": pass
                elif kind == "mid": mid_starts.append((fr, fc, g, cost, path + [f"{key}(off ledge, falling)"]))
        # shifts, with mid-air chains (each extra shift costs 1)
        stack = [(r, c, g, cost, path)] + mid_starts; mid_seen = set()
        while stack:
            r0, c0, g0, cst, pth = stack.pop()
            for key in "QEW":
                g2 = shift(g0, key)
                if g2 == g0: continue
                for kind, fr, fc, _ in fall(lv, r0, c0, g2, allow_mid):
                    p2 = pth + [f"{key}->{g2}@({r0},{c0})"]
                    if kind == "rest": push(cst + 1, (fr, fc, g2), p2)
                    elif kind == "win": push(cst + 1, WIN, p2 + ["WIN"])
                    elif kind == "mid" and (fr, fc, g2) not in mid_seen and cst + 1 < 6:
                        mid_seen.add((fr, fc, g2)); stack.append((fr, fc, g2, cst + 1, p2 + ["(mid-air)"]))
    return None, None

def report(name, rows):
    lv = Level(rows)
    best, path = solve(lv, True)
    ground, gpath = solve(lv, False)
    print(f"{name}: min shifts={best} | without mid-air={ground}")
    print("    any:   ", " ".join(path or []))
    print("    ground:", " ".join(gpath or []))
    return best, ground

def load_catalog(path):
    import re
    src = open(path, encoding="utf-8").read()
    blocks = re.findall(
        r'LevelParser\.Parse\(\s*"([^"]+)",\s*\[(.*?)\]\s*,?\s*(?:par:\s*(\d+)\s*)?\)', src, re.S
    )
    return [(name, re.findall(r'"([^"]*)"', body), int(par) if par else 0) for name, body, par in blocks]


if __name__ == "__main__":
    import os
    here = os.path.dirname(os.path.abspath(__file__))
    catalog = os.path.join(here, "..", "src", "GravityShift.Game", "Levels", "LevelCatalog.cs")
    wanted = set(sys.argv[1:])
    problems = 0
    for name, rows, par in load_catalog(catalog):
        if not wanted or name in wanted:
            best, _ = report(name, rows)
            if best != par:
                problems += 1
                print(f"    !! par is {par} in LevelCatalog.cs, solver minimum is {best}")
    sys.exit(1 if problems else 0)
