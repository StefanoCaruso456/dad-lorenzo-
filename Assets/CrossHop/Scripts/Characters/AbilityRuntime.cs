using CrossHop.Gameplay;

namespace CrossHop.Characters
{
    /// <summary>
    /// The per-run instance of an ability. This is where mutable state lives (charges
    /// used, timers, …) — never on the <see cref="CharacterAbility"/> asset. Created by
    /// <see cref="CharacterAbility.CreateRuntime"/> at run start and discarded on run end.
    ///
    /// Every hook is a no-op by default, so a runtime overrides only what it needs.
    /// </summary>
    public abstract class AbilityRuntime
    {
        /// <summary>Handles for the current run. Set by <see cref="OnRunStart"/>.</summary>
        protected AbilityContext Context { get; private set; }

        /// <summary>Called once as the run begins.</summary>
        public virtual void OnRunStart(AbilityContext context) => Context = context;

        /// <summary>Called once as the run ends (death or restart).</summary>
        public virtual void OnRunEnd() { }

        /// <summary>Called every frame while playing (magnet pulls, timers, …).</summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>Transform the coin reward banked at the end of the run.</summary>
        public virtual int ModifyCoinReward(int baseAmount) => baseAmount;

        /// <summary>
        /// Return true to absorb an otherwise-fatal hit (e.g. Fireproof). The player
        /// survives with a brief invulnerability window; consume any charge here.
        /// </summary>
        public virtual bool TryAbsorbDeath(DeathCause cause) => false;

        /// <summary>Adjust the hop feel for this run (e.g. Low-G floatier hops).</summary>
        public virtual void ModifyHopProfile(ref HopProfile profile) { }
    }
}
