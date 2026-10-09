using System;
using System.Collections.Generic;
using System.IO;
using ChuchuGames.ProcArt;
using static ChuchuGames.ProcArt.Shapes;

namespace GhostHotel.EditorTools.ArtGen
{
    /// <summary>
    /// Placeholder art in the GDD §7 palette, pushed towards eerie (decayed hotel, hollow-eyed ghosts),
    /// generated from code so it is consistent and can be
    /// regenerated at will. Final art replaces these files 1:1 by keeping the same names.
    /// Engine-free: runs from the Unity menu and from Tools/ArtGen (plain .NET).
    /// </summary>
    public static class GhostHotelArt
    {
        // GDD §7.2 master palette
        static readonly Rgba NightNavy = Rgba.Hex("#1B1B3A"), DeepPurple = Rgba.Hex("#2E2654"), DuskViolet = Rgba.Hex("#4B3F7A"),
            LampAmber = Rgba.Hex("#F2A541"), WarmGlow = Rgba.Hex("#FFD58A"), PaperCream = Rgba.Hex("#F6EEDC"),
            Brass = Rgba.Hex("#B08A3E"), Ink = Rgba.Hex("#2A2333"), Valid = Rgba.Hex("#5CB85C"), Broken = Rgba.Hex("#D9534F"),
            Aura = Rgba.Hex("#9370DB"), Wood = Rgba.Hex("#5C3D2E");

        // GDD §7.3 ghost tints, by GhostType name.
        public static readonly Dictionary<string, string> Tints = new Dictionary<string, string>
        {
            ["Weeper"] = "#A9CFF2", ["Poltergeist"] = "#9FE3B0", ["Victorian"] = "#C9B6F2", ["Drowned"] = "#7FD6CC",
            ["ChildGhost"] = "#FFE29A", ["Banshee"] = "#F2B8C6", ["Wisp"] = "#FFF3B0", ["HeadlessKnight"] = "#B9C2CF",
            ["Bartholomew"] = "#EAD9B0", // the bellhop: a named character with his own art key
        };

        public static readonly string[] Expressions = { "neutral", "happy", "sad" };

        public static readonly string[] TagIds =
            { "Dark", "Water", "Warm", "Attic", "Basement", "Mirror", "Music", "Garden", "Cursed", "Noisy", "Cold" };

        /// <summary>Writes every placeholder into <paramref name="root"/> (…/Art/Generated). Returns the files written.</summary>
        public static List<string> GenerateAll(string root, Action<string> log = null)
        {
            var files = new List<string>();
            void Save(string rel, Raster r)
            {
                var path = System.IO.Path.Combine(root, rel);
                Png.Write(path, r);
                files.Add(path);
                log?.Invoke(rel);
            }

            foreach (var kv in Tints)
            foreach (var expr in Expressions)
            {
                Save($"Ghosts/ghost_{kv.Key}_{expr}.png", Ghost(kv.Key, expr));
                Save($"Portraits/portrait_{kv.Key}_{expr}.png", Portrait(kv.Key, expr));
            }
            foreach (var tag in TagIds) Save($"Tags/tag_{tag}.png", TagIcon(tag));
            foreach (var expr in Expressions)
            {
                Save($"Portraits/portrait_Vane_{expr}.png", VanePortrait(expr));
                Save($"Portraits/portrait_Edith_{expr}.png", EdithPortrait(expr));
            }

            Save("UI/star.png", StarIcon(true));
            Save("UI/star_empty.png", StarIcon(false));
            Save("UI/moon.png", MoonIcon(true));
            Save("UI/moon_empty.png", MoonIcon(false));
            Save("UI/panel_9s.png", RoundedSquare(64, 16));
            Save("UI/button_9s.png", RoundedSquare(64, 22));
            Save("UI/circle.png", CircleSprite(128));
            Save("UI/glow.png", GlowSprite(128));
            Save("UI/hand.png", Hand());
            Save("UI/lock.png", LockIcon());
            Save("UI/waxseal.png", WaxSeal());

            Save("Rooms/room_stage.png", RoomStage());
            Save("Backgrounds/exterior.png", Exterior());
            Save("Backgrounds/lobby.png", Lobby());
            Save("Backgrounds/desk.png", Desk());
            Save("Backgrounds/dawn.png", Dawn());
            Save("UI/fog.png", FogStrip());
            Save("UI/vignette.png", Vignette());
            return files;
        }

        // ------------------------------------------------------------------ ghosts

        static readonly Rgba Socket = Rgba.Hex("#0E0A16"), EyeGlow = Rgba.Hex("#E6FFF6"), ColdGlow = Rgba.Hex("#B8E8FF"),
            Bone = Rgba.Hex("#E9E4D4"), Tarnish = Rgba.Hex("#7A6436");

        /// <summary>Deterministic per-name seed (string.GetHashCode is randomised per process in .NET).</summary>
        static int Seed(string s)
        {
            int h = 17;
            foreach (var c in s) h = unchecked(h * 31 + c);
            return h & 0xFFFF;
        }

        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

        /// <summary>Type tint washed towards a cold grey: every ghost looks drained, but types stay distinct.</summary>
        static Rgba Spectral(string type) => Rgba.Lerp(Rgba.Hex(Tints[type]), Rgba.Hex("#C9D3DC"), 0.3f);

        /// <summary>
        /// 256×256 in-room ghost. Same dome silhouette for everyone (GDD §7.3) made eerie: translucent
        /// body fading into a tattered tail, hollow sockets with pinprick lights, a cold glow.
        /// </summary>
        public static Raster Ghost(string type, string expr)
        {
            var r = new Raster(256, 256);
            var tint = Spectral(type);
            int seed = Seed(type);
            bool wisp = type == "Wisp", knight = type == "HeadlessKnight";
            var body = wisp ? WispBody(seed) : knight ? KnightBody(seed) : TatteredBody(seed);

            if (type == "Banshee") Hair(r, tint, seed, behind: true);

            r.Glow(body, ColdGlow.WithAlpha(0.32f), 30);
            // Translucent: solid at the head, fading to nothing at the tail.
            r.Fill(body, (x, y) =>
            {
                float t = Clamp01((y - 60) / 185f);
                var c = Rgba.Lerp(tint.Shade(0.22f), tint.Shade(-0.18f), t);
                return c.WithAlpha(0.9f * (1 - t * t * 0.97f));
            });
            r.Fill(body, Noise.Mottled(new Rgba(1, 1, 1, 0.08f), 48, 0.55f, 0.3f, seed)); // faint mist inside the body
            r.Stroke(body, 10, tint.Shade(-0.4f).WithAlpha(0.16f), 10);                   // sunken edges

            if (knight)
            {
                KnightNeck(r);
                HeldHelmet(r, seed);
            }
            else Eyes(r, type, expr, wisp ? 150 : 112, wisp ? 18 : 25, wisp ? 0.75f : 1f);
            Mouth(r, type, expr, wisp ? 184 : 150);
            Accessory(r, type, tint, seed);
            if (type == "Banshee") Hair(r, tint, seed, behind: false);
            r.Mask(Box(128, 128, 124, 124, 48), 16); // soft edges: the glow never ends in a hard sprite border
            return r;
        }

