using System.Collections.Generic;
using GhostHotel.Model;
using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>
    /// Every sprite, sound and font the code-built UI uses, looked up by file name (e.g.
    /// "ghost_Drowned_happy", "tag_Water", "star_2"). Filled by Tools/Ghost Hotel/Generate Placeholder Art,
    /// so swapping in final art is just replacing files with the same names.
    /// </summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Art Library", fileName = "ArtLibrary")]
    public sealed class ArtLibrarySO : ScriptableObject
    {
        public Font serif;
        public Font sans;
        public Sprite[] sprites = new Sprite[0];
        public AudioClip[] clips = new AudioClip[0];

        Dictionary<string, Sprite> _sprites;
        Dictionary<string, AudioClip> _clips;

        public Sprite Sprite(string name)
        {
            if (_sprites == null)
            {
                _sprites = new Dictionary<string, Sprite>();
                foreach (var s in sprites) if (s != null) _sprites[s.name] = s;
            }
            return _sprites.TryGetValue(name, out var sprite) ? sprite : null;
        }

        public AudioClip Clip(string name)
        {
            if (_clips == null)
            {
                _clips = new Dictionary<string, AudioClip>();
                foreach (var c in clips) if (c != null) _clips[c.name] = c;
            }
            return _clips.TryGetValue(name, out var clip) ? clip : null;
        }

        /// <param name="expression">"neutral", "happy" or "sad".</param>
        public Sprite Ghost(GhostType type, string expression = "neutral") => Sprite($"ghost_{type}_{expression}");
        public Sprite Portrait(GhostType type, string expression = "neutral") => Sprite($"portrait_{type}_{expression}");
        public Sprite TagIcon(string tag) => Sprite($"tag_{tag}");
        public AudioClip Voice(GhostType type) => Clip($"blip_{type}");

        void OnValidate()
        {
            _sprites = null;
            _clips = null;
        }
    }
}
