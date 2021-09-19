using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal class AutoSmite
    {
        public static SpellClass SmiteKey;
        public static CastSlot SmiteSlot;

        private static Tab _menuTab => MenuManagerProvider.GetTab($"SIXAIO - Auto Smite");

        private static bool UseSmite
        {
            get => _menuTab.GetItem<Switch>("Use Smite").IsOn;
            set => _menuTab.GetItem<Switch>("Use Smite").IsOn = value;
        }

        private static bool SmiteOnLaneclear
        {
            get => _menuTab?.GetItem<Switch>("Smite On Laneclear")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Smite On Laneclear").IsOn = value;
        }

        private static bool SmiteOnLastHit
        {
            get => _menuTab?.GetItem<Switch>("Smite On LastHit")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Smite On LastHit").IsOn = value;
        }

        private static bool SmiteOnTick
        {
            get => _menuTab?.GetItem<Switch>("Smite On Tick")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Smite On Tick").IsOn = value;
        }

        private static bool Baron
        {
            get => _menuTab.GetItem<Switch>("Baron").IsOn;
            set => _menuTab.GetItem<Switch>("Baron").IsOn = value;
        }

        private static bool Dragon
        {
            get => _menuTab.GetItem<Switch>("Dragon").IsOn;
            set => _menuTab.GetItem<Switch>("Dragon").IsOn = value;
        }

        private static bool RiftHerald
        {
            get => _menuTab.GetItem<Switch>("RiftHerald").IsOn;
            set => _menuTab.GetItem<Switch>("RiftHerald").IsOn = value;
        }

        private static bool Blue
        {
            get => _menuTab.GetItem<Switch>("Blue").IsOn;
            set => _menuTab.GetItem<Switch>("Blue").IsOn = value;
        }

        private static bool Red
        {
            get => _menuTab.GetItem<Switch>("Red").IsOn;
            set => _menuTab.GetItem<Switch>("Red").IsOn = value;
        }

        private static bool Razorbeak
        {
            get => _menuTab.GetItem<Switch>("Razorbeak").IsOn;
            set => _menuTab.GetItem<Switch>("Razorbeak").IsOn = value;
        }

        private static bool MurkWolf
        {
            get => _menuTab.GetItem<Switch>("MurkWolf").IsOn;
            set => _menuTab.GetItem<Switch>("MurkWolf").IsOn = value;
        }

        private static bool Gromp
        {
            get => _menuTab.GetItem<Switch>("Gromp").IsOn;
            set => _menuTab.GetItem<Switch>("Gromp").IsOn = value;
        }

        private static bool Krug
        {
            get => _menuTab.GetItem<Switch>("Krug").IsOn;
            set => _menuTab.GetItem<Switch>("Krug").IsOn = value;
        }

        private static bool Crab
        {
            get => _menuTab.GetItem<Switch>("Crab").IsOn;
            set => _menuTab.GetItem<Switch>("Crab").IsOn = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            var spellBook = UnitManager.MyChampion.GetSpellBook();
            if (spellBook.GetSpellClass(SpellSlot.Summoner1).SpellData.CastRange == 500.0)
            {
                SmiteKey = spellBook.GetSpellClass(SpellSlot.Summoner1);
                SmiteSlot = CastSlot.Summoner1;
            }
            else if (spellBook.GetSpellClass(SpellSlot.Summoner2).SpellData.CastRange == 500.0)
            {
                SmiteKey = spellBook.GetSpellClass((SpellSlot)5);
                SmiteSlot = CastSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainTick -= InputHandler;
                CoreEvents.OnCoreLaneclearInputAsync -= InputHandler;
                CoreEvents.OnCoreLasthitInputAsync -= InputHandler;
                return Task.CompletedTask;
            }

            MenuManager.AddTab(new Tab($"SIXAIO - Auto Smite"));
            _menuTab.AddItem(new Switch() { Title = "Use Smite", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Smite On Laneclear", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Smite On LastHit", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Smite On Tick", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Baron", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Dragon", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "RiftHerald", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Blue", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Red", IsOn = true });
            _menuTab.AddItem(new Switch() { Title = "Razorbeak", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "MurkWolf", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Gromp", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Krug", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Crab", IsOn = false });

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
            foreach (var enemy in UnitManager.Enemies)
            {
                if (enemy.IsJungle && enemy.IsAlive &&
                    enemy.DistanceTo(UnitManager.MyChampion.Position) <= dist &&
                    ((Baron && enemy.UnitComponentInfo.SkinName.Contains("SRU_Baron")) ||
                    (Dragon && enemy.UnitComponentInfo.SkinName.StartsWith("SRU_Dragon")) ||
                    (RiftHerald && enemy.UnitComponentInfo.SkinName.Contains("SRU_RiftHerald")) ||
                    (Red && enemy.UnitComponentInfo.SkinName.Contains("SRU_Red")) ||
                    (Blue && enemy.UnitComponentInfo.SkinName.Contains("SRU_Blue")) ||
                    (Crab && enemy.UnitComponentInfo.SkinName.Contains("Sru_Crab")) ||
                    (Krug && enemy.UnitComponentInfo.SkinName.Contains("SRU_Krug")) ||
                    (Gromp && enemy.UnitComponentInfo.SkinName.Contains("SRU_Gromp")) ||
                    (MurkWolf && enemy.UnitComponentInfo.SkinName.Contains("SRU_MurkWolf")) ||
                    (Razorbeak && enemy.UnitComponentInfo.SkinName.Contains("SRU_Razorbeak"))))
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
