using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Extensions;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Champions
{
    internal sealed class Kindred : Champion
    {
        private static TargetSelection _targetSelection;

        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredmarkofthekindredstackcounter", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        private static bool HasQBuff()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredqasbuff", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private static bool HasWBuff()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("kindredwclonebuffvisible", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        public Kindred()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.Combo ||
                        mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        if (!ShouldQ() ||
                            DashModeSelected != DashMode.ToMouse)
                        {
                            return false;
                        }

                        return UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 850 && TargetSelector.IsAttackable(x));
                    }

                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        if (DashModeSelected != DashMode.ToMouse)
                        {
                            return false;
                        }

                        return UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 650 && TargetSelector.IsAttackable(x)) ||
                               UnitManager.EnemyMinions.Any(x => x.IsAlive && x.Distance <= 650 && TargetSelector.IsAttackable(x)) ||
                               UnitManager.EnemyJungleMobs.Any(x => x.IsAlive && x.Distance <= 650 && TargetSelector.IsAttackable(x));
                    }

                    return false;
                },
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (HasWBuff())
                    {
                        return false;
                    }

                    if (mode == Orbwalker.OrbWalkingModeType.Combo ||
                        mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        return UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && TargetSelector.IsAttackable(x));
                    }

                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        return UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 650 && TargetSelector.IsAttackable(x)) ||
                               UnitManager.EnemyJungleMobs.Any(x => x.IsAlive && x.Distance <= 650 && TargetSelector.IsAttackable(x));
                    }

                    return false;
                },
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                Range = () => ERange(),
                ShouldCast = (mode, target, spellClass, damage) => target is not null && target.Distance <= ERange(),
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var heroTarget = TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                                         Orbwalker.TargetHero.Distance <= ERange() &&
                                         Orbwalker.TargetHero.HealthPercent > ETargetMinimumHealthPercent
                                            ? Orbwalker.TargetHero
                                            : UnitManager.EnemyChampions.FirstOrDefault(x =>
                                                            x.IsAlive && x.Distance <= ERange() &&
                                                            TargetSelector.IsAttackable(x) &&
                                                            x.HealthPercent > ETargetMinimumHealthPercent &&
                                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false));
                        if (heroTarget is null)
                        {
                            return GetJungleTarget(ERange(), x => x.HealthPercent > ETargetMinimumHealthPercent);
                        }
                    }

                    return TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                           Orbwalker.TargetHero.Distance <= ERange() &&
                           Orbwalker.TargetHero.HealthPercent > ETargetMinimumHealthPercent
                                                ? Orbwalker.TargetHero
                                                : UnitManager.EnemyChampions.FirstOrDefault(x =>
                                                                x.IsAlive && x.Distance <= ERange() &&
                                                                TargetSelector.IsAttackable(x) &&
                                                                x.HealthPercent > ETargetMinimumHealthPercent &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false));
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                ShouldCast = (mode, _, spellClass, _) =>
                {
                    return UnitManager.AllyChampions
                            .Where(x => !x.IsTargetDummy)
                            .Any(ally =>
                                    ally.HealthPercent < (RSettings.GetItem<Counter>(ally.ModelName)?.Value ?? 0) &&
                                    ally.IsAlive &&
                                    ally.Distance <= 550 &&
                                    TargetSelector.IsAttackable(ally, false) &&
                                        (UnitManager.EnemyChampions.Any(enemy =>
                                            enemy.IsAlive &&
                                            enemy.Distance <= 3000 &&
                                            enemy.IsCastingSpell &&
                                            IsCastingSpellOnAlly(enemy, ally)) ||
                                        UnitManager.EnemyTowers.Any(enemy =>
                                            enemy.IsAlive &&
                                            enemy.Distance <= 3000 &&
                                            enemy.IsCastingSpell &&
                                            IsCastingSpellOnAlly(enemy, ally))));
                },
            };
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            var toggleRCombo = RSettings.GetItem<KeyBinding>("R Toggle Combo").SelectedKey;

            if (keyBeingPressed == toggleRCombo && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                UseR = !UseR;
            }
        }

        private bool IsCastingSpellOnAlly(GameObjectBase enemy, GameObjectBase ally)
        {
            var spell = enemy.GetCurrentCastingSpell();
            return spell is not null && spell.TargetIndexes.Any(targetIndex => ally.Index == targetIndex);
        }

        private bool ShouldQ() => Qonlyifhaswbuff ? HasWBuff() : true;

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell != null)
            {
                if (spell.SpellSlot == SpellSlot.E)
                {
                    SpellW.ExecuteCastSpell();
                }
                if (spell.SpellSlot == SpellSlot.W)
                {
                    SpellQ.ExecuteCastSpell();
                }
            }
        }

        private static int ERange() => PassiveStacks() switch
        {
            < 4 => 500,
            < 7 => 575,
            < 10 => 600,
            < 13 => 625,
            < 16 => 650,
            < 19 => 675,
            < 22 => 700,
            < 25 => 725,
            >= 25 => 750
        };

        internal override void OnCoreRender()
        {
            SpellE.DrawRange();

            if (UseR)
            {
                var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                w2s.Y += 40;
                RenderFactory.DrawText($"Toggle R Enabled", 18, w2s, Color.Blue);
            }
        }

        internal override void OnCoreMainInput()
        {
            SpellR.ExecuteCastSpell();

            if (SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if ((UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)) ||
                (UseWLaneclear && SpellW.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)) ||
                (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)))
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            SmiteHandler();
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private bool Qonlyifhaswbuff
        {
            get => QSettings.GetItem<Switch>("Q only if has w buff").IsOn;
            set => QSettings.GetItem<Switch>("Q only if has w buff").IsOn = value;
        }

        private bool SmiteMarkedJungleCamp
        {
            get => MenuTab.GetItem<Switch>("Smite Marked Jungle Camp").IsOn;
            set => MenuTab.GetItem<Switch>("Smite Marked Jungle Camp").IsOn = value;
        }

        internal int ETargetMinimumHealthPercent
        {
            get => ESettings.GetItem<Counter>("E Target Minimum Health Percent").Value;
            set => ESettings.GetItem<Counter>("E Target Minimum Health Percent").Value = value;
        }

        public SpellClass SmiteKey { get; private set; }
        public CastSlot SmiteSlot { get; private set; }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kindred)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            LoadSmiteSettings();

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });
            QSettings.AddItem(new Switch() { Title = "Q only if has w buff", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Use W Laneclear", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Target Minimum Health Percent", Value = 15, MinValue = 0, MaxValue = 100, ValueFrequency = 1 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new KeyBinding("R Toggle Combo", Keys.T));
            RSettings.AddItem(new InfoDisplay() { Title = "-Use R if health percent is lower than-" });
            foreach (var ally in UnitManager.AllyChampions)
            {
                RSettings.AddItem(new Counter() { Title = ally.ModelName, MinValue = 0, MaxValue = 100, Value = 10, ValueFrequency = 1 });
            }


            MenuTab.AddDrawOptions(SpellSlot.E);
        }

        private void LoadSmiteSettings()
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
                return;
            }

            MenuTab.AddItem(new Switch() { Title = "Smite Marked Jungle Camp", IsOn = true });
        }

        private void SmiteHandler()
        {
            if (SmiteMarkedJungleCamp && SmiteKey is not null)
            {
                var damage = 600f;
                var buffDamage = 600f;
                var smiteBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Contains("itemsmitecounter", StringComparison.OrdinalIgnoreCase) && x.Stacks >= 0);
                if (smiteBuff is not null)
                {
                    if (smiteBuff.Stacks > 20 && smiteBuff.Stacks <= 40)
                    {
                        buffDamage = 600f;
                    }
                    else if (smiteBuff.Stacks <= 20 && smiteBuff.Stacks > 0)
                    {
                        buffDamage = 900f;
                    }
                    else if (smiteBuff.Stacks == 0)
                    {
                        buffDamage = 1200f;
                    }

                    if (buffDamage > damage)
                    {
                        damage = buffDamage;
                    }
                }

                var smiteDamageTrackerAvatarBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Contains("SmiteDamageTrackerAvatar", StringComparison.OrdinalIgnoreCase) && x.Stacks >= 1);
                if (smiteDamageTrackerAvatarBuff is not null)
                {
                    if (smiteDamageTrackerAvatarBuff.Stacks == 1 ||
                        smiteDamageTrackerAvatarBuff.Stacks == 1200)
                    {
                        buffDamage = 1200f;
                    }

                    if (buffDamage > damage)
                    {
                        damage = buffDamage;
                    }
                }

                var smiteDamageTrackerStalkerBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Contains("SmiteDamageTrackerStalker", StringComparison.OrdinalIgnoreCase) && x.Stacks >= 1);
                if (smiteDamageTrackerStalkerBuff is not null)
                {
                    if (smiteDamageTrackerStalkerBuff.Stacks >= 600 && smiteDamageTrackerStalkerBuff.Stacks <= 900)
                    {
                        buffDamage = smiteDamageTrackerStalkerBuff.Stacks;
                    }

                    if (buffDamage > damage)
                    {
                        damage = buffDamage;
                    }
                }

                var itemDamage = 600f;
                var smiteItem = UnitManager.MyChampion.Inventory.GetItemList().FirstOrDefault(x => x.ID == ItemID.Mosstomper_Seedling || x.ID == ItemID.Scorchclaw_Pup || x.ID == ItemID.Gustwalker_Hatchling);
                if (smiteItem is not null)
                {
                    if (smiteItem.Charges > 20 && smiteItem.Charges <= 40)
                    {
                        itemDamage = 600f;
                    }
                    else if (smiteItem.Charges <= 20 && smiteItem.Charges > 0)
                    {
                        itemDamage = 900f;
                    }
                    else if (smiteItem.Charges == 0)
                    {
                        itemDamage = 1200f;
                    }

                    if (itemDamage > damage)
                    {
                        damage = itemDamage;
                    }
                }

                if (SmiteKey.Damage > damage)
                {
                    damage = SmiteKey.Damage;
                }

                if (SmiteKey.Charges > 0 && SmiteKey.IsSpellReady)
                {
                    var jungleTarget = GetJungleTarget(500f, enemy => enemy.BuffManager.ActiveBuffs.Any(x => x.Name == "kindredhittracker" && x.Stacks >= 1));
                    if (jungleTarget != null && jungleTarget.Health < damage)
                    {
                        var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                        OrbSettings.TargetChampionsOnly = false;
                        SpellCastProvider.CastSpell(SmiteSlot, jungleTarget.Position);
                        OrbSettings.TargetChampionsOnly = tempTargetChamps;
                    }
                }
            }
        }

        public GameObjectBase GetJungleTarget(float dist, Func<GameObjectBase, bool> predicate)
        {
            foreach (var enemy in UnitManager.EnemyJungleMobs)
            {
                if (enemy.IsJungle && enemy.IsAlive &&
                    enemy.Distance <= dist &&
                    predicate(enemy) &&
                    !enemy.UnitComponentInfo.SkinName.Contains("mini", StringComparison.OrdinalIgnoreCase) &&
                    ((enemy.UnitComponentInfo.SkinName.Contains("SRU_Baron", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Dragon", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_RiftHerald", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Red", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Blue", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("Sru_Crab", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Krug", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Contains("SRU_Gromp", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Equals("SRU_Murkwolf", StringComparison.OrdinalIgnoreCase)) ||
                    (enemy.UnitComponentInfo.SkinName.Equals("SRU_Razorbeak", StringComparison.OrdinalIgnoreCase))))
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
