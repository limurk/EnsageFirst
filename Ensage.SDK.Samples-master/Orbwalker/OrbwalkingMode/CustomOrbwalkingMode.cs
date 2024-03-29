﻿// <copyright file="CustomOrbwalkingMode.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace OrbwalkingMode
{
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Ensage;
    using Ensage.SDK.Abilities;
    using Ensage.SDK.Abilities.npc_dota_hero_skywrath_mage;
    using Ensage.SDK.Extensions;
    using Ensage.SDK.Helpers;
    using Ensage.SDK.Orbwalker.Metadata;
    using Ensage.SDK.Orbwalker.Modes;
    using Ensage.SDK.Service;

    using log4net;

    using PlaySharp.Toolkit.Logging;

    [ExportOrbwalkingMode] // note that its not a plugin
    internal class CustomOrbwalkingMode : OrbwalkingModeAsync // or just OrbwalkingMode
    {
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly AbilityFactory abilityFactory;

        private readonly Hero hero;

        private skywrath_mage_arcane_bolt arcanebolt;

        private Settings settings;

        [ImportingConstructor]
        public CustomOrbwalkingMode(IServiceContext context)
            : base(context)
        {
            // custom orbwalking mode to farm creeps with zeus arc lightning

            this.hero = context.Owner as Hero;
            this.abilityFactory = context.AbilityFactory;
        }

        public override bool CanExecute
        {
            get
            {
                if (this.settings == null)
                {
                    return false;
                }

                return this.settings.Active && this.settings.Key;
            }
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            if (this.arcanebolt.CanBeCasted)
            {
                var creep = EntityManager<Creep>.Entities
                    .Where(x => x.IsValid && x.IsAlive && x.IsSpawned && x.IsEnemy(this.Owner) && this.arcanebolt.CanHit(x))
                    .OrderBy(x => x.Health)
                    .FirstOrDefault(x => this.arcanebolt.GetDamage(x) >= x.Health);

                if (creep != null)
                {
                    Log.Warn("Killing " + Game.Localize(creep.Name) + " with " + creep.Health + " health");
                    this.arcanebolt.UseAbility(creep);
                    await Task.Delay(this.arcanebolt.GetCastDelay(creep), token);
                }
            }

            // no auto attacks
            // just move to mouse position
            this.Orbwalker.OrbwalkTo(null);
        }

        protected override void OnActivate()
        {
            if (this.hero?.HeroId != HeroId.npc_dota_hero_skywrath_mage)
            {
                // only for zeus
                return;
            }

            this.settings = new Settings(this.Orbwalker.Settings.Factory.Parent);
            this.arcanebolt = this.abilityFactory.GetAbility<skywrath_mage_arcane_bolt>();
        }

        protected override void OnDeactivate()
        {
            this.settings?.Dispose();
        }
    }
}
