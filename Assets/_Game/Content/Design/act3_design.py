# Act 3 night designs (nights 26-40). Run: python Assets/_Game/Content/Design/act3_design.py
# Writes act3.json for Tools/NightDesigner.
import json, os, collections

HERE = os.path.dirname(os.path.abspath(__file__))
POOL = ["Dark", "Water", "Mirror", "Warm", "Music", "Garden", "Cursed"]


def T(target, text):
    return dict(target=target, text=text)


def E(kind, **kw):
    return dict(kind=kind, **kw)


def N(number, title, size, guests, intro, events=(), tokens=0, perfect=(1, 10), tags=(4, 8), **extra):
    floors, cols = size
    d = dict(number=number, title=title, floors=floors, roomsPerFloor=cols, guests=list(guests), intro=intro,
             midnight=list(events), swapTokens=tokens, tagPool=POOL, minTags=tags[0], maxTags=tags[1],
             minPerfect=perfect[0], maxPerfect=perfect[1])
    d.update(extra)
    return d


NIGHTS = [
    N(26, "Checkout", (4, 5), ["rowena", "barnaby", "corvin", "ashworth", "bartholomew"],
      "Bartholomew[sad]: Five floors' worth of corridors now, and half of them smell of Vane's cologne.\n"
      "Bartholomew: Some of tonight's guests care who sleeps directly above or below them. Above means the floor up, same column.",
      tutorial=[T("card", "New rule: Below someone. Rowena wants a Victorian in the room directly above hers.")]),
    N(27, "Elbow Room", (4, 5), ["barnaby", "cobb", "nell", "tam", "morrow"],
      "Bartholomew: Mr. Barnaby can't abide a crowded floor. Count the guests on each floor before you place him.",
      tutorial=[T("card", "New rule: Max guests on my floor. Count everyone on that floor, including them.")]),
    N(28, "The Deed", (4, 5), ["rowena", "corvin", "aine", "bea", "ember"],
      "Bartholomew[sad]: Vane's left papers on the desk. A deed, with Edith's signature forged at the bottom.",
      vaneOffer="Mr. Vane: Ellis. Let's not pretend. Your grandmother is in my warehouse, and you are running out of time.\n"
                "Mr. Vane[happy]: Sign the deed and I'll let her go. She'll walk right out. You have my word as a gentleman.\n"
                "Mr. Vane: Or keep your little hotel, and keep her where she is.\n"
                "> Sign the deed | Throw it in the fire",
      events=[E("VanesVisit")], tokens=1),
    N(29, "Cursed Rooms", (4, 5), ["cobb", "brigid", "squire", "nell", "bartholomew", "ivy"],
      "Bartholomew[sad]: Some rooms have gone cold and wrong overnight. Cursed. Most guests lose a star in one.\n"
      "Bartholomew: Cobb says they're cosy. Cobb is an unusual ghost.",
      perfect=(1, 2000), tags=(6, 10),
      tutorial=[T("card", "CURSED rooms cost most guests a star. A few guests actually like them.")]),
    N(30, "Riddle of the Humming Lady", (4, 5), ["una", "aine", "sprocket", "aldric", "ashworth"],
      "Bartholomew: A banshee who only hums. Aine says she's family. Listen to the clues.",
      riddleGuest="una"),
    N(31, "Rush Night III", (4, 5), ["tam", "flicker", "greaves", "pip", "bartholomew", "ned"],
      "Bartholomew[happy]: Old friends back to help, and new faces besides. Busy, busy, busy.",
      perfect=(3, 5000), tags=(5, 9)),
    N(32, "Midnight Mass", (4, 5), ["rowena", "corvin", "cobb", "morrow", "wren"],
      "Bartholomew[sad]: Someone's holding a séance in the cellar again. And the pipes are groaning.",
      events=[E("SeanceDownstairs"), E("BurstPipe")], tokens=2, perfect=(1, 10)),
    N(33, "The Fifth Floor", (5, 5), ["bea", "ember", "fenwick", "dolly", "tobias", "nell"],
      "Bartholomew: The fifth floor. Edith never opened it. She said the view was too good, it'd attract the wrong sort.",
      perfect=(1, 60)),
    N(34, "The Final Offer", (5, 5), ["brigid", "flicker", "gloria", "marisol", "moira"],
      "Bartholomew[sad]: He's not smiling tonight.",
      vaneOffer="Mr. Vane: Last chance, Ellis. After tonight I stop asking.\n"
                "Mr. Vane: I'll take the deed, the guests, the lot. You walk away rich, and Edith walks away free.\n"
                "Mr. Vane[happy]: Everyone gets what they want. Except the ghosts, and who asks them?\n"
                "> Take the deal | No. Never.",
      riddleGuest="flicker", events=[E("VanesVisit")], tokens=1, perfect=(1, 8)),
    N(35, "The Open Door", (5, 5), ["bea", "ember", "sprocket", "hester", "greaves"],
      "Bartholomew: The cellar door's standing open. Light keeps going out on whichever floor is nearest it.",
      events=[E("PowerFlicker")], tokens=1, perfect=(1, 8)),
    N(36, "Riddle of the Last Song", (5, 5), ["una", "aine", "brigid", "mags", "thorne"],
      "Bartholomew: Una's back. She's humming the whole tune now. Edith's tune.",
      riddleGuest="una", perfect=(1, 8)),
    N(37, "The Collector's Visit", (5, 5), ["barnaby", "squire", "lumen", "salt", "bartholomew"],
      "Bartholomew[sad]: He's walking the halls with a pen, marking doors. Anywhere he marks goes cold.",
      events=[E("VanesVisit"), E("VanesVisit", title="Mr. Vane, Again")], tokens=2, perfect=(1, 10)),
    N(38, "Homecoming", (5, 5), ["ivy", "tobias", "finn", "elsie", "cornelia", "gloria", "bartholomew"],
      "Bartholomew[happy]: Keeper, look at the door. They've all come back. Every guest you ever helped, back to help you.",
      perfect=(3, 400), tags=(6, 10)),
    N(39, "The Long Way Down", (5, 5), ["morrow", "ashworth", "pip", "rattles", "ned", "bartholomew"],
      "Bartholomew: The last ordinary night, I think. After this, the door opens properly.",
      events=[E("UnexpectedGuest", guestId="stranger", description="A veiled figure at the gate. She says she's been waiting a long time.")],
      tokens=1, perfect=(1, 12)),
    N(40, "Grand Night: Checkout", (5, 5), ["greaves", "aldric", "moira", "marisol", "wren", "lumen", "hester", "bartholomew"],
      "Bartholomew: Every room. Every friend we've made. Vane's in the lobby with his pen. Show him what this hotel is for.",
      events=[E("VanesVisit"), E("SeanceDownstairs")], tokens=2, perfect=(1, 12), tags=(6, 10),
      ledgerPage="Ellis. Last page, I promise.\n"
                 "The warehouse door only opens from our side on a Grand Night, when every room is full and every guest is calm.\n"
                 "Fill the hotel. Fill it with everyone. I'll be standing right behind the door.\n"
                 "And love? Don't sign anything.\n"
                 "— Gran."),
]

guests = {g["id"]: g for g in json.load(open(os.path.join(HERE, "..", "guests.json"), encoding="utf-8"))["guests"]}
ACT3 = {"rowena", "barnaby", "corvin", "cobb", "nell", "tam", "bea", "aine", "brigid", "una", "ember", "flicker", "squire", "sprocket"}
count = collections.Counter(g for n in NIGHTS for g in n["guests"])
problems = [f"{gid}: {count[gid]} nights, {guests[gid]['stays']} stays" for gid in ACT3 if count[gid] != guests[gid]["stays"]]
problems += [f"{gid}: not in guests.json" for gid in count if gid not in guests]
print("schedule:", "OK" if not problems else "\n  " + "\n  ".join(problems))

json.dump(dict(nights=NIGHTS), open(os.path.join(HERE, "act3.json"), "w", encoding="utf-8", newline="\n"),
          ensure_ascii=False, indent=2)
print(f"{len(NIGHTS)} night designs written")
