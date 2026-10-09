using System.Collections.Generic;

namespace ChuchuGames.GridPuzzle
{
    /// <summary>
    /// Tags on one cell, split into a permanent base layer and a temporary modifier layer
    /// (auras, events). Effective tags = (base ∪ added) − removed, so removals always win.
    /// Call <see cref="ClearModifiers"/> before re-applying modifiers after a change.
    /// </summary>
    public sealed class TagLayers
    {
        readonly HashSet<string> _base = new HashSet<string>();
        readonly HashSet<string> _added = new HashSet<string>();
        readonly HashSet<string> _removed = new HashSet<string>();

        public IReadOnlyCollection<string> Base => _base;
        public IReadOnlyCollection<string> Added => _added;
        public IReadOnlyCollection<string> Removed => _removed;

        public TagLayers() { }

        public TagLayers(IEnumerable<string> baseTags)
        {
            foreach (var t in baseTags) _base.Add(t);
        }

        public bool AddBase(string tag) => _base.Add(tag);
        public bool RemoveBase(string tag) => _base.Remove(tag);

        public void AddModifier(string tag) => _added.Add(tag);
        public void RemoveModifier(string tag) => _removed.Add(tag);

        public void ClearModifiers()
        {
            _added.Clear();
            _removed.Clear();
        }

        public bool Has(string tag) => !_removed.Contains(tag) && (_base.Contains(tag) || _added.Contains(tag));

        /// <summary>True when the tag is present only because of a modifier (shown with a clock badge in UI).</summary>
        public bool IsTemporary(string tag) => Has(tag) && !_base.Contains(tag);

        public IEnumerable<string> Effective()
        {
            foreach (var t in _base)
                if (!_removed.Contains(t)) yield return t;
            foreach (var t in _added)
                if (!_base.Contains(t) && !_removed.Contains(t)) yield return t;
        }
    }
}
