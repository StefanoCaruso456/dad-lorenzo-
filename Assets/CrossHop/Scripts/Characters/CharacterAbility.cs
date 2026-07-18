using UnityEngine;

namespace CrossHop.Characters
{
    /// <summary>
    /// A character's optional gameplay twist, authored as a read-only asset. Because a
    /// ScriptableObject is a single shared instance, it must hold <b>no per-run state</b>
    /// — it is pure configuration plus a factory. Call <see cref="CreateRuntime"/> at
    /// run start to get a fresh <see cref="AbilityRuntime"/> that owns the mutable state
    /// and is discarded when the run ends.
    /// </summary>
    public abstract class CharacterAbility : ScriptableObject
    {
        [Tooltip("Short player-facing description, e.g. 'Earns double coins'.")]
        [TextArea] public string description;

        /// <summary>Create a fresh, run-scoped runtime for this ability.</summary>
        public abstract AbilityRuntime CreateRuntime();
    }
}