        static Sdf TatteredBody(int seed)
        {
            var dome = Union(Circle(128, 100, 70), Rect(58, 100, 140, 60));
            var tail = Polygon((58, 150), (198, 150), (206, 194), (188, 232), (170, 204), (152, 248), (134, 210),
                (114, 242), (98, 206), (76, 236), (62, 198), (50, 170));
            return Displace(SmoothUnion(dome, tail, 14), 5f, 18f, seed);
        }

        static Sdf WispBody(int seed) => Displace(Union(
            SmoothUnion(Circle(128, 166, 50), Polygon((128, 34), (94, 142), (162, 142)), 22),
            Polygon((92, 70), (82, 150), (116, 140)),
            Polygon((166, 62), (140, 140), (174, 150))), 4f, 12f, seed);

        static Sdf KnightBody(int seed)
        {
            var torso = Rect(62, 92, 132, 80, 34); // rounded shoulders, no head
            var tail = Polygon((62, 150), (194, 150), (204, 196), (186, 232), (166, 204), (148, 246), (128, 210),
                (108, 240), (90, 204), (70, 232), (56, 190));
            return Displace(SmoothUnion(torso, tail, 12), 4f, 16f, seed);
        }

        static void KnightNeck(Raster r)
        {
            r.Fill(Ellipse(128, 96, 28, 10), Socket);
            var wisps = Union(Path(6, (120, 92), (112, 72), (120, 52), (114, 34)), Path(5, (136, 92), (144, 70), (138, 50)));
            r.Glow(wisps, ColdGlow.WithAlpha(0.5f), 12);
            r.Fill(wisps, ColdGlow.WithAlpha(0.55f));
        }

        static void HeldHelmet(Raster r, int seed)
        {
            var steel = Rgba.Hex("#7D8492");
            var helm = Union(Intersect(Circle(198, 196, 32), Rect(166, 164, 64, 34)), Rect(168, 192, 60, 18, 4));
            r.Fill(helm, steel);
            r.Fill(helm, Noise.Mottled(Rgba.Hex("#8B4A2B", 0.7f), 10, 0.55f, 0.15f, seed)); // rust
            r.Fill(Capsule(178, 186, 218, 186, 7), Socket);
            r.Glow(Union(Circle(188, 186, 3), Circle(208, 186, 3)), EyeGlow.WithAlpha(0.8f), 8);
            r.Fill(Union(Circle(188, 186, 2.5f), Circle(208, 186, 2.5f)), EyeGlow);
        }

        static void Eyes(Raster r, string type, string expr, float y, float dx, float k)
        {
            float rx = 12 * k, ry = (expr == "happy" ? 9 : expr == "sad" ? 17 : 15) * k;
            // Sunken shadow around the sockets.
            r.Fill(Ellipse(128 - dx, y + 3, rx + 8, ry + 9), Socket.WithAlpha(0.2f), 8);
            r.Fill(Ellipse(128 + dx, y, rx + 8, ry + 9), Socket.WithAlpha(0.2f), 8);
            // Hollow sockets, deliberately uneven.
            Sdf left = Ellipse(128 - dx, y + 3, rx + 1.5f, ry + 1.5f), right = Ellipse(128 + dx, y, rx, ry);
            if (expr == "sad")
            {
                left = Rotate(left, -14, 128 - dx, y + 3);
                right = Rotate(right, 14, 128 + dx, y);
            }
            r.Fill(left, Socket);
            r.Fill(right, Socket);
            // Pinprick lights: up when pleased, down when sad.
            float py = expr == "happy" ? -2 : expr == "sad" ? 6 : 1;
            var pupils = Union(Circle(128 - dx + 2, y + 3 + py, 2.6f), Circle(128 + dx + 2, y + py, 2.6f));
            r.Glow(pupils, EyeGlow.WithAlpha(0.7f), 7);
            r.Fill(pupils, EyeGlow);

            if (type == "Weeper") // black tear streaks
            {
                var tears = Union(Path(3.5f, (128 - dx, y + ry + 2), (126 - dx, y + ry + 26), (129 - dx, y + ry + 52)),
                    Path(3f, (128 + dx + 2, y + ry), (130 + dx, y + ry + 30)));
                r.Fill(tears, Socket.WithAlpha(0.55f));
            }
        }

        static void Mouth(Raster r, string type, string expr, float y)
        {
            switch (type)
            {
                case "HeadlessKnight":
                    return;
                case "Poltergeist": // jagged grin
                    if (expr == "sad")
                    {
                        r.Fill(Path(4, (104, y + 4), (114, y - 2), (124, y + 5), (134, y - 2), (144, y + 5), (154, y)), Socket);
                        return;
                    }
                    var grin = Intersect(Ellipse(128, y + 2, 28, expr == "happy" ? 14 : 10), Rect(96, y - 2, 64, 24));
                    r.Fill(grin, Socket);
                    for (int i = 0; i < 5; i++)
                        r.Fill(Polygon((104 + i * 10, y - 1), (113 + i * 10, y - 1), (108.5f + i * 10, y + 7)), Bone);
                    return;
                case "Banshee": // always mid-wail
                    r.Fill(Ellipse(128, y + 8, 11, expr == "happy" ? 12 : 21), Socket);
                    return;
            }
            switch (expr)
            {
                case "happy": r.Fill(Arc(128, y - 16, 24, 32, 148, 4), Socket); break; // thin, too-wide smile
                case "sad": r.Fill(Ellipse(128, y + 8, 8, 16), Socket); break;        // long open droop
                default: r.Fill(Ellipse(128, y + 2, 6, 9), Socket); break;           // small dark "o"
            }
        }

