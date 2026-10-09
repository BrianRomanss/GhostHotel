using System;
using System.Collections.Generic;
using System.IO;
using ChuchuGames.ProcArt;
using static ChuchuGames.ProcArt.Synth;

namespace GhostHotel.EditorTools.ArtGen
{
    /// <summary>
    /// Placeholder SFX for GDD §8, synthesized so the game has feedback sounds from day one.
    /// Replace with recorded/composed audio by keeping the file names.
    /// </summary>
    public static class GhostHotelAudio
    {
        /// <summary>Voice-blip pitch (Hz) and timbre per ghost type (GDD §8: "a short voice blip per line").</summary>
        static readonly Dictionary<string, (float hz, Wave wave)> Voices = new Dictionary<string, (float, Wave)>
        {
            ["Weeper"] = (330, Wave.Sine), ["Poltergeist"] = (520, Wave.Square), ["Victorian"] = (440, Wave.Triangle),
            ["Drowned"] = (196, Wave.Triangle), ["ChildGhost"] = (660, Wave.Sine), ["Banshee"] = (740, Wave.Saw),
            ["Wisp"] = (880, Wave.Sine), ["HeadlessKnight"] = (147, Wave.Saw), ["Bartholomew"] = (262, Wave.Triangle),
            ["Vane"] = (110, Wave.Saw), ["Edith"] = (392, Wave.Sine),
        };

        public static List<string> GenerateAll(string root, Action<string> log = null)
        {
            var files = new List<string>();
            void Save(string name, float[] samples)
            {
                var path = Path.Combine(root, name + ".wav");
                Wav.Write(path, Declick(Normalize(samples, 0.7f)));
                files.Add(path);
                log?.Invoke(name);
            }

            // Drag pick-up: paper whoosh (filtered noise sweeping up).
            Save("pickup", Env(LowPass(Noise(0.22f, 11), 800, 4000), 0.04f, 0.18f, 0f, 2f));
            // Valid drop: soft bell.
            Save("drop", Bell(0.6f, 880, 0.5f));
            // Invalid hover: low wooden creak.
            var creak = Tone(0.35f, 95, 70, Wave.Saw);
            for (int i = 0; i < creak.Length; i++) creak[i] *= 0.6f + 0.4f * (float)Math.Sin(i * 0.004);
            Save("creak", Env(LowPass(creak, 600), 0.02f, 0.3f, 0f, 1.5f));
            // Stars: rising chime, one per star (GDD: "pitch up per star").
            float[] starHz = { 1046.5f, 1318.5f, 1568f };
            for (int i = 0; i < 3; i++) Save($"star_{i + 1}", Bell(0.7f, starHz[i], 0.45f));
            // Guest moves on: choir "ahh" chord + sparkle.
            var choir = Mix(Pad(220f), Pad(277.2f), Pad(329.6f), Pad(440f));
            for (int i = 0; i < 6; i++) choir = At(choir, Bell(0.5f, 1568f * (1 + i * 0.12f), 0.15f), 0.3f + i * 0.15f);
            Save("move_on", choir);
            // Ledger page turn.
            Save("page", Env(LowPass(Noise(0.3f, 5), 3000, 900), 0.01f, 0.28f, 0f, 1.2f));
            // Midnight: single clock chime.
            Save("midnight", Mix(Bell(2.5f, 392f, 0.7f), Bell(2.5f, 196f, 0.4f)));
            // Dawn: warm swell.
            var swell = Mix(Tone(2.2f, 261.6f, -1, Wave.Triangle, 0.3f), Tone(2.2f, 329.6f, -1, Wave.Triangle, 0.25f),
                Tone(2.2f, 392f, -1, Wave.Triangle, 0.25f), Tone(2.2f, 523.3f, -1, Wave.Sine, 0.2f));
            Save("dawn", Env(LowPass(swell, 1500), 1.2f, 1f, 0f, 2f));
            // UI click.
            Save("click", Env(Tone(0.05f, 1800, 1200, Wave.Sine), 0.001f, 0.045f, 0f, 3f));
            // Voice blips.
            foreach (var kv in Voices)
                Save($"blip_{kv.Key}", Env(LowPass(Tone(0.07f, kv.Value.hz, kv.Value.hz * 1.15f, kv.Value.wave), 2500), 0.005f, 0.065f, 0f, 2f));
            // Check-in music: a slow music-box loop (GDD §8: "soft music box, low tempo so players can think").
            Save("music_checkin", MusicBox());
            // Lobby and menu: a cold, breathing drone (the hotel by day, empty and old).
            Save("music_lobby", LobbyDrone());
            return files;
        }

