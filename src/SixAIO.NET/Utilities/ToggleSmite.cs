using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using SharpDX;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Utilities
{
    internal sealed class ToggleSmite
    {
        public static SpellClass SmiteKey;
        public static CastSlot SmiteSlot;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group ToggleSmiteGroup => Tab.GetGroup("Toggle Smite");

        private static bool UseSmite
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Use Smite").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Use Smite").IsOn = value;
        }

        private static bool Baron
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Baron").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Baron").IsOn = value;
        }

        private static bool Dragon
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Dragon").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Dragon").IsOn = value;
        }

        private static bool RiftHerald
        {
            get => ToggleSmiteGroup.GetItem<Switch>("RiftHerald").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("RiftHerald").IsOn = value;
        }

        private static bool Blue
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Blue").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Blue").IsOn = value;
        }

        private static bool Red
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Red").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Red").IsOn = value;
        }

        private static bool Razorbeak
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Razorbeak").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Razorbeak").IsOn = value;
        }

        private static bool MurkWolf
        {
            get => ToggleSmiteGroup.GetItem<Switch>("MurkWolf").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("MurkWolf").IsOn = value;
        }

        private static bool Gromp
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Gromp").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Gromp").IsOn = value;
        }

        private static bool Krug
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Krug").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Krug").IsOn = value;
        }

        private static bool Crab
        {
            get => ToggleSmiteGroup.GetItem<Switch>("Crab").IsOn;
            set => ToggleSmiteGroup.GetItem<Switch>("Crab").IsOn = value;
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
                return Task.CompletedTask;
            }
            Keyboard.OnKeyPress += OnKeyPress;
            CoreEvents.OnCoreRender += CoreEvents_OnCoreRender;

            Tab.AddGroup(new Group("Toggle Smite"));
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Use Smite", IsOn = false });
            ToggleSmiteGroup.AddItem(new KeyBinding("Enabled", Keys.U));
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Baron", IsOn = true });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Dragon", IsOn = true });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "RiftHerald", IsOn = true });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Blue", IsOn = true });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Red", IsOn = true });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Razorbeak", IsOn = false });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "MurkWolf", IsOn = false });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Gromp", IsOn = false });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Krug", IsOn = false });
            ToggleSmiteGroup.AddItem(new Switch() { Title = "Crab", IsOn = false });

            return Task.CompletedTask;
        }

        private static void CoreEvents_OnCoreRender()
        {
            if (UseSmite)
            {
                var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                w2s.Y += 20;
                RenderFactory.DrawText($"Toggle Smite Enabled", 18, w2s, Color.Blue);
            }
        }

        private static void OnKeyPress(Keys keyBeingPressed, Keyboard.KeyPressState pressState)
        {
            var enabledKey = ToggleSmiteGroup.GetItem<KeyBinding>("Enabled").SelectedKey;

            if (keyBeingPressed == enabledKey && pressState == Keyboard.KeyPressState.Down)
            {
                UseSmite = !UseSmite;
            }
        }

        internal static Task OnCoreMainTick()
        {
            if (UseSmite)
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
                var smiteDamageTrackerBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "itemsmitecounter" && x.Stacks >= 0);
                var smiteDamage = 600;
                if (smiteDamageTrackerBuff is not null)
                {
                    smiteDamage = smiteDamageTrackerBuff.Stacks switch
                    {
                        > 20 and <= 40 => 600,
                        > 0 and <= 20 => 900,
                        0 => 1200,
                        _ => 600
                    };
                }

                if (jungleTarget != null)
                {
                    if (jungleTarget.Health < smiteDamage)
                    {
                        var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                        OrbSettings.TargetChampionsOnly = false;
                        SpellCastProvider.CastSpell(SmiteSlot, jungleTarget.Position);
                        OrbSettings.TargetChampionsOnly = tempTargetChamps;
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