        static void Accessory(Raster r, string type, Rgba tint, int seed)
        {
            switch (type)
            {
                case "Victorian": // crooked, dusty top hat + ragged lace collar
                    var hatColor = Rgba.Hex("#2B2226");
                    var hat = Rotate(Union(Rect(98, 0, 60, 40, 3), Rect(78, 34, 100, 9, 4)), -12, 128, 34);
                    r.Fill(hat, hatColor);
                    r.Fill(Rotate(Rect(98, 26, 60, 7), -12, 128, 34), Tarnish);
                    r.Fill(hat, Noise.Mottled(new Rgba(0.8f, 0.8f, 0.8f, 0.25f), 6, 0.6f, 0.15f, seed)); // dust
                    r.Fill(Displace(Ellipse(128, 154, 48, 9), 3, 5, seed), Bone.WithAlpha(0.55f));
                    break;
                case "Drowned": // torn cap, seaweed, dripping water
                    r.Fill(Ellipse(128, 44, 48, 15), Rgba.Hex("#1F2A44"));
                    r.Fill(Subtract(Rect(82, 46, 92, 13, 6), Polygon((140, 44), (152, 62), (130, 62))), Bone.WithAlpha(0.6f));
                    var weed = Rgba.Hex("#3E6B4A", 0.85f);
                    r.Fill(Path(6, (86, 70), (78, 104), (88, 132), (80, 166), (88, 196)), weed);
                    r.Fill(Path(5, (166, 62), (176, 96), (168, 124), (178, 150)), weed);
                    var drip = Rgba.Hex("#7FB8C8", 0.75f);
                    r.Fill(SmoothUnion(Circle(112, 238, 4), Polygon((112, 226), (109, 237), (115, 237)), 2), drip);
                    r.Fill(SmoothUnion(Circle(150, 250, 3.5f), Polygon((150, 240), (147, 249), (153, 249)), 2), drip);
                    break;
                case "ChildGhost": // faded, crooked bow and a stitched tear
                    var bow = Rgba.Hex("#C98DA0", 0.85f);
                    r.Fill(Rotate(Union(Polygon((160, 46), (138, 30), (140, 60)), Polygon((160, 46), (182, 34), (178, 62))), 14, 160, 46), bow);
                    r.Fill(Circle(160, 46, 6), bow.Shade(-0.2f));
                    r.Fill(Path(3, (168, 120), (186, 138)), Socket.WithAlpha(0.5f));
                    for (int i = 0; i < 3; i++) r.Fill(Capsule(170 + i * 6, 134 - i * 6, 178 + i * 6, 124 - i * 6, 2), Socket.WithAlpha(0.45f));
                    break;
                case "Weeper": // tattered mourning veil
                    var veil = Displace(Intersect(Circle(128, 100, 76), Rect(40, 20, 176, 64)), 4, 8, seed);
                    r.Fill(veil, Rgba.Hex("#1C1626", 0.5f));
                    r.Stroke(veil, 2, Rgba.Hex("#1C1626", 0.6f));
                    break;
                case "Poltergeist": // a dragging chain
                    var steel = Rgba.Hex("#6E6A72");
                    (float x, float y)[] pts = { (56, 150), (72, 162), (90, 172), (110, 178), (130, 178), (150, 172), (170, 162), (188, 150) };
                    for (int i = 0; i < pts.Length; i++)
                    {
                        var link = Ellipse(pts[i].x, pts[i].y, i % 2 == 0 ? 8 : 5, i % 2 == 0 ? 5 : 8);
                        r.Stroke(link, 3, steel);
                    }
                    break;
                case "Bartholomew": // faded bellhop pillbox cap, tilted
                    var red = Rgba.Hex("#8E2F35");
                    r.Fill(Rotate(Rect(96, 22, 64, 34, 6), -10, 128, 40), red);
                    r.Fill(Rotate(Rect(96, 46, 64, 9, 3), -10, 128, 40), Tarnish);
                    r.Fill(Rotate(Circle(128, 18, 6), -10, 128, 40), Tarnish);
                    break;
            }
        }

        static void Hair(Raster r, Rgba tint, int seed, bool behind)
        {
            var hair = tint.Shade(-0.5f).WithAlpha(behind ? 0.9f : 0.8f);
            var rng = new Random(seed);
            if (behind)
            {
                for (int i = 0; i < 6; i++)
                {
                    float side = i < 3 ? -1 : 1, x = 128 + side * (52 + (i % 3) * 10);
                    r.Fill(Path(7, (x, 60), (x + side * (6 + rng.Next(8)), 130), (x - side * rng.Next(6), 190), (x + side * rng.Next(10), 240)), hair);
                }
            }
            else
            {
                r.Fill(Path(4, (112, 40), (100, 76), (106, 104)), hair);
                r.Fill(Path(4, (146, 40), (158, 80), (150, 100)), hair);
            }
        }

        /// <summary>256×256 portrait: head-and-shoulders on a dark disc, same framing for every guest (GDD §7.3).</summary>
        /// <summary>Mr. Vane: gaunt "developer" in a top hat, amber eyes, a smile with too many teeth.</summary>
        public static Raster VanePortrait(string expr)
        {
            var r = new Raster(256, 256);
            var disc = Circle(128, 128, 124);
            r.Fill(disc, Raster.RadialGradient(128, 110, 150, Rgba.Hex("#2A1418"), Rgba.Hex("#07060C")));
            var layer = new Raster(256, 256);
            var suit = Rgba.Hex("#121018");
            layer.Fill(Union(Rect(52, 196, 152, 80, 30), Polygon((70, 200), (128, 172), (186, 200), (186, 256), (70, 256))), suit);
            layer.Fill(Polygon((112, 186), (144, 186), (136, 240), (120, 240)), Bone);                 // shirt
            layer.Fill(Polygon((122, 192), (134, 192), (132, 232), (124, 232)), Rgba.Hex("#7A1820")); // red tie
            var skin = Rgba.Hex("#C9C2BC");
            layer.Fill(Displace(Ellipse(128, 128, 40, 58), 2, 10, 5), skin);                          // long gaunt face
            layer.Fill(Ellipse(110, 150, 10, 16), skin.Shade(-0.25f).WithAlpha(0.6f), 8);             // hollow cheeks
            layer.Fill(Ellipse(146, 150, 10, 16), skin.Shade(-0.25f).WithAlpha(0.6f), 8);
            layer.Fill(Union(Rect(90, 14, 76, 62, 4), Rect(70, 68, 116, 12, 5)), suit);               // top hat
            layer.Fill(Rect(90, 58, 76, 8), Rgba.Hex("#7A1820"));
            float eyeY = 116;
            var eyes = Union(Ellipse(112, eyeY, 9, expr == "happy" ? 4 : 6), Ellipse(144, eyeY, 9, expr == "happy" ? 4 : 6));
            layer.Glow(eyes, Rgba.Hex("#F2A541", 0.7f), 10);
            layer.Fill(eyes, Rgba.Hex("#F2B441"));
            layer.Fill(Union(Capsule(98, eyeY - 14, 122, eyeY - 10, 3), Capsule(134, eyeY - 10, 158, eyeY - 14, 3)), Rgba.Hex("#2A2026")); // arched brows
            if (expr == "sad")
                layer.Fill(Capsule(112, 162, 144, 162, 3), Rgba.Hex("#3A2024"));                   // displeased line
            else
            {
                var smile = Intersect(Ellipse(128, 156, expr == "happy" ? 30 : 24, 12), Rect(90, 156, 76, 20));
                layer.Fill(smile, Rgba.Hex("#2A1418"));
                for (int i = 0; i < 7; i++)
                    layer.Fill(Rect(103 + i * 7.3f, 156, 5.6f, 6, 1), Bone);                           // too many teeth
            }
            layer.Mask(disc);
            r.Draw(layer, 0, 0);
            r.Fill(disc, Raster.RadialGradient(128, 128, 126, new Rgba(0, 0, 0, 0), new Rgba(0, 0, 0, 0.5f)));
            r.Stroke(disc, 4, Rgba.Hex("#7A1820"));
            return r;
        }

