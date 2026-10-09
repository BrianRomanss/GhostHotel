# Act 2 night designs (nights 11-25). Run: python Assets/_Game/Content/Design/act2_design.py
# Writes act2.json for Tools/NightDesigner, which finds validated room layouts for each night.
import json, os, collections

HERE = os.path.dirname(os.path.abspath(__file__))
POOL = ["Dark", "Water", "Mirror", "Warm", "Music", "Garden"]


def T(target, text):
    return dict(target=target, text=text)


def E(kind, **kw):
    return dict(kind=kind, **kw)


def N(number, title, size, guests, intro, events=(), tokens=0, perfect=(1, 10), tags=(3, 6), **extra):
    floors, cols = size
    d = dict(number=number, title=title, floors=floors, roomsPerFloor=cols, guests=list(guests), intro=intro,
             midnight=list(events), swapTokens=tokens, tagPool=POOL, minTags=tags[0], maxTags=tags[1],
             minPerfect=perfect[0], maxPerfect=perfect[1])
    d.update(extra)
    return d


NIGHTS = [
    N(11, "Help Wanted", (3, 4), ["wren", "ivy", "hester", "bartholomew"],
      "Bartholomew[happy]: A third floor! And a new sort of guest: the kind that changes the rooms around it.\n"
      "Bartholomew: Wren glows. Any room next to Wren stops being Dark. Mind who you put beside it.\n"
      "Bartholomew: Oh, and we can afford staff now. Have a look in the Lobby tomorrow.",
      perfect=(1, 40),
      tutorial=[T("card", "Wren has an AURA. Rooms next to Wren lose their Dark tag. Temporary tags get a violet ring.")]),
    N(12, "The Clock Strikes", (3, 4), ["wren", "ivy", "moira", "salt"],
      "Bartholomew[sad]: It's that time of year. At midnight, the hotel... does things. Pipes burst. Lights fail.\n"
      "Bartholomew: You'll get a Swap Token to fix whatever goes wrong. Spend it wisely.",
      events=[E("BurstPipe")], tokens=1),
    N(13, "Two Little Lights", (3, 4), ["wren", "lumen", "ivy", "mags"],
      "Bartholomew: Ivy only wants to be next to Wren. Some guests want a particular neighbour, not just any neighbour.",
      tutorial=[T("card", "New rule: Next to someone. Ivy wants Wren in a room beside hers.")]),
    N(14, "A Gentleman Calls", (3, 4), ["hester", "fenwick", "greaves", "bartholomew"],
      "Bartholomew[sad]: There's a man at the door. Polished shoes. He doesn't knock, he just... waits.",
      vaneOffer="Mr. Vane[happy]: Good evening. Ellis, isn't it? Edith's grandchild. My condolences on the inheritance.\n"
                "Mr. Vane: I collect old buildings. And their contents. I'd pay handsomely for one small thing: your guest list.\n"
                "Mr. Vane: A little Ectoplasm now, for a name or two. Nobody need know.\n"
                "> Take his money | Show him the door",
      tutorial=[T("card", "Some guests have a HIDDEN rule. You only see a hint. Break it, and it costs a star at dawn.")]),
    N(15, "Banshee in the Belfry", (3, 4), ["moira", "marisol", "hester", "mags"],
      "Bartholomew: Two banshees tonight. Their screech goes straight up and down through the floors, never sideways.",
      events=[E("RestlessNight")], tokens=1),
    N(16, "Power Cut", (3, 4), ["greaves", "fenwick", "salt", "lumen", "dolly"],
      "Bartholomew[sad]: Mr. Vane walked the corridors this afternoon. Measuring. Smiling.",
      events=[E("PowerFlicker"), E("VanesVisit")], tokens=2, perfect=(1, 6)),
    N(17, "Riddle of the Stranger", (3, 4), ["stranger", "thorne", "mags", "bartholomew", "pip"],
      "Bartholomew: A veiled guest. Won't give a name, won't say what they need. Only riddles.",
      riddleGuest="stranger",
      tutorial=[T("card", "A RIDDLE night. Read the Stranger's clues and work out their rules. Their stars stay hidden until dawn.")]),
    N(18, "The Fourth Floor", (4, 4), ["thorne", "greaves", "hester", "moira", "ashworth"],
      "Bartholomew[happy]: The fourth floor's open! Mind your step, the banister's original. 1887. Mostly woodworm.",
      perfect=(1, 5)),
    N(19, "Seance", (4, 4), ["marisol", "dolly", "ned", "mags", "bartholomew"],
      "Bartholomew[sad]: Someone's lit candles in the cellar. A séance. They're trying to reach Edith.",
      events=[E("SeanceDownstairs")], tokens=1),
    N(20, "Rush Night II", (4, 4), ["wren", "fenwick", "salt", "lumen", "bartholomew", "tobias", "bramble"],
      "Bartholomew[happy]: The lake fog's back and everyone wants a room! Lots of guests, nothing too tricky.",
      perfect=(4, 400), tags=(4, 7)),
    N(21, "The Second Offer", (4, 4), ["moira", "marisol", "gloria", "cornelia"],
      "Bartholomew[sad]: He's back.",
      vaneOffer="Mr. Vane: Ellis. You turned me down, or you didn't. Either way, I'm a patient man.\n"
                "Mr. Vane[happy]: Old Salt. The banshees. The knight in the cellar. They'd fetch a fine price in the right collection.\n"
                "Mr. Vane: Just sign here. A deposit on the deed. Nothing binding.\n"
                "> Sign | Tear it up",
      events=[E("BurstPipe")], tokens=2),
    N(22, "Unexpected Guest", (4, 4), ["bramble", "dolly", "finn", "elsie"],
      "Bartholomew: Quiet night. I'll just pop out for a minute, see if anyone's at the gate...",
      events=[E("UnexpectedGuest", guestId="bartholomew", description="Bartholomew locked himself out. Find him an empty room, or leave him in the snow.")],
      tokens=1, perfect=(1, 6)),
    N(23, "The Stranger Returns", (4, 4), ["stranger", "aldric", "morrow", "ned"],
      "Bartholomew: The veiled one's back. Same riddles. Do you know, I think I know that voice.",
      riddleGuest="stranger", perfect=(1, 6)),
    N(24, "The Long Night", (4, 4), ["marisol", "ashworth", "finn", "bartholomew"],
      "Bartholomew[sad]: Everything's creaking tonight. Brace yourself, keeper.",
      events=[E("BurstPipe"), E("PowerFlicker")], tokens=2, perfect=(1, 8)),
    N(25, "Grand Night: The Door Below", (4, 4), ["fenwick", "greaves", "thorne", "bartholomew", "aldric", "rattles"],
      "Bartholomew: Every room, every guest. And Greaves says the door under the cellar has started to knock back.",
      events=[E("VanesVisit"), E("SeanceDownstairs")], tokens=2, perfect=(1, 4),
      ledgerPage="Ellis, love. If you've found this one, you've found the door.\n"
                 "Vane isn't a developer. He's a collector, and the in-between is his warehouse.\n"
                 "Every ghost who moves on is one he can't sell. That's why he wants the hotel.\n"
                 "I went through to stop him. I'm still here. Keep going.\n"
                 "— E."),
]

# Check the story schedule: each guest should appear exactly as many nights as their stays.
guests = {g["id"]: g for g in json.load(open(os.path.join(HERE, "..", "guests.json"), encoding="utf-8"))["guests"]}
count = collections.Counter(g for n in NIGHTS for g in n["guests"])
ACT1 = {"morrow", "ashworth", "pip", "gloria", "rattles", "tobias", "elsie", "finn", "cornelia", "aldric", "ned"}
problems = [f"{gid}: {count[gid]} nights, {g['stays']} stays" for gid, g in guests.items()
            if g["stays"] < 10 and gid not in ACT1 and count[gid] and count[gid] != g["stays"]]
for gid in count:
    if gid not in guests:
        problems.append(f"{gid}: not in guests.json")
print("schedule:", "OK" if not problems else "\n  " + "\n  ".join(problems))

json.dump(dict(nights=NIGHTS), open(os.path.join(HERE, "act2.json"), "w", encoding="utf-8", newline="\n"),
          ensure_ascii=False, indent=2)
print(f"{len(NIGHTS)} night designs written")
