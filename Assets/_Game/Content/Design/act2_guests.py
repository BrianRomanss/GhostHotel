# Act 2 cast for Ghost Hotel. Run: python Assets/_Game/Content/Design/act2_guests.py
# Merges these guests into guests.json (replacing any with the same id). Kept as a script so the
# story text stays readable and reviewable in one place.
import json, os

HERE = os.path.dirname(os.path.abspath(__file__))
PATH = os.path.join(HERE, "..", "guests.json")


def R(kind, **kw):
    return dict(kind=kind, **kw)


NOISY = dict(name="Noisy", addTag="Noisy", directions="All", range=1)
GLOW = dict(name="Glow", removeTag="Dark", directions="All", range=1)
SCREECH = dict(name="Screech", addTag="Noisy", directions="Vertical", range=1)

ACT2 = [
    dict(id="wren", name="Wren", type="Wisp", era="unknown", stays=4,
         bio="A tiny shy light. Brightens any room next to it, whether you like it or not.",
         likes=[R("Isolation"), R("Floor", end="Top", count=1)], dislikes=[R("ForbidsTag", tag="Basement")], aura=GLOW,
         arrival="Wren[sad]: ...hello. I'm small. I'll be no trouble. Somewhere alone, or high up, near the sky.\nI just glow a little. The dark rooms next to me stop being dark. Sorry.",
         story=["Wren: I don't remember being anything but a light. Is that strange?",
                "Wren[sad]: A little girl followed me through the marsh once. I was trying to lead her home. She thought I was leading her away.",
                "Wren: Ivy says I'm the nicest light she's ever met. I didn't know lights could be nice.",
                "Wren[happy]: I remember now. I was a lantern in a window, waiting for someone to come home. They came home. I can go out now."],
         checkout="Wren[happy]: Keep my wick. It still smells of warm wax.", keepsake="Lantern Wick", keepsakePassive="ExtraHint"),
    dict(id="ivy", name="Ivy", type="ChildGhost", era="1880s", stays=3,
         bio="Lost in the marsh as a girl. Follows lights. Wants to be next to Wren.",
         needs=[R("ForbidsTag", tag="Dark")], likes=[R("WantNeighbour", guestId="wren"), R("Floor", end="Top", count=1)], dislikes=[R("AvoidNeighbour", ghostType="Banshee")],
         arrival="Ivy[happy]: Is the little light here? The one called Wren? I want the room right next to it!\nNo dark rooms please. And no screamy ladies.",
         story=["Ivy[sad]: I followed a light into the marsh and never came out. I wasn't scared, though. It was pretty.",
                "Ivy: Wren says it was trying to lead me home. I believe it. Lights don't lie.",
                "Ivy[happy]: Wren told me where home was. It's not far. I'm going to walk there now, slowly, and look at everything."],
         checkout="Ivy[happy]: Here's my ribbon. Tie it on a lantern so people find their way.", keepsake="Marsh Ribbon"),
    dict(id="hester", name="Hester", type="Weeper", era="1860s", stays=4,
         bio="A governess who covered every mirror in the house. Won't say why.",
         needs=[R("RequiresTag", tag="Dark")], likes=[R("Floor", end="Top", count=1)],
         hiddenRule=R("ForbidsTag", tag="Mirror"), hiddenHint="I can't bear to see my own face...",
         arrival="Hester[sad]: A dark room, high up. Thank you.\nAnd... no. Never mind. I'm sure it will be fine.",
         story=["Hester[sad]: I was a governess here, when it was a house. The children were dears. The mirrors were not.",
                "Hester: Something lived in the mirrors. It wore my face and smiled when I didn't. I covered every one.",
                "Hester[sad]: Mr. Vane asked me about the mirrors yesterday. Very politely. Too politely.",
                "Hester[happy]: The thing in the mirror was only ever me, afraid. I looked at it tonight. It looked back, and it was tired too. We're both going to rest."],
         checkout="Hester[happy]: My hand mirror. You can uncover it now.", keepsake="Hand Mirror", keepsakePassive="RevealHidden"),
    dict(id="moira", name="Moira", type="Banshee", era="1820s", stays=4,
         bio="Her screech goes straight through floors and ceilings.",
         likes=[R("Edge")], dislikes=[R("AvoidNeighbour", ghostType="ChildGhost")], aura=SCREECH,
         arrival="Moira: I keen, keeper. It's what I am. It goes up and down through the floors, never sideways.\nA corner room. And keep the little ones away. I frighten them.",
         story=["Moira[sad]: I keened for every death in the village for two hundred years. Nobody ever keened for me.",
                "Moira: The singer, Marisol, says my voice has a lovely range. Nobody has ever said lovely about my voice.",
                "Moira[sad]: Vane wants to bottle my voice. He says it would sell. I said no. He smiled anyway.",
                "Moira[happy]: Marisol keened for me tonight. Badly. Beautifully. I can stop now."],
         checkout="Moira[happy]: My comb. Silver, for a banshee's hair. Brush gently.", keepsake="Silver Comb", keepsakePassive="CalmShield"),
    dict(id="mags", name="Mags", type="Poltergeist", era="1960s", stays=4,
         bio="Throws teacups when she's nervous, which is always. Hates mirrors and misery.",
         needs=[R("ForbidsTag", tag="Mirror")], likes=[R("Company"), R("RequiresTag", tag="Warm")], dislikes=[R("AvoidNeighbour", ghostType="Weeper")], aura=NOISY,
         arrival="Mags[happy]: Mags! Sorry about the noise, it just happens. No mirrors, please, I throw things at them.\nNeighbours are lovely! Not the weepy kind though. I can't stand it when people cry, I start throwing teacups.",
         story=["Mags: I ran the tea room downstairs in the sixties. Best scones in the county. Then the fire.",
                "Mags[sad]: I keep throwing teacups because my hands remember the fire. They want to throw water on it.",
                "Mags: Old Salt taught me to breathe like the tide. In for six, out for six. Only broke two cups today.",
                "Mags[happy]: I made scones tonight. Real ones, in the old oven. Nobody threw anything. I'm done."],
         checkout="Mags[happy]: The last teacup! Unbroken. A miracle.", keepsake="Unbroken Teacup", keepsakePassive="ExtraSwap"),
    dict(id="fenwick", name="Lord Fenwick", type="Victorian", era="1890s", stays=4,
         bio="A ruined lord who still dresses for dinner. Wants solitude, and a mirror for his cravat.",
         needs=[R("RequiresTag", tag="Mirror")], likes=[R("Isolation"), R("Floor", end="Top", count=1)],
         hiddenRule=R("ForbidsTag", tag="Water"), hiddenHint="Damp ruins a good waistcoat. Not that one would complain.",
         arrival="Lord Fenwick: Fenwick. Lord. A mirror for my cravat, and no neighbours, if it can be managed.\nThe rest is of no consequence. One does not complain.",
         story=["Lord Fenwick: I gambled the family estate away in a single night. To a polite man in a top hat.",
                "Lord Fenwick[sad]: The man was Vane. He has been collecting deeds for a very long time, it seems.",
                "Lord Fenwick: I have written to my great-granddaughter. She runs a bakery. She is happy. The estate would only have been a burden.",
                "Lord Fenwick[happy]: I have nothing left to lose, which turns out to be a remarkable thing to have. Goodnight, keeper."],
         checkout="Lord Fenwick[happy]: My signet ring. Never wager it.", keepsake="Signet Ring", keepsakePassive="EctoplasmBonus"),
    dict(id="salt", name="Old Salt", type="Drowned", era="1870s", stays=3,
         bio="A lighthouse keeper. Wants water, the ground floor, and no singing.",
         needs=[R("RequiresTag", tag="Water")], likes=[R("Floor", end="Bottom", count=1)],
         hiddenRule=R("AvoidNeighbour", ghostType="Banshee"), hiddenHint="Sirens. Never sleep near a singer.",
         arrival="Old Salt: Kept the Hollow Point light forty years. Water, ground floor, nothing fancy.\nAnd... ah, never mind. An old sailor's superstition.",
         story=["Old Salt: The night the light went out, a ship hit the rocks. I was asleep. I've been awake ever since.",
                "Old Salt[sad]: Captain Morrow's Lark. It was the Lark, keeper. I've been afraid to ask if he forgave me.",
                "Old Salt[happy]: Morrow left a note in his logbook for me. 'The fog was too thick for any light.' Forty years, and it wasn't my fault."],
         checkout="Old Salt[happy]: The lighthouse key. Leave a light on for someone.", keepsake="Lighthouse Key"),
    dict(id="lumen", name="Lumen", type="Wisp", era="unknown", stays=3,
         bio="A brighter, braver wisp. Likes corners and the sky. Dislikes rattling.",
         likes=[R("Edge")], dislikes=[R("AvoidNeighbour", ghostType="Poltergeist")], aura=GLOW,
         hiddenRule=R("ForbidsTag", tag="Basement"), hiddenHint="It's so far from the sky down there...",
         arrival="Lumen[happy]: Another light! Hello! A corner room, so I can see out two windows.\nNo rattling neighbours. And, um... I'd rather be nearer the sky.",
         story=["Lumen: Wren is my sister, I think. We came from the same window.",
                "Lumen[sad]: The cellar frightens me. Something down there eats light. I felt it reach for me.",
                "Lumen[happy]: I lit the cellar stairs for Sir Greaves tonight and nothing ate me. I'm braver than I was. That's enough."],
         checkout="Lumen[happy]: A spark for your pocket. It won't burn.", keepsake="Pocket Spark"),
    dict(id="greaves", name="Sir Greaves", type="HeadlessKnight", era="1500s", stays=4,
         bio="Guards the cellar door. Wishes to serve a proper noble, upstairs or adjacent.",
         needs=[R("RequiresTag", tag="Basement")], likes=[R("WantNeighbour", ghostType="Victorian"), R("Edge")], dislikes=[R("ForbidsTag", tag="Mirror")],
         arrival="Sir Greaves: (The helmet clanks.) Greaves. I guard doors. The cellar door, by preference.\nA noble neighbour would be fitting. And no mirrors.",
         story=["Sir Greaves: Sir Aldric was my master. He never told me he had moved on. I have been guarding his door all this time.",
                "Sir Greaves[sad]: There is a door under the cellar, keeper. It was not there before. I hear Edith's voice behind it.",
                "Sir Greaves: Vane offered me a new master. I said a knight chooses his own. He did not like that.",
                "Sir Greaves[happy]: I have chosen. I serve this hotel, and its keeper, and I will hold that door until you are ready. Then I rest."],
         checkout="Sir Greaves[happy]: My gauntlet. Throw it down when you need a champion.", keepsake="Iron Gauntlet", keepsakePassive="CalmShield"),
    dict(id="marisol", name="Marisol", type="Banshee", era="1940s", stays=4,
         bio="A wartime singer. Needs music, and an audience.",
         needs=[R("RequiresTag", tag="Music")], likes=[R("Company"), R("Edge")], aura=SCREECH,
         arrival="Marisol[happy]: Darling! Marisol, the Nightingale of the Blitz. A room with a gramophone, or I simply won't sing.\nAnd neighbours, please. A singer needs an audience. Even if they cover their ears.",
         story=["Marisol: I sang in the shelters while the bombs fell. Kept everyone calm. Nobody kept me calm.",
                "Marisol[sad]: My voice went strange after. High. Terrible. People said I'd become a banshee. They weren't wrong.",
                "Marisol[happy]: Moira and I are rehearsing. Two banshees in harmony. Bartholomew wept. Possibly from joy.",
                "Marisol[happy]: We sang for Moira tonight. She's free. And so, I think, am I. Encore? No. Curtain."],
         checkout="Marisol[happy]: My gramophone needle. Play something sad and lovely.", keepsake="Gramophone Needle", keepsakePassive="ExtraHint"),
    dict(id="thorne", name="Mr. Thorne", type="Victorian", era="1900s", stays=3,
         bio="The old head gardener. Needs the greenhouse and peace, and loathes poltergeists.",
         needs=[R("RequiresTag", tag="Garden")], likes=[R("Isolation"), R("Edge")], dislikes=[R("AvoidNeighbour", ghostType="Poltergeist")],
         arrival="Mr. Thorne: Thorne. Head gardener, once. A room by the greenhouse.\nQuiet. No poltergeists. They knock over the pots.",
         story=["Mr. Thorne: Edith and I planted every rose on the north wall. She talked to them. I pretended not to.",
                "Mr. Thorne[sad]: The roses are dying from the roots up. Something is poisoning the soil under the hotel.",
                "Mr. Thorne[happy]: I grafted a cutting from Edith's rose. It'll outlive the rot. Plant it somewhere you'll see it."],
         checkout="Mr. Thorne[happy]: A rose cutting. Water it on Sundays.", keepsake="Rose Cutting", keepsakePassive="EctoplasmBonus"),
    dict(id="dolly", name="Dolly", type="ChildGhost", era="1920s", stays=3,
         bio="Bartholomew's little sister. Wants to be next to him. Frightened of knights.",
         needs=[R("ForbidsTag", tag="Dark")], likes=[R("WantNeighbour", guestId="bartholomew"), R("Company")], dislikes=[R("AvoidNeighbour", ghostType="HeadlessKnight")],
         arrival="Dolly[happy]: Barty! It's me, Dolly! Can I have the room next to my big brother?\nNot the dark ones. And the clanky men with no heads are scary.",
         story=["Dolly: Barty worked here as a bellboy so we'd have money. I used to wait for him by the back door.",
                "Dolly[sad]: There was a fever that winter. I didn't wake up. Barty stayed here instead of going home. Because home was too sad.",
                "Dolly[happy]: Barty says he'll stay and help the keeper, but I should go on ahead and save him a seat. I will! The best seat."],
         checkout="Dolly[happy]: My spinning top. Give it to Barty when he's ready to come.", keepsake="Spinning Top"),
    dict(id="bramble", name="Old Bramble", type="HeadlessKnight", era="1600s", stays=2,
         bio="Wants the ground floor, the far end, and no fuss.",
         needs=[R("Floor", end="Bottom", count=1)], likes=[R("Edge")], dislikes=[R("Isolation")],
         arrival="Old Bramble: Ground floor. End room. No fuss. No neighbours. That's all.",
         story=["Old Bramble: Built this place, I did. The foundations. Dug deeper than they told me to. Should've stopped.",
                "Old Bramble[happy]: Told Greaves where the old door is. Bricked it up myself, 1648. Whatever's behind it, it's been waiting a long time. Not my problem now."],
         checkout="Old Bramble: My trowel. You'll be needing it.", keepsake="Mason's Trowel"),
    dict(id="stranger", name="The Stranger", type="Weeper", era="?", stays=2,
         bio="A veiled guest who gives only riddles.",
         needs=[R("ForbidsTag", tag="Water")], likes=[R("Isolation")], dislikes=[R("AvoidNeighbour", ghostType="Poltergeist")],
         riddleClues=["\"I drowned once. Never again will I sleep where water lies.\"",
                      "\"I came here to be alone. A neighbour on any side would spoil it.\"",
                      "\"Rattling chains remind me of the ship. Keep them far from me.\""],
         arrival="The Stranger: (A veiled figure. Its voice is familiar, but you can't place it.)\nI won't tell you what I need. Listen to what I say instead.",
         story=["The Stranger: You placed me well. Edith would have, too. Did you know she kept every guest's needs in her head?",
                "The Stranger[happy]: (The veil slips. It's a younger Edith, or the memory of her.) Not yet, love. Keep going. You're nearly ready to find me."],
         checkout="The Stranger[happy]: My veil. Next time, you'll see my face.", keepsake="Black Veil"),
]

with open(PATH, encoding="utf-8") as f:
    data = json.load(f)
by_id = {g["id"]: g for g in data["guests"]}
order = [g["id"] for g in data["guests"]]
for g in ACT2:
    if g["id"] not in by_id:
        order.append(g["id"])
    by_id[g["id"]] = g
data["guests"] = [by_id[i] for i in order]
with open(PATH, "w", encoding="utf-8", newline="\n") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
print(f"{len(ACT2)} Act 2 guests merged; {len(data['guests'])} guests total")