        /// <summary>Edith: the missing grandmother, seen from the in-between. Round glasses, grey bun, shawl.</summary>
        public static Raster EdithPortrait(string expr)
        {
            var r = new Raster(256, 256);
            var disc = Circle(128, 128, 124);
            r.Fill(disc, Raster.RadialGradient(128, 110, 150, Rgba.Hex("#1A2430"), Rgba.Hex("#07060C")));
            var layer = new Raster(256, 256);
            var spectral = Rgba.Hex("#C8D8E4");
            layer.Glow(Ellipse(128, 140, 60, 80), ColdGlow.WithAlpha(0.35f), 30);
            layer.Fill(Displace(Union(Rect(48, 190, 160, 90, 40), Polygon((60, 196), (128, 176), (196, 196), (196, 256), (60, 256))), 4, 10, 3),
                Rgba.Hex("#6B5A7A", 0.85f));                                                            // shawl
            layer.Fill(Ellipse(128, 128, 44, 52), spectral.WithAlpha(0.92f));                          // face
            layer.Fill(Union(Circle(128, 70, 26), Ellipse(128, 92, 50, 26)), Rgba.Hex("#A8A8B0"));     // grey bun + hair
            layer.Fill(Union(Ring(Circle(110, 124, 13), 2.5f), Ring(Circle(146, 124, 13), 2.5f), Capsule(123, 124, 133, 124, 3)), Tarnish); // glasses
            float py = expr == "sad" ? 128 : 124;
            layer.Fill(Union(Circle(110, py, 3.5f), Circle(146, py, 3.5f)), Socket);
            switch (expr)
            {
                case "happy": layer.Fill(Arc(128, 140, 14, 25, 155, 3.5f), Socket.WithAlpha(0.8f)); break;
                case "sad": layer.Fill(Arc(128, 164, 12, 210, 330, 3.5f), Socket.WithAlpha(0.8f)); break;
                default: layer.Fill(Capsule(120, 152, 136, 152, 3), Socket.WithAlpha(0.7f)); break;
            }
            layer.Mask(disc);
            r.Draw(layer, 0, 0);
            r.Fill(disc, Raster.RadialGradient(128, 128, 126, new Rgba(0, 0, 0, 0), new Rgba(0, 0, 0, 0.5f)));
            r.Stroke(disc, 4, Tarnish);
            return r;
        }

        public static Raster Portrait(string type, string expr)
        {
            var r = new Raster(256, 256);
            var disc = Circle(128, 128, 124);
            r.Fill(disc, Raster.RadialGradient(128, 110, 150, Rgba.Hex("#1E1A2C"), Rgba.Hex("#07060C")));
            var layer = new Raster(256, 256);
            layer.Draw(Ghost(type, expr), 0, 34);
            layer.Mask(disc);
            r.Draw(layer, 0, 0);
            r.Fill(disc, Raster.RadialGradient(128, 128, 126, new Rgba(0, 0, 0, 0), new Rgba(0, 0, 0, 0.55f))); // vignette
            r.Stroke(disc, 4, Tarnish);
            return r;
        }
        // ------------------------------------------------------------------ icons

        /// <summary>128×128 tag icon: cream disc, brass ring, one symbol (GDD §4.2).</summary>
        public static Raster TagIcon(string tag)
        {
            var r = new Raster(128, 128);
            var disc = Circle(64, 64, 60);
            r.Fill(disc, Rgba.Hex("#E3D5B6"));                                                     // aged parchment
            r.Fill(disc, Noise.Mottled(Rgba.Hex("#7A5A36", 0.35f), 26, 0.58f, 0.2f, Seed(tag)));    // stains
            r.Stroke(Circle(64, 64, 58), 5, Tarnish);

            switch (tag)
            {
                case "Dark": // crescent moon
                    r.Fill(Subtract(Circle(62, 64, 30), Circle(76, 54, 26)), DeepPurple);
                    break;
                case "Water":
                    r.Fill(Drop(64, 74, 22, 36), Rgba.Hex("#4A90D9"));
                    r.Fill(Ellipse(56, 74, 5, 9), new Rgba(1, 1, 1, 0.6f));
                    break;
                case "Warm":
                    r.Fill(Drop(64, 76, 24, 40), LampAmber);
                    r.Fill(Drop(64, 84, 12, 22), WarmGlow);
                    break;
                case "Attic":
                    r.Stroke(Polygon((64, 26), (100, 58), (28, 58)), 7, Ink);
                    r.Stroke(Rect(38, 58, 52, 40), 7, Ink);
                    r.Fill(Circle(64, 76, 7), LampAmber);
                    break;
                case "Basement":
                    r.Fill(Union(Capsule(32, 44, 54, 44, 7), Capsule(54, 44, 54, 64, 7), Capsule(54, 64, 74, 64, 7),
                        Capsule(74, 64, 74, 84, 7), Capsule(74, 84, 96, 84, 7)), Ink);
                    r.Fill(Union(Capsule(72, 30, 96, 52, 6), Polygon((100, 56), (84, 54), (98, 40))), Broken);
                    break;
                case "Mirror":
                    r.Fill(Ellipse(64, 60, 24, 32), Rgba.Hex("#CFE6F2"));
                    r.Stroke(Ellipse(64, 60, 24, 32), 6, Brass.Shade(-0.2f));
                    r.Fill(Capsule(64, 92, 64, 104, 6), Brass.Shade(-0.2f));
                    r.Fill(Capsule(54, 50, 62, 40, 4), new Rgba(1, 1, 1, 0.8f));
                    break;
                case "Music":
                    r.Fill(Union(Rotate(Ellipse(52, 86, 13, 9), -20, 52, 86), Capsule(63, 84, 63, 34, 6), Capsule(63, 34, 84, 44, 6)), Ink);
                    r.Fill(Union(Rotate(Ellipse(82, 80, 11, 8), -20, 82, 80), Capsule(91, 78, 91, 42, 5)), Ink);
                    break;
                case "Garden":
                    r.Fill(Rotate(Intersect(Circle(46, 64, 40), Circle(82, 64, 40)), -40, 64, 64), Valid);
                    r.Fill(Rotate(Capsule(40, 64, 88, 64, 3), -40, 64, 64), Valid.Shade(-0.3f));
                    break;
                case "Cursed":
                    r.Fill(Arc(64, 64, 26, 200, 520, 6), Aura);
                    r.Fill(Arc(64, 64, 12, 20, 300, 5), Aura);
                    r.Fill(Circle(90, 34, 5), Aura);
                    r.Fill(Circle(36, 94, 4), Aura);
                    break;
                case "Noisy":
                    r.Fill(Union(Rect(30, 50, 16, 28, 3), Polygon((44, 52), (62, 36), (62, 92), (44, 76))), Ink);
                    r.Fill(Arc(66, 64, 16, -45, 45, 5), Aura);
                    r.Fill(Arc(66, 64, 28, -45, 45, 5), Aura);
                    r.Fill(Arc(66, 64, 40, -40, 40, 5), Aura);
                    break;
                case "Cold":
                    var ice = Rgba.Hex("#5AA9E6");
                    for (int i = 0; i < 3; i++)
                        r.Fill(Rotate(Capsule(64, 30, 64, 98, 7), i * 60, 64, 64), ice);
                    r.Fill(Circle(64, 64, 8), ice);
                    break;
            }
            return r;
        }

        static Sdf Drop(float cx, float cy, float r, float height) =>
            SmoothUnion(Circle(cx, cy, r), Polygon((cx, cy - height), (cx - r * 0.85f, cy - 2), (cx + r * 0.85f, cy - 2)), 6);

