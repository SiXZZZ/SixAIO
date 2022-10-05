using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AutoSmite
    {
        public static SpellClass SmiteKey;
        public static CastSlot SmiteSlot;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group AutoSmiteGroup => Tab.GetGroup("Auto Smite");

        private static bool UseSmite
        {
            get => AutoSmiteGroup.GetItem<Switch>("Use Smite").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Use Smite").IsOn = value;
        }

        private static bool SmiteOnLaneclear
        {
            get => AutoSmiteGroup?.GetItem<Switch>("Smite On Laneclear")?.IsOn ?? false;
            set => AutoSmiteGroup.GetItem<Switch>("Smite On Laneclear").IsOn = value;
        }

        private static bool SmiteOnLastHit
        {
            get => AutoSmiteGroup?.GetItem<Switch>("Smite On LastHit")?.IsOn ?? false;
            set => AutoSmiteGroup.GetItem<Switch>("Smite On LastHit").IsOn = value;
        }

        private static bool SmiteOnTick
        {
            get => AutoSmiteGroup?.GetItem<Switch>("Smite On Tick")?.IsOn ?? false;
            set => AutoSmiteGroup.GetItem<Switch>("Smite On Tick").IsOn = value;
        }

        private static bool Baron
        {
            get => AutoSmiteGroup.GetItem<Switch>("Baron").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Baron").IsOn = value;
        }

        private static bool Dragon
        {
            get => AutoSmiteGroup.GetItem<Switch>("Dragon").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Dragon").IsOn = value;
        }

        private static bool RiftHerald
        {
            get => AutoSmiteGroup.GetItem<Switch>("RiftHerald").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("RiftHerald").IsOn = value;
        }

        private static bool Blue
        {
            get => AutoSmiteGroup.GetItem<Switch>("Blue").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Blue").IsOn = value;
        }

        private static bool Red
        {
            get => AutoSmiteGroup.GetItem<Switch>("Red").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Red").IsOn = value;
        }

        private static bool Razorbeak
        {
            get => AutoSmiteGroup.GetItem<Switch>("Razorbeak").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Razorbeak").IsOn = value;
        }

        private static bool MurkWolf
        {
            get => AutoSmiteGroup.GetItem<Switch>("MurkWolf").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("MurkWolf").IsOn = value;
        }

        private static bool Gromp
        {
            get => AutoSmiteGroup.GetItem<Switch>("Gromp").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Gromp").IsOn = value;
        }

        private static bool Krug
        {
            get => AutoSmiteGroup.GetItem<Switch>("Krug").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Krug").IsOn = value;
        }

        private static bool Crab
        {
            get => AutoSmiteGroup.GetItem<Switch>("Crab").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Crab").IsOn = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            var spellBook = UnitManager.MyChampion.GetSpellBook();
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Smite, SummonerSpellSlot.First))
            {
                SmiteKey = spellBook.GetSpellClass(SpellSlot.Summoner1);
                SmiteSlot = CastSlot.Summoner1;
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Smite, SummonerSpellSlot.Second))
            {
                SmiteKey = spellBook.GetSpellClass(SpellSlot.Summoner2);
                SmiteSlot = CastSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainTick -= OnCoreMainTick;
                CoreEvents.OnCoreLaneclearInputAsync -= OnCoreLaneclearInputAsync;
                CoreEvents.OnCoreLasthitInputAsync -= OnCoreLasthitInputAsync;
                return Task.CompletedTask;
            }

            Tab.AddGroup(new Group("Auto Smite"));
            AutoSmiteGroup.AddItem(new Switch() { Title = "Use Smite", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Smite On Laneclear", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Smite On LastHit", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Smite On Tick", IsOn = false });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Baron", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Dragon", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "RiftHerald", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Blue", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Red", IsOn = true });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Razorbeak", IsOn = false });
            AutoSmiteGroup.AddItem(new Switch() { Title = "MurkWolf", IsOn = false });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Gromp", IsOn = false });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Krug", IsOn = false });
            AutoSmiteGroup.AddItem(new Switch() { Title = "Crab", IsOn = false });

            return Task.CompletedTask;
        }

        internal static Task OnCoreMainTick()
        {
            if (SmiteOnTick)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        internal static Task OnCoreLaneclearInputAsync()
        {
            if (SmiteOnLaneclear)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        internal static Task OnCoreLasthitInputAsync()
        {
            if (SmiteOnLastHit)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            GameObjectBase jungleTarget;
            if (UseSmite && SmiteKey?.Charges > 0)
            {
                jungleTarget = GetJungleTarget(500f);
                var smiteDamage = 900f;
                if (jungleTarget != null)
                {
                    foreach (var buffEntry in UnitManager.MyChampion.BuffManager.GetBuffList())
                    {
                        if (!(buffEntry.Name != "SmiteDamageTracker"))
                        {
                            smiteDamage = 450f;
                            break;
                        }
                    }

                    if (jungleTarget.Health < smiteDamage)
                    {
                        SpellCastProvider.CastSpell(SmiteSlot, jungleTarget.Position);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public static GameObjectBase GetJungleTarget(float dist)
        {
            foreach (var enemy in UnitManager.EnemyJungleMobs)
            {
                if (enemy.IsJungle && enemy.IsAlive &&
                    enemy.DistanceTo(UnitManager.MyChampion.Position) <= dist &&
                    ((Baron && enemy.UnitComponentInfo.SkinName.Contains("SRU_Baron")) ||
                    (Dragon && enemy.UnitComponentInfo.SkinName.Contains("SRU_Dragon")) ||
                    (RiftHerald && enemy.UnitComponentInfo.SkinName.Contains("SRU_RiftHerald")) ||
                    (Red && enemy.UnitComponentInfo.SkinName.Contains("SRU_Red")) ||
                    (Blue && enemy.UnitComponentInfo.SkinName.Contains("SRU_Blue")) ||
                    (Crab && enemy.UnitComponentInfo.SkinName.Contains("Sru_Crab")) ||
                    (Krug && enemy.UnitComponentInfo.SkinName.Contains("SRU_Krug")) ||
                    (Gromp && enemy.UnitComponentInfo.SkinName.Contains("SRU_Gromp")) ||
                    (MurkWolf && enemy.UnitComponentInfo.SkinName.Equals("SRU_Murkwolf")) ||
                    (Razorbeak && enemy.UnitComponentInfo.SkinName.Equals("SRU_Razorbeak"))))
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
