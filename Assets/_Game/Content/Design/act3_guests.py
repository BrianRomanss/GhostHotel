# Act 3 cast for Ghost Hotel. Run: python Assets/_Game/Content/Design/act3_guests.py
# Act 3 rules: Vertical ("below a Victorian"), FloorCount ("max 2 on my floor"), Cursed rooms.
# Design rule learned in Act 2: every guest has at least two likes, so one awkward neighbour can
# never make a 3-star night impossible.
import json, os

HERE = os.path.dirname(os.path.abspath(__file__))
PATH = os.path.join(HERE, "..", "guests.json")


def R(kind, **kw):
    return dict(kind=kind, **kw)


NOISY = dict(name="Noisy", addTag="Noisy", directions="All", range=1)
GLOW = dict(name="Glow", removeTag="Dark", directions="All", range=1)
SCREECH = dict(name="Screech", addTag="Noisy", directions="Vertical", range=1)

ACT3 = [
    dict(id="rowena", name="Rowena", type="Weeper", era="1910s", stays=3,
         bio="Lost her husband to one of Vane's deals. Wants a quiet room under someone respectable.",
         needs=[R("RequiresTag", tag="Dark")], likes=[R("Vertical", relation="Below", ghostType="Victorian"), R("Floor", end="Bottom", count=1)],
         hiddenRule=R("FloorCount", count=2), hiddenHint="Crowded corridors make me feel I can't breathe...",
         arrival="Rowena[sad]: A dark room, low down. Or beneath a respectable gentleman, if you have one. They used to keep the noise off.\nI shan't trouble you with the rest.",
         story=["Rowena[sad]: My Henry signed a paper for a man in a top hat. Then Henry was gone, and so was the house.",
                "Rowena: Lord Fenwick says Vane collects the people with the deeds. Henry is in his warehouse somewhere. Waiting.",
                "Rowena[happy]: Edith found Henry. She sent him out the other side. He's waiting for me there, she says. I'm going."],
         checkout="Rowena[happy]: Henry's pocket watch. It stopped the day he signed. It's ticking again.", keepsake="Stopped Watch", keepsakePassive="CalmShield"),
    dict(id="barnaby", name="Mr. Barnaby", type="Victorian", era="1890s", stays=3,
         bio="Vane's former bookkeeper. Wants a mirror, a quiet floor, and no banshees.",
         needs=[R("RequiresTag", tag="Mirror")], likes=[R("FloorCount", count=2), R("Edge")], dislikes=[R("AvoidNeighbour", ghostType="Banshee")],
         arrival="Mr. Barnaby: Barnaby. Formerly of Vane and Associates. A mirror, if you please, and a floor that isn't crowded.\nNo banshees. I've heard enough screaming in my career.",
         story=["Mr. Barnaby[sad]: I kept Vane's books for forty years. Every soul he bought, I wrote down. In ink.",
                "Mr. Barnaby: I still have the ledger, keeper. Every name. If you could free them, I could cross them out.",
                "Mr. Barnaby[happy]: Seventeen names crossed out tonight. The ink is fading. So, pleasantly, am I."],
         checkout="Mr. Barnaby[happy]: Vane's old ledger. Burn it when the last name is gone.", keepsake="Vane's Ledger", keepsakePassive="RevealHidden"),
    dict(id="corvin", name="Sir Corvin", type="HeadlessKnight", era="1300s", stays=3,
         bio="The oldest knight in the cellar. Serves beneath nobles, literally.",
         needs=[R("RequiresTag", tag="Basement")], likes=[R("Vertical", relation="Below", ghostType="Victorian"), R("Company")],
         dislikes=[R("ForbidsTag", tag="Mirror")],
         arrival="Sir Corvin: (A rusted helmet, very old.) Corvin. I guard from below. Put a noble above me, and I'll keep them safe.\nNo mirrors. You know why.",
         story=["Sir Corvin: Greaves and Aldric were my squires, once. Good lads. They kept the cellar door while I slept.",
                "Sir Corvin[sad]: The door is open a crack now. I've seen what's behind it. Shelves. Thousands of shelves. Jars with lights in them.",
                "Sir Corvin[happy]: Every light that leaves a jar goes home. I've seen it. That's worth guarding for. Now I'll rest, and you'll guard."],
         checkout="Sir Corvin[happy]: My broken sword. The other half is in the in-between. Bring it back.", keepsake="Broken Sword"),
    dict(id="cobb", name="Cobb", type="Poltergeist", era="1930s", stays=3,
         bio="A chimney sweep who thinks the curse is cosy. Likes crowds, up to a point.",
         likes=[R("RequiresTag", tag="Cursed"), R("Company")], dislikes=[R("FloorCount", count=3)], aura=NOISY,
         arrival="Cobb[happy]: Cobb, sweep! Curse in the room? Lovely, feels like a warm chimney.\nPut me with folk. Not too many, mind, I get rattly.",
         story=["Cobb: I got stuck up the hotel chimney in '38. Nobody heard me knocking. Funny, cos I'm loud now.",
                "Cobb[sad]: The curse is warm because it's hungry, keeper. It's trying to swallow the rooms. I'll keep it busy.",
                "Cobb[happy]: Found the way out of the chimney. It goes up! Always did. Cheerio."],
         checkout="Cobb[happy]: My brush. Sweep the curse out, it's only soot really.", keepsake="Sweep's Brush", keepsakePassive="ExtraSwap"),
    dict(id="nell", name="Nell", type="Drowned", era="1900s", stays=3,
         bio="The lake ferry woman. Likes to sleep above another sailor, or right by the water.",
         needs=[R("RequiresTag", tag="Water")], likes=[R("Vertical", relation="Above", ghostType="Drowned"), R("Floor", end="Bottom", count=1)],
         dislikes=[R("AvoidNeighbour", ghostType="Poltergeist")],
         arrival="Nell: Nell, ferry woman. Water, please. Ground floor, or above another sailor, I'm not fussy.\nNo poltergeists. They rock the boat.",
         story=["Nell: I rowed the dead across the lake for sixty years. Then one night I rowed myself.",
                "Nell[sad]: Young Tam drowned on my last crossing. I couldn't reach him. I've been rowing in circles looking.",
                "Nell[happy]: Tam's found. He was waiting on the far shore, the silly boy. We'll cross together."],
         checkout="Nell[happy]: My oarlock. Keep someone afloat.", keepsake="Brass Oarlock"),
    dict(id="tam", name="Tam", type="Drowned", era="1900s", stays=2,
         bio="A boy who fell off Nell's ferry. Wants to be next to Nell.",
         needs=[R("RequiresTag", tag="Water")], likes=[R("WantNeighbour", guestId="nell"), R("Company")],
         hiddenRule=R("ForbidsTag", tag="Cold"), hiddenHint="It was so cold in the water... I don't ever want to be cold again.",
         arrival="Tam[sad]: Is Nell here? The ferry lady? I want to be near her. And near water. I like water. I'm not scared of it.\nI'm just... a bit cold.",
         story=["Tam[sad]: I fell off the ferry reaching for a fish. Nell jumped in. She couldn't find me.",
                "Tam[happy]: I told Nell it wasn't her fault. She cried. Ghosts can cry, did you know? Then we laughed. We're going now."],
         checkout="Tam[happy]: A river stone. It's warm if you hold it long enough.", keepsake="Warm River Stone"),
    dict(id="bea", name="Bea", type="ChildGhost", era="1940s", stays=3,
         bio="Sleeps with a nightlight. Ideally, a wisp in the room above.",
         needs=[R("ForbidsTag", tag="Dark")], likes=[R("Vertical", relation="Below", ghostType="Wisp"), R("Company")],
         dislikes=[R("AvoidNeighbour", ghostType="Banshee")],
         arrival="Bea: Can I have a wisp upstairs? Like a nightlight? Or just someone next door.\nNo dark rooms. No screaming ladies.",
         story=["Bea[sad]: Mum sent me to the country in the war. The hotel was the country. I didn't like the dark.",
                "Bea: Ember sits upstairs and glows through the floorboards for me. I can see the cracks light up. It's nice.",
                "Bea[happy]: I'm not scared of the dark now. I just like the light better. That's allowed. Bye!"],
         checkout="Bea[happy]: My torch. It only works for brave people.", keepsake="Tin Torch", keepsakePassive="ExtraHint"),
    dict(id="aine", name="Aine", type="Banshee", era="1700s", stays=3,
         bio="The eldest banshee. Enjoys noise. Likes corners.",
         likes=[R("RequiresTag", tag="Noisy"), R("Edge")], dislikes=[R("FloorCount", count=3)], aura=SCREECH,
         arrival="Aine: I am Aine. I keened before your family had a name. Noise does not trouble me. I like it.\nA corner, if there is one. Not a crowded floor.",
         story=["Aine: Moira and Marisol keened each other free. I taught them, long ago. I am proud.",
                "Aine[sad]: Vane bottled my sister's voice in 1801. It is on a shelf behind the cellar door. I can hear her.",
                "Aine[happy]: Brigid uncorked the bottle. My sister sang her way out. Now I will follow her voice."],
         checkout="Aine[happy]: An empty bottle. Never let him fill it again.", keepsake="Empty Bottle"),
    dict(id="brigid", name="Brigid", type="Banshee", era="1800s", stays=3,
         bio="Aine's youngest sister. Hates the curse. Wants a corner, or solitude.",
         needs=[R("ForbidsTag", tag="Cursed")], likes=[R("Edge"), R("Isolation")], aura=SCREECH,
         hiddenRule=R("AvoidNeighbour", ghostType="ChildGhost"), hiddenHint="Little ones cry when they hear me. I can't bear it.",
         arrival="Brigid: Not a cursed room. Anything but. A corner, or alone.\nAnd... please, be careful who you put beside me.",
         story=["Brigid[sad]: I was the one Vane bottled. Aine says I've been out for a century. It doesn't feel like it.",
                "Brigid: I'm going back in. Behind the door. There are other bottles. I know which corks are loose.",
                "Brigid[happy]: Forty-one bottles. Forty-one voices. They're all singing on the way out. Listen!"],
         checkout="Brigid[happy]: A cork. The last one. Keep it, so nobody can use it.", keepsake="The Last Cork", keepsakePassive="EctoplasmBonus"),
    dict(id="una", name="Una", type="Banshee", era="?", stays=2,
         bio="A banshee who speaks only in riddles.",
         likes=[R("RequiresTag", tag="Noisy"), R("Vertical", relation="Above", ghostType="Banshee")], dislikes=[R("ForbidsTag", tag="Water")],
         aura=SCREECH,
         riddleClues=["\"Silence is my enemy. I sleep best where the house is loud.\"",
                      "\"I keen above my sisters, never below. Or let the house be loud enough.\"",
                      "\"The lake took my voice once. No water in my room.\""],
         arrival="Una: (She only hums.)",
         story=["Una: (She hums a tune. Bartholomew says it's one Edith used to hum.)",
                "Una[happy]: (She hums the whole tune for the first time. Then she's gone, and the tune stays.)"],
         checkout="Una[happy]: (She leaves a tuning fork on the desk. It hums the same note.)", keepsake="Tuning Fork"),
    dict(id="ember", name="Ember", type="Wisp", era="unknown", stays=3,
         bio="A warm, steady wisp. Likes to watch over a knight, or a corner.",
         likes=[R("Vertical", relation="Above", ghostType="HeadlessKnight"), R("Edge")], dislikes=[R("ForbidsTag", tag="Basement")], aura=GLOW,
         arrival="Ember: Ember. I glow, steady. Put me over a knight and I'll light their watch. Or a corner. I'm easy.\nNot the cellar. Something down there eats light.",
         story=["Ember: Lumen told me about the thing that eats light. It's real. It's Vane's warehouse, breathing.",
                "Ember[sad]: I lit the knights' way to the door. Sir Corvin went through. He didn't come back yet.",
                "Ember[happy]: Corvin came back. With jars. With lights in them. They're all going home. I'll light the way."],
         checkout="Ember[happy]: A spark that never goes out. Keep it for the last night.", keepsake="Eternal Spark"),
    dict(id="flicker", name="Flicker", type="Wisp", era="unknown", stays=2,
         bio="A nervous wisp. Upper floors only. Alone or with a friend, can't decide.",
         needs=[R("Floor", end="Top", count=2)], likes=[R("Isolation"), R("Company")], aura=GLOW,
         riddleClues=["\"Put me low and I'll go out. I live in the top two floors.\"",
                      "\"I want to be alone. Or I want a friend. Either! Honestly, either.\"",
                      "\"I make the dark rooms beside me less dark. Mind who sleeps there.\""],
         arrival="Flicker: Hi! Top floors please! I'd like to be alone. Or not! Either! Sorry!",
         story=["Flicker[sad]: I keep changing my mind because if I choose wrong, I might go out. That's what happened last time.",
                "Flicker[happy]: I chose! I chose to go. It's fine. It's lovely, actually. Bye, bye, bye!"],
         checkout="Flicker[happy]: A match! Unstruck! For emergencies!", keepsake="Unstruck Match"),
    dict(id="squire", name="The Hollow Squire", type="HeadlessKnight", era="1300s", stays=2,
         bio="Sir Corvin's squire. Wants the lower floors, near his master or any company.",
         needs=[R("Floor", end="Bottom", count=2)], likes=[R("WantNeighbour", guestId="corvin"), R("Company")],
         hiddenRule=R("ForbidsTag", tag="Cursed"), hiddenHint="My master says never sleep where he has been.",
         arrival="The Hollow Squire: (Just an empty suit of armour. It bows.) I serve Sir Corvin. Low floors. Near him, or near anyone.",
         story=["The Hollow Squire: My body is in the warehouse. A jar on the third shelf. I can feel the cork.",
                "The Hollow Squire[happy]: The cork is out. I am whole for one moment, and then I am free. Thank you, keeper."],
         checkout="The Hollow Squire[happy]: My spurs. They jingle when someone's coming home.", keepsake="Jingling Spurs"),
    dict(id="sprocket", name="Sprocket", type="Poltergeist", era="1950s", stays=2,
         bio="A clockmaker poltergeist. Lives in attics. Likes ticking company below or beside.",
         needs=[R("RequiresTag", tag="Attic")], likes=[R("Vertical", relation="Above", ghostType="HeadlessKnight"), R("Company"), R("Edge")],
         dislikes=[R("ForbidsTag", tag="Water")], aura=NOISY,
         arrival="Sprocket[happy]: Sprocket! Clocks! The attic's where the clockwork is. Put a knight below me, they tick nicely.\nOr anyone. No water, it rusts the gears.",
         story=["Sprocket: Every clock in the hotel stops at 12:07. That's when Edith went through the door. I've been trying to restart them.",
                "Sprocket[happy]: They're ticking! All of them! That means she's still in there. Still alive, in the way that counts."],
         checkout="Sprocket[happy]: A spring. Wind it on the last night.", keepsake="Clock Spring"),
]

with open(PATH, encoding="utf-8") as f:
    data = json.load(f)
by_id = {g["id"]: g for g in data["guests"]}
order = [g["id"] for g in data["guests"]]
for g in ACT3:
    if g["id"] not in by_id:
        order.append(g["id"])
    by_id[g["id"]] = g
data["guests"] = [by_id[i] for i in order]
with open(PATH, "w", encoding="utf-8", newline="\n") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
print(f"{len(ACT3)} Act 3 guests merged; {len(data['guests'])} guests total")