        static Raster StarIcon(bool filled)
        {
            var r = new Raster(64, 64);
            var star = Star(32, 34, 28, 12);
            if (filled)
            {
                r.Glow(star, WarmGlow.WithAlpha(0.5f), 8);
                r.Fill(star, Raster.VerticalGradient(6, WarmGlow.Shade(0.3f), 60, LampAmber));
            }
            else r.Fill(star, DuskViolet.WithAlpha(0.7f));
            r.Stroke(star, 2.5f, filled ? Brass : DuskViolet.Shade(0.2f));
            return r;
        }

        static Raster MoonIcon(bool filled)
        {
            var r = new Raster(64, 64);
            var c = Circle(32, 32, 26);
            r.Fill(c, filled ? WarmGlow : DuskViolet.WithAlpha(0.6f));
            r.Stroke(c, 3, filled ? Brass : DuskViolet.Shade(0.2f));
            if (filled) r.Fill(Circle(24, 26, 5), LampAmber.WithAlpha(0.5f));
            return r;
        }

        static Raster RoundedSquare(int size, float radius)
        {
            var r = new Raster(size, size);
            r.Fill(Rect(0, 0, size, size, radius), Rgba.White);
            return r;
        }

        static Raster CircleSprite(int size)
        {
            var r = new Raster(size, size);
            r.Fill(Circle(size / 2f, size / 2f, size / 2f - 1), Rgba.White);
            return r;
        }

        static Raster GlowSprite(int size)
        {
            var r = new Raster(size, size);
            r.Fill(Circle(size / 2f, size / 2f, size / 2f), Raster.RadialGradient(size / 2f, size / 2f, size / 2f, Rgba.White, Rgba.White.WithAlpha(0)));
            return r;
        }

        static Raster Hand()
        {
            var r = new Raster(128, 128);
            var hand = Union(Rect(40, 56, 52, 50, 18), Capsule(60, 64, 60, 14, 18), Capsule(80, 62, 86, 40, 14), Capsule(40, 76, 26, 62, 14));
            r.Glow(hand, WarmGlow.WithAlpha(0.6f), 14);
            r.Fill(hand, PaperCream);
            r.Stroke(hand, 4, Ink);
            r.Fill(Rect(40, 104, 52, 14, 4), Brass);
            return r;
        }

        static Raster LockIcon()
        {
            var r = new Raster(64, 64);
            r.Fill(Arc(32, 28, 13, 180, 360, 6), Brass.Shade(-0.2f));
            r.Fill(Rect(14, 28, 36, 28, 6), Brass);
            r.Fill(Circle(32, 40, 4), Ink);
            return r;
        }

        static Raster WaxSeal()
        {
            var r = new Raster(128, 128);
            var red = Rgba.Hex("#A8323A");
            var blob = Union(Circle(64, 64, 50), Circle(30, 40, 14), Circle(100, 34, 12), Circle(104, 98, 14), Circle(26, 96, 12));
            r.Fill(blob, red);
            r.Stroke(Circle(64, 64, 36), 4, red.Shade(-0.3f));
            r.Fill(Star(64, 66, 22, 10), red.Shade(0.25f));
            return r;
        }

        // ------------------------------------------------------------------ scenes (haunted)

        static readonly Rgba Wall = Rgba.Hex("#2A2238"), Plaster = Rgba.Hex("#5E5462"), OldWood = Rgba.Hex("#3A2A24"),
            Moonlight = Rgba.Hex("#BFD8E8"), Candle = Rgba.Hex("#F2C46A"), Web = Rgba.Hex("#D8D8E0");

        /// <summary>Faded stripes, grime, water stains and patches where the paper has peeled off.</summary>
        static void PeelingWallpaper(Raster r, Rgba baseColor, int stripe, int seed, int patches)
        {
            r.Clear(baseColor);
            for (int x = 0; x < r.Width; x += stripe)
                r.Fill(Rect(x, 0, stripe * 0.32f, r.Height), baseColor.Shade(0.05f));
            var full = Rect(0, 0, r.Width, r.Height);
            r.Fill(full, Noise.Mottled(Rgba.Hex("#0A0810", 0.45f), r.Width / 6f, 0.48f, 0.25f, seed));         // grime
            r.Fill(full, Noise.Mottled(Rgba.Hex("#4A3B2A", 0.25f), r.Width / 10f, 0.62f, 0.12f, seed + 9));    // water stains
            var rng = new Random(seed);
            for (int i = 0; i < patches; i++)
            {
                float cx = (float)rng.NextDouble() * r.Width, cy = (float)rng.NextDouble() * r.Height * 0.7f;
                float w = Math.Min(r.Width * (0.03f + (float)rng.NextDouble() * 0.035f), 90f), h = w * (0.7f + (float)rng.NextDouble() * 0.8f);
                var patch = Displace(Ellipse(cx, cy, w, h), w * 0.3f, w * 0.35f, seed + i);
                r.Fill(patch, Rgba.Lerp(baseColor, Plaster, 0.6f).WithAlpha(0.9f));
                r.Fill(patch, Noise.Mottled(Rgba.Hex("#2A2328", 0.4f), w * 0.3f, 0.5f, 0.2f, seed + i));
                r.Stroke(patch, 3, Rgba.Hex("#1A1420", 0.55f), 2);                                             // curled edge
            }
        }

        static void Cracks(Raster r, int seed, int count, float x0, float y0, float x1, float y1)
        {
            var rng = new Random(seed);
            for (int c = 0; c < count; c++)
            {
                float x = x0 + (float)rng.NextDouble() * (x1 - x0), y = y0 + (float)rng.NextDouble() * (y1 - y0);
                var pts = new List<(float, float)> { (x, y) };
                float ang = (float)(rng.NextDouble() * Math.PI * 2);
                for (int s = 0; s < 6; s++)
                {
                    ang += (float)(rng.NextDouble() - 0.5) * 1.4f;
                    x += (float)Math.Cos(ang) * (10 + rng.Next(18));
                    y += (float)Math.Sin(ang) * (10 + rng.Next(18));
                    pts.Add((x, y));
                }
                r.Fill(Path(2.2f, pts.ToArray()), Rgba.Hex("#0B0910", 0.65f));
            }
        }

        /// <summary>Cobweb in a corner: radial threads plus sagging rings.</summary>
        static void Cobweb(Raster r, float cx, float cy, float size, int dirX, int dirY, float alpha = 0.35f)
        {
            var c = Web.WithAlpha(alpha);
            const int spokes = 6;
            for (int i = 0; i <= spokes; i++)
            {
                double a = i * Math.PI / 2 / spokes;
                r.Fill(Capsule(cx, cy, cx + dirX * (float)Math.Cos(a) * size, cy + dirY * (float)Math.Sin(a) * size, 1.4f), c);
            }
            for (int ring = 1; ring <= 4; ring++)
            {
                float rr = size * ring / 4.5f;
                var pts = new List<(float, float)>();
                for (int i = 0; i <= spokes; i++)
                {
                    double a = i * Math.PI / 2 / spokes;
                    float sag = (i % 2 == 1) ? 0.85f : 1f; // threads sag between spokes
                    pts.Add((cx + dirX * (float)Math.Cos(a) * rr * sag, cy + dirY * (float)Math.Sin(a) * rr * sag));
                }
                r.Fill(Path(1.2f, pts.ToArray()), c);
            }
        }

