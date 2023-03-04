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
using Oasys.SDK.Tools;
using SharpDX;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Utilities
{
    internal sealed class AutoSmite
    {
        public static SpellClass SmiteKey;
        public static CastSlot SmiteSlot;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group AutoSmiteGroup => Tab.GetGroup("Auto Smite");

        private static bool LogSmiteAction
        {
            get => AutoSmiteGroup.GetItem<Switch>("Log Smite Action").IsOn;
            set => AutoSmiteGroup.GetItem<Switch>("Log Smite Action").IsOn = value;
        }

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
            Keyboard.OnKeyPress += OnKeyPress;
            CoreEvents.OnCoreRender += CoreEvents_OnCoreRender;

            Tab.AddGroup(new Group("Auto Smite"));
            AutoSmiteGroup.AddItem(new Switch() { Title = "Log Smite Action", IsOn = true });
            AutoSmiteGroup.AddItem(new KeyBinding("Enabled", Keys.U));
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

        private static void CoreEvents_OnCoreRender()
        {
            if (UseSmite)
            {
                var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                w2s.Y += 20;
                RenderFactory.DrawText($"Smite Enabled", 18, w2s, Color.Blue);
            }
        }

        private static void OnKeyPress(Keys keyBeingPressed, Keyboard.KeyPressState pressState)
        {
            var enabledKey = AutoSmiteGroup.GetItem<KeyBinding>("Enabled").SelectedKey;

            if (keyBeingPressed == enabledKey && pressState == Keyboard.KeyPressState.Down)
            {
                UseSmite = !UseSmite;
            }
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

        private static float _lastLog;
        private static Task InputHandler()
        {
            if (UseSmite && SmiteKey is not null)
            {
                var damage = 600f;
                var buffDamage = 0f;
                //var smiteDamageTrackerAvatarBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Contains("SmiteDamageTrackerAvatar", System.StringComparison.OrdinalIgnoreCase) && x.Stacks >= 0);
                var smiteBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Contains("itemsmitecounter", System.StringComparison.OrdinalIgnoreCase) && x.Stacks >= 0);
                if (smiteBuff is not null)
                {
                    if (smiteBuff.Stacks > 20)
                    {
                        buffDamage = 600f;
                    }
                    else if (smiteBuff.Stacks <= 20 && smiteBuff.Stacks > 0)
                    {
                        buffDamage = 900f;
                    }
                    else
                    {
                        buffDamage = 1200f;
                    }

                    if (buffDamage > damage)
                    {
                        damage = buffDamage;
                    }
                }

                if (SmiteKey.Damage > damage)
                {
                    damage = SmiteKey.Damage;
                }

                if (LogSmiteAction && EngineManager.GameTime >= _lastLog + 5)
                {
                    foreach (var item in UnitManager.MyChampion.BuffManager.ActiveBuffs.Where(x => x.Name.Contains("smite", System.StringComparison.OrdinalIgnoreCase) && x.Stacks >= 0))
                    {
                        Logger.Log($"Damage: {SmiteKey.Damage} - Buff: {item.Name}({item.Stacks}) - GameTime: {EngineManager.GameTime}");
                    }

                    Logger.Log($"SpellDamage: {SmiteKey.Damage} - BuffDamage: {buffDamage} - ActualDamage: {damage} - Smite Charges: {SmiteKey.Charges} - GameTime: {EngineManager.GameTime}");
                    _lastLog = EngineManager.GameTime;
                }

                if (SmiteKey.Charges > 0 && SmiteKey.IsSpellReady)
                {
                    var jungleTarget = GetJungleTarget(500f);
                    if (jungleTarget != null && jungleTarget.Health < damage)
                    {
                        if (LogSmiteAction)
                        {
                            Logger.Log($"Target: {jungleTarget.UnitComponentInfo.SkinName} {jungleTarget.Health}HP - Damage: {damage} - GameTime: {EngineManager.GameTime}");
                        }

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
                    enemy.Distance <= dist &&
                    !enemy.UnitComponentInfo.SkinName.Contains("mini", System.StringComparison.OrdinalIgnoreCase) &&
                    ((Baron && enemy.UnitComponentInfo.SkinName.Contains("SRU_Baron", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Dragon && enemy.UnitComponentInfo.SkinName.Contains("SRU_Dragon", System.StringComparison.OrdinalIgnoreCase)) ||
                    (RiftHerald && enemy.UnitComponentInfo.SkinName.Contains("SRU_RiftHerald", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Red && enemy.UnitComponentInfo.SkinName.Contains("SRU_Red", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Blue && enemy.UnitComponentInfo.SkinName.Contains("SRU_Blue", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Crab && enemy.UnitComponentInfo.SkinName.Contains("Sru_Crab", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Krug && enemy.UnitComponentInfo.SkinName.Contains("SRU_Krug", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Gromp && enemy.UnitComponentInfo.SkinName.Contains("SRU_Gromp", System.StringComparison.OrdinalIgnoreCase)) ||
                    (MurkWolf && enemy.UnitComponentInfo.SkinName.Equals("SRU_Murkwolf", System.StringComparison.OrdinalIgnoreCase)) ||
                    (Razorbeak && enemy.UnitComponentInfo.SkinName.Equals("SRU_Razorbeak", System.StringComparison.OrdinalIgnoreCase))))
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