        static float[] Pad(float hz)
        {
            var a = Tone(1.6f, hz, -1, Wave.Saw, 0.25f);
            var b = Tone(1.6f, hz * 1.005f, -1, Wave.Saw, 0.25f); // detune for a soft chorus
            return Env(LowPass(Mix(a, b), 900), 0.25f, 1.35f, 0.0f, 1.2f);
        }

        /// <summary>
        /// 16-second check-in loop in A minor (i–VI–iv–V), on slightly detuned bells, like an old
        /// music box that has sat in an attic too long. Slow, so players can think (GDD §8).
        /// </summary>
        static float[] MusicBox()
        {
            float[][] chords =
            {
                new[] { 440f, 523.3f, 659.3f, 880f },    // Am
                new[] { 349.2f, 440f, 523.3f, 698.5f },  // F
                new[] { 293.7f, 349.2f, 440f, 587.3f },  // Dm
                new[] { 329.6f, 415.3f, 493.9f, 659.3f }, // E (G# gives the eerie harmonic-minor pull)
            };
            const float note = 0.25f;
            var song = Silence(16f);
            var rng = new Random(13);
            float t = 0;
            for (int bar = 0; bar < 4; bar++)
            {
                var c = chords[bar];
                int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 1, 0, 1, 2, 3, 2, 3, 2, 1 };
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (i % 4 == 3 && rng.NextDouble() < 0.35) { t += note; continue; } // missing teeth on the comb
                    float detune = 1f + (float)(rng.NextDouble() - 0.5) * 0.012f;
                    song = At(song, Bell(1.2f, c[pattern[i]] * detune, 0.2f), t);
                    t += note;
                }
                song = At(song, Bell(3f, c[0] / 2f, 0.16f), bar * 4f);
            }
            Array.Resize(ref song, (int)(16f * SampleRate)); // exact loop length
            return song;
        }

        /// <summary>
        /// 24-second lobby/menu loop: a low detuned drone that slowly breathes, with a few distant
        /// bell tones. Sparse and cold, so the hotel feels empty and old.
        /// </summary>
        static float[] LobbyDrone()
        {
            const float len = 24f;
            var a = Tone(len, 110f, -1, Wave.Saw, 0.2f);
            var b = Tone(len, 110.6f, -1, Wave.Saw, 0.2f);
            var c = Tone(len, 164.8f, -1, Wave.Triangle, 0.12f);   // fifth
            var d = Tone(len, 130.8f * 0.997f, -1, Wave.Triangle, 0.08f); // a sour minor third
            var drone = LowPass(Mix(a, b, c, d), 500);
            for (int i = 0; i < drone.Length; i++) // slow swell, period = loop length so it loops cleanly
                drone[i] *= 0.6f + 0.4f * (float)Math.Sin(i * 2 * Math.PI / drone.Length - Math.PI / 2) * 0.5f + 0.2f;
            float[] bells = { 880f, 659.3f, 830.6f, 587.3f };
            for (int i = 0; i < bells.Length; i++)
                drone = At(drone, LowPass(Bell(3.5f, bells[i], 0.12f), 2500), 2f + i * 5.5f);
            Array.Resize(ref drone, (int)(len * SampleRate));
            return drone;
        }
    }
}