        /// <summary>Old floorboards with dark gaps, scratches and dust.</summary>
        static void GappyFloor(Raster r, float top, int seed)
        {
            r.Fill(Rect(0, top, r.Width, r.Height - top), Raster.VerticalGradient(top, OldWood.Shade(0.05f), r.Height, OldWood.Shade(-0.45f)));
            r.Fill(Rect(0, top, r.Width, 5), Tarnish.WithAlpha(0.7f));
            var rng = new Random(seed);
            for (float y = top + 26; y < r.Height; y += 26)
            {
                r.Fill(Rect(0, y, r.Width, 3), Rgba.Hex("#0B0806", 0.8f));
                for (int k = 0; k < r.Width / 160; k++)
                    r.Fill(Rect(rng.Next(r.Width), y - 26, 3, 26), Rgba.Hex("#0B0806", 0.6f));
            }
            r.Fill(Rect(0, top, r.Width, r.Height - top), Noise.Mottled(Rgba.Hex("#8A8070", 0.18f), 40, 0.6f, 0.2f, seed)); // dust
        }

        /// <summary>Low drifting mist between two heights.</summary>
        static void Mist(Raster r, float y0, float y1, int seed, float alpha = 0.3f)
        {
            r.Fill(Rect(0, y0, r.Width, y1 - y0), (x, y) =>
            {
                float t = (y - y0) / (y1 - y0);
                float band = (float)Math.Sin(t * Math.PI);
                float n = Noise.Fbm(x, y * 2.2f, r.Width / 8f, 4, seed);
                return new Rgba(0.82f, 0.86f, 0.92f, Clamp01((n - 0.35f) * 2f) * band * alpha);
            });
        }

        static void DeadTree(Raster r, float x, float y, float len, float angleDeg, float thickness, int depth, Random rng, Rgba color)
        {
            if (depth == 0 || len < 4) return;
            double a = angleDeg * Math.PI / 180;
            float x2 = x + (float)Math.Cos(a) * len, y2 = y - (float)Math.Sin(a) * len;
            r.Fill(Capsule(x, y, x2, y2, thickness), color);
            int branches = 2 + (rng.NextDouble() < 0.3 ? 1 : 0);
            for (int i = 0; i < branches; i++)
                DeadTree(r, x2, y2, len * (0.6f + (float)rng.NextDouble() * 0.2f), angleDeg + (float)(rng.NextDouble() - 0.5) * 80,
                    thickness * 0.65f, depth - 1, rng, color);
        }

        static void Bat(Raster r, float x, float y, float s, Rgba c)
        {
            r.Fill(Polygon((x, y), (x - 12 * s, y - 8 * s), (x - 22 * s, y - 2 * s), (x - 16 * s, y + 2 * s), (x - 8 * s, y),
                (x - 2 * s, y + 5 * s), (x + 2 * s, y + 5 * s), (x + 8 * s, y), (x + 16 * s, y + 2 * s), (x + 22 * s, y - 2 * s), (x + 12 * s, y - 8 * s)), c);
        }

        static void CandleFlame(Raster r, float x, float y, float s)
        {
            r.Glow(Circle(x, y - 6 * s, 10 * s), Candle.WithAlpha(0.35f), 50 * s);
            r.Fill(Rect(x - 4 * s, y, 8 * s, 22 * s, 2), Bone.Shade(-0.1f));
            r.Fill(SmoothUnion(Circle(x, y - 4 * s, 4 * s), Polygon((x, y - 18 * s), (x - 3.5f * s, y - 5 * s), (x + 3.5f * s, y - 5 * s)), 2), Candle);
        }

        /// <summary>512×512 room stage (GDD §7.4) gone to seed: peeling paper, a boarded window, cobwebs, cold moonlight.</summary>
        public static Raster RoomStage()
        {
            var r = new Raster(512, 512);
            PeelingWallpaper(r, Wall, 40, 31, 3);
            Cracks(r, 5, 3, 260, 60, 480, 360);
            // Window: tarnished arch, dark glass with a cracked pane, one plank nailed across.
            var frame = Union(Rect(52, 96, 150, 170, 12), Circle(127, 110, 75));
            var glass = Intersect(Grow(frame, -10), Rect(0, 30, 512, 500));
            r.Fill(frame, Tarnish);
            r.Fill(glass, Raster.VerticalGradient(40, Rgba.Hex("#0D1424"), 260, Rgba.Hex("#1C2238")));
            r.Glow(Circle(150, 92, 16), Moonlight.WithAlpha(0.45f), 30);
            r.Fill(Circle(150, 92, 15), Rgba.Hex("#E8F0E0"));
            r.Fill(Rect(122, 40, 9, 220), Tarnish);
            r.Fill(Rect(52, 176, 150, 8), Tarnish);
            r.Fill(Path(1.6f, (70, 200), (92, 214), (86, 236), (110, 252)), Moonlight.WithAlpha(0.6f));     // crack in the glass
            r.Fill(Rotate(Rect(36, 150, 186, 22, 3), -9, 128, 160), OldWood.Shade(0.15f));                    // plank
            r.Fill(Union(Circle(54, 174, 3), Circle(202, 146, 3)), Rgba.Hex("#9A9AA0"));
            // Cold moonbeam onto the floor.
            r.Fill(Polygon((70, 250), (190, 250), (330, 470), (120, 470)), Moonlight.WithAlpha(0.07f), 20);
            // Crooked portrait on the right wall.
            var pic = Rotate(Rect(330, 110, 110, 140, 4), 8, 385, 180);
            r.Fill(pic, Tarnish.Shade(-0.2f));
            r.Fill(Rotate(Rect(340, 120, 90, 120, 2), 8, 385, 180), Rgba.Hex("#1A1622"));
            r.Fill(Rotate(Union(Circle(385, 165, 20), Rect(358, 180, 54, 50, 20)), 8, 385, 180), Rgba.Hex("#2E2836"));
            r.Fill(Rotate(Union(Circle(378, 164, 2.5f), Circle(392, 164, 2.5f)), 8, 385, 180), Rgba.Hex("#C9D3DC", 0.8f)); // its eyes
            Cobweb(r, 0, 0, 120, 1, 1);
            Cobweb(r, 512, 0, 90, -1, 1);
            GappyFloor(r, 420, 7);
            Mist(r, 360, 512, 3, 0.35f);
            return r;
        }

