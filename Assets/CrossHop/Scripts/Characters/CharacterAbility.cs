using UnityEngine;

namespace CrossHop.Characters
{
    /// <summary>
    /// Base type for a character's optional gameplay twist. Abilities are authored
    /// assets so designers can attach one to a character without writing code.
    /// Keep effects small, passive and readable — the core game must stay fair.
    /// </summary>
    public abstract class CharacterAbility : ScriptableObject
    {
        [Tooltip("Short player-facing description, e.g. 'Earns double coins'.")]
        [TextArea] public string description;

        /// <summary>Called once when the character carrying this ability spawns for a run.</summary>
        public virtual void OnRunStart(AbilityContext context) { }

        /// <summary>Called when the run ends (death or restart). Undo any run-scoped state.</summary>
        public virtual void OnRunEnd(AbilityContext context) { }

        /// <summary>Hook to transform coins collected during the run (e.g. double them).</summary>
        public virtual int ModifyCoinReward(int baseAmount) => baseAmount;
    }
}