        /// <summary>1920×1080 exterior for the Main Menu: a sagging, half-boarded hotel on a foggy hill.</summary>
        public static Raster Exterior()
        {
            var r = new Raster(1920, 1080);
            r.Fill(Rect(0, 0, 1920, 1080), Raster.VerticalGradient(0, Rgba.Hex("#06060E"), 900, Rgba.Hex("#2A2240")));
            var rng = new Random(7);
            for (int i = 0; i < 60; i++)
                r.Fill(Circle(rng.Next(1920), rng.Next(560), 1f + (float)rng.NextDouble() * 1.2f), Bone.WithAlpha(0.3f + (float)rng.NextDouble() * 0.4f));
            r.Glow(Circle(1560, 190, 80), Moonlight.WithAlpha(0.3f), 200);
            r.Fill(Circle(1560, 190, 80), Rgba.Hex("#E4ECD6"));
            r.Fill(Circle(1560, 190, 80), Noise.Mottled(Rgba.Hex("#9AA290", 0.5f), 30, 0.5f, 0.2f, 4)); // craters
            // Clouds across the moon.
            r.Fill(Displace(Ellipse(1560, 205, 300, 46), 30, 60, 11), Rgba.Hex("#14121E", 0.8f), 40);
            r.Fill(Displace(Ellipse(1700, 150, 180, 26), 20, 40, 12), Rgba.Hex("#14121E", 0.7f), 30);
            for (int i = 0; i < 5; i++) Bat(r, 1350 + i * 70 + rng.Next(30), 300 + rng.Next(90), 1.2f + (float)rng.NextDouble() * 0.8f, Rgba.Hex("#05040A"));

            // Hill, dead trees, gravestones, fence.
            var ground = Rgba.Hex("#0B0A14");
            r.Fill(Union(Circle(300, 1290, 580), Circle(1650, 1320, 640), Rect(0, 940, 1920, 140)), ground);
            DeadTree(r, 260, 860, 140, 92, 18, 6, new Random(3), ground);
            DeadTree(r, 1700, 880, 160, 86, 20, 6, new Random(5), ground);
            DeadTree(r, 1450, 930, 90, 100, 12, 5, new Random(9), ground);
            foreach (var (gx, gy, s) in new[] { (420f, 930f, 1f), (500f, 950f, 0.8f), (1300f, 950f, 0.9f), (1380f, 935f, 1.1f), (1520f, 955f, 0.8f) })
                r.Fill(Rotate(Union(Rect(gx - 22 * s, gy - 50 * s, 44 * s, 60 * s, 4), Circle(gx, gy - 50 * s, 22 * s)), (gx % 13) - 6, gx, gy), Rgba.Hex("#2A2836"));

            // The hotel leans: build it upright, then rotate the whole silhouette a few degrees.
            const float lean = -3f, px = 960, py = 960;
            Sdf L(Sdf s) => Rotate(s, lean, px, py);
            var body = Rgba.Hex("#221C30");
            r.Fill(L(Displace(Polygon((960, 140), (1350, 330), (570, 330)), 6, 30, 2)), Rgba.Hex("#15111F"));     // ragged roof
            r.Fill(L(Rect(640, 320, 640, 640)), body);
            r.Fill(L(Rect(640, 320, 640, 640)), Noise.Mottled(Rgba.Hex("#0A0812", 0.5f), 120, 0.45f, 0.25f, 6)); // grime
            r.Fill(L(Rect(890, 200, 80, 90, 6)), Rgba.Hex("#C8B46A", 0.7f));                                     // attic window, dim
            var rng2 = new Random(21);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                float x = 690 + col * 145, y = 370 + row * 140;
                var win = L(Rect(x, y, 100, 100, 14));
                double roll = rng2.NextDouble();
                r.Fill(L(Rect(x - 8, y - 8, 116, 116, 16)), Rgba.Hex("#17121F"));
                if (roll < 0.35) // lit, sickly
                {
                    r.Glow(win, Candle.WithAlpha(0.25f), 30);
                    r.Fill(win, Rgba.Hex("#D9B860"));
                    r.Fill(L(Union(Rect(x + 47, y, 6, 100), Rect(x, y + 47, 100, 6))), Rgba.Hex("#3A2E24"));
                    if (rng2.NextDouble() < 0.5) // a silhouette watching from inside
                        r.Fill(L(Union(Circle(x + 50, y + 44, 15), Rect(x + 34, y + 54, 32, 46, 10))), Rgba.Hex("#1A1420", 0.85f));
                }
                else if (roll < 0.65) // boarded up
                {
                    r.Fill(win, Rgba.Hex("#0B0910"));
                    r.Fill(L(Rotate(Rect(x - 6, y + 30, 112, 16, 2), 18, x + 50, y + 38)), OldWood.Shade(0.15f));
                    r.Fill(L(Rotate(Rect(x - 6, y + 56, 112, 16, 2), -14, x + 50, y + 64)), OldWood.Shade(0.1f));
                }
                else // broken glass
                {
                    r.Fill(win, Rgba.Hex("#0B0910"));
                    r.Fill(L(Polygon((x, y), (x + 40, y), (x + 18, y + 30), (x + 30, y + 58), (x, y + 70))), Rgba.Hex("#26304A", 0.8f));
                }
            }
            r.Fill(L(Union(Rect(860, 860, 120, 100, 4), Circle(920, 860, 60))), Rgba.Hex("#0B0910"));            // door
            r.Glow(L(Circle(920, 900, 30)), Candle.WithAlpha(0.25f), 60);
            Cobweb(r, 640, 320, 120, 1, 1, 0.25f);
            // Fence
            for (int x = 0; x < 1920; x += 38)
                r.Fill(Union(Rect(x, 960, 7, 80), Polygon((x - 3, 962), (x + 3.5f, 946), (x + 10, 962))), Rgba.Hex("#05040A"));
            r.Fill(Rect(0, 990, 1920, 6), Rgba.Hex("#05040A"));
            Mist(r, 820, 1080, 8, 0.45f);
            DrawGhostAt(r, "Weeper", 470, 400, 0.35f);
            DrawGhostAt(r, "Victorian", 1180, 500, 0.3f);
            DrawGhostAt(r, "ChildGhost", 1380, 740, 0.3f);
            return r;
        }

        /// <summary>1920×1080 day lobby. Hotspot positions must match LobbyScreen (ledger, calendar, bell, Bartholomew).</summary>
        public static Raster Lobby()
        {
            var r = new Raster(1920, 1080);
            PeelingWallpaper(r, Wall.Shade(0.04f), 64, 41, 6);
            Cracks(r, 2, 6, 500, 80, 1450, 500);
            Cobweb(r, 0, 0, 260, 1, 1);
            Cobweb(r, 1920, 0, 220, -1, 1);
            GappyFloor(r, 820, 4);
            // Candle chandelier instead of a warm lamp, with hanging threads.
            r.Fill(Rect(956, 0, 6, 120), Tarnish);
            r.Stroke(Ellipse(960, 150, 130, 26), 6, Tarnish);
            for (int i = 0; i < 5; i++) CandleFlame(r, 850 + i * 55, 128, 1.3f);
            for (int i = 0; i < 4; i++) r.Fill(Path(1.2f, (860 + i * 70, 160), (856 + i * 70, 230 + i * 18)), Web.WithAlpha(0.3f));
            // Stained ledger (left) and torn calendar (right).
            var paper = Rgba.Hex("#D2C3A0");
            r.Fill(Rect(160, 230, 260, 340, 6), paper);
            r.Fill(Rect(160, 230, 260, 340, 6), Noise.Mottled(Rgba.Hex("#6B5032", 0.45f), 60, 0.55f, 0.2f, 13));
            string[] ink = { "#8E4A4A", "#4A6B8E", "#5A7A5A", "#9A8250", "#6E5A8E" };
            for (int i = 0; i < ink.Length; i++) r.Fill(Rect(195, 280 + i * 52, 190, 18, 6), Rgba.Hex(ink[i], 0.8f));
            var cal = Displace(Rect(1500, 230, 260, 300, 6), 5, 14, 17); // ripped edges
            r.Fill(cal, paper);
            r.Fill(cal, Noise.Mottled(Rgba.Hex("#6B5032", 0.4f), 50, 0.55f, 0.2f, 19));
            r.Fill(Intersect(cal, Rect(1490, 220, 280, 76)), Rgba.Hex("#7A2E2E"));
            // Crooked portrait of Edith between ledger and desk.
            r.Fill(Rotate(Rect(560, 240, 190, 240, 4), -5, 655, 360), Tarnish.Shade(-0.15f));
            r.Fill(Rotate(Rect(574, 254, 162, 212, 2), -5, 655, 360), Rgba.Hex("#1A1622"));
            r.Fill(Rotate(Union(Circle(655, 330, 34), Rect(612, 356, 86, 110, 34)), -5, 655, 360), Rgba.Hex("#3A3044"));
            // Dusty, scratched desk and a tarnished bell.
            r.Fill(Rect(560, 600, 800, 36, 6), Tarnish.Shade(-0.1f));
            r.Fill(Rect(600, 636, 720, 300, 8), OldWood.Shade(0.08f));
            r.Fill(Rect(650, 686, 620, 200, 6), OldWood.Shade(-0.1f));
            r.Fill(Rect(600, 636, 720, 300, 8), Noise.Mottled(Rgba.Hex("#8A8070", 0.2f), 30, 0.62f, 0.15f, 23));
            Cracks(r, 8, 3, 640, 680, 1280, 900);
            r.Fill(Rect(900, 580, 120, 16, 6), Tarnish.Shade(-0.2f));
            r.Fill(Intersect(Circle(960, 586, 46), Rect(900, 520, 120, 66)), Tarnish.Shade(0.15f));
            r.Fill(Circle(960, 534, 8), Tarnish);
            CandleFlame(r, 1300, 572, 1.2f);
            Mist(r, 760, 1080, 12, 0.3f);
            return r;
        }

        /// <summary>1920×1080 arrival desk: a long, dusty counter with half the room keys missing.</summary>
        public static Raster Desk()
        {
            var r = new Raster(1920, 1080);
            PeelingWallpaper(r, Wall, 56, 51, 5);
            Cracks(r, 3, 5, 100, 80, 1200, 560);
            Cobweb(r, 0, 0, 220, 1, 1);
            // Key cabinet, several hooks empty.
            r.Fill(Rect(1320, 140, 420, 360, 8), OldWood.Shade(0.12f));
            var rng = new Random(2);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
            {
                r.Fill(Rect(1350 + col * 80, 170 + row * 110, 60, 80, 6), OldWood.Shade(-0.2f));
                r.Fill(Circle(1380 + col * 80, 186 + row * 110, 4), Tarnish);
                if (rng.NextDouble() < 0.55)
                    r.Fill(Union(Capsule(1380 + col * 80, 190 + row * 110, 1380 + col * 80, 222 + row * 110, 5), Circle(1380 + col * 80, 228 + row * 110, 9)), Tarnish.Shade(0.1f));
            }
            Cobweb(r, 1740, 140, 100, -1, 1, 0.3f);
            // Counter
            r.Fill(Rect(0, 640, 1920, 36, 4), Tarnish.Shade(-0.15f));
            r.Fill(Rect(0, 676, 1920, 404), Raster.VerticalGradient(676, OldWood.Shade(0.05f), 1080, OldWood.Shade(-0.5f)));
            for (int x = 80; x < 1920; x += 360) r.Fill(Rect(x, 716, 280, 300, 8), OldWood.Shade(-0.12f));
            r.Fill(Rect(0, 676, 1920, 404), Noise.Mottled(Rgba.Hex("#8A8070", 0.18f), 40, 0.6f, 0.2f, 29));
            // Stained guest book, a candle and the bell.
            r.Fill(Rotate(Rect(380, 590, 260, 60, 6), -4, 510, 620), Rgba.Hex("#C8B898"));
            r.Fill(Rotate(Rect(380, 590, 260, 60, 6), -4, 510, 620), Noise.Mottled(Rgba.Hex("#5A3E26", 0.5f), 30, 0.55f, 0.2f, 31));
            r.Fill(Rotate(Rect(506, 590, 8, 60), -4, 510, 620), Tarnish);
            CandleFlame(r, 760, 612, 1.4f);
            r.Fill(Rect(1500, 620, 110, 16, 6), Tarnish.Shade(-0.2f));
            r.Fill(Intersect(Circle(1555, 626, 42), Rect(1500, 560, 110, 66)), Tarnish.Shade(0.15f));
            Mist(r, 520, 760, 14, 0.25f);
            return r;
        }

        /// <summary>1920×1080 dawn: relief after the night, but the dead trees and mist remain.</summary>
        public static Raster Dawn()
        {
            var r = new Raster(1920, 1080);
            r.Fill(Rect(0, 0, 1920, 1080), (x, y) =>
            {
                float t = y / 1080f;
                return t < 0.55f
                    ? Rgba.Lerp(Rgba.Hex("#D99A58"), Rgba.Hex("#C89488"), t / 0.55f)
                    : Rgba.Lerp(Rgba.Hex("#C89488"), Rgba.Hex("#4E4670"), (t - 0.55f) / 0.45f);
            });
            r.Glow(Circle(960, 1000, 240), Rgba.Hex("#FFF0C8").WithAlpha(0.5f), 380);
            r.Fill(Circle(960, 1040, 210), Rgba.Hex("#F2DFA8"));
            var silhouette = Rgba.Hex("#2A2232");
            r.Fill(Union(Circle(200, 1300, 380), Circle(1760, 1320, 420)), silhouette);
            DeadTree(r, 160, 1000, 120, 88, 14, 6, new Random(12), silhouette);
            DeadTree(r, 1780, 1010, 130, 94, 15, 6, new Random(14), silhouette);
            Mist(r, 780, 1080, 21, 0.5f);
            return r;
        }

        /// <summary>1024×256 seamless-ish fog strip for drifting overlays.</summary>
        public static Raster FogStrip()
        {
            var r = new Raster(1024, 256);
            r.Fill(Rect(0, 0, 1024, 256), (x, y) =>
            {
                float band = (float)Math.Sin(y / 256f * Math.PI);
                float n = Noise.Fbm(x, y * 2f, 180, 5, 77);
                return new Rgba(0.85f, 0.88f, 0.94f, Clamp01((n - 0.3f) * 2.2f) * band * 0.75f);
            });
            return r;
        }

        /// <summary>512×512 vignette: clear centre, dark edges.</summary>
        public static Raster Vignette()
        {
            var r = new Raster(512, 512);
            r.Fill(Rect(0, 0, 512, 512), (x, y) =>
            {
                float dx = (x - 256) / 256f, dy = (y - 256) / 256f;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                return new Rgba(0.02f, 0.01f, 0.05f, Clamp01((d - 0.55f) / 0.6f) * 0.85f);
            });
            return r;
        }

        static void DrawGhostAt(Raster target, string type, int x, int y, float opacity)
        {
            var g = Ghost(type, "neutral");
            target.Draw(g, x, y, opacity + 0.35f);
        }
    }
}
