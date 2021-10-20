using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Xayah : Champion
    {
        private enum Mode
        {
            Champs,
            Jungle,
            Everything,
        }

        private Mode _mode = Mode.Everything;
        private IEnumerable<AIBaseClient> _feathers = new List<AIBaseClient>();

        public Xayah()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                CastTime = 0.3f,
                Damage = (target, spellClass) =>
                            target != null
                            ? Helpers.DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            (25 + spellClass.Level * 25 +
                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.5f))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            _mode == Mode.Champs &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x) && x.Distance <= 1050 && x.IsAlive)
                            .OrderBy(x => x.Health)
                            .FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) => ShouldCastE(spellClass),
            };
        }

        private bool ShouldCastE(SpellClass spellClass)
        {
            if (!UseE)
            {
                return false;
            }

            if (spellClass.IsSpellReady && spellClass.Charges >= 1 && UnitManager.MyChampion.Mana > 30)
            {
                if (spellClass.Charges >= FeathersToHitChampions ||
                   spellClass.Charges >= FeathersToHitTargets ||
                    spellClass.Charges >= FeathersToHitEpicMonster)
                {
                    switch (_mode)
                    {
                        case Mode.Champs:
                            return UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x)).Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitChampions);
                        case Mode.Jungle:
                            return UnitManager.EnemyJungleMobs.Where(x => TargetSelector.IsAttackable(x)).Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitTargets);
                        case Mode.Everything:
                            return UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x)).Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitChampions) ||
                                   UnitManager.EnemyJungleMobs.Where(x => x.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                                                                          x.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                                                                          x.UnitComponentInfo.SkinName.ToLower().Contains("herald"))
                                                              .Where(x => TargetSelector.IsAttackable(x))
                                                              .Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitEpicMonster) ||
                                   UnitManager.EnemyJungleMobs.Where(x => TargetSelector.IsAttackable(x)).Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitTargets) ||
                                   UnitManager.EnemyMinions.Where(x => TargetSelector.IsAttackable(x)).Any(x => GetFeathersBetweenMeAndEnemy(x) >= FeathersToHitTargets);
                        default:
                            break;
                    }
                }
            }

            return false;
        }

        private int GetFeathersBetweenMeAndEnemy(AIBaseClient enemy)
        {
            return _feathers.Count(feather =>
                    Geometry.DistanceFromPointToLine(enemy.W2S, new Vector2[] { UnitManager.MyChampion.W2S, feather.W2S }) <= enemy.UnitComponentInfo.UnitBoundingRadius);
        }

        internal override void OnCoreMainInput()
        {
            _mode = Mode.Champs;
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private float _lastTick = 0;
        internal override void OnCoreMainTick()
        {
            if (GameEngine.GameTime > _lastTick + 0.1)
            {
                _feathers = UnitManager.AllObjects.Where(obj => obj.IsAlive && obj.OnMyTeam && obj.Name.Contains("Feather"));
                _lastTick = GameEngine.GameTime;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            _mode = Mode.Everything;
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreRender()
        {
            if (UnitManager.MyChampion.IsAlive && DrawFeathers && DrawThickness > 0 && UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Charges >= 1 && !UnitManager.MyChampion.W2S.IsZero)
            {
                var color = ColorConverter.GetColor(DrawColor);
                foreach (var feather in _feathers.Where(x => !x.W2S.IsZero && x.IsAlive))
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawLine(UnitManager.MyChampion.W2S.X, UnitManager.MyChampion.W2S.Y, feather.W2S.X, feather.W2S.Y, DrawThickness, color);
                }
            }
        }

        internal float GetEDamage(AIBaseClient enemy)
        {
            var feathers = GetFeathersBetweenMeAndEnemy(enemy);
            var armorMod = Helpers.DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var physicalDamage = armorMod * ((45 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10 + UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) * feathers);
            if (!enemy.IsObject(ObjectTypeFlag.AIHeroClient))
            {
                physicalDamage *= 0.5f;
            }
            return (float)physicalDamage;
        }

        private enum FeatherMode
        {
            //Kill,
            //Stun,
            //Damage,
            Stacks
        }

        private List<string> ConstructFeatherModeTable()
        {
            var keyTable = Enum.GetNames(typeof(FeatherMode)).ToList();
            return keyTable;
        }

        private bool DrawFeathers
        {
            get => MenuTab.GetItem<Switch>("Draw Feathers").IsOn;
            set => MenuTab.GetItem<Switch>("Draw Feathers").IsOn = value;
        }

        private int DrawThickness
        {
            get => MenuTab.GetItem<Counter>("Draw Thickness").Value;
            set => MenuTab.GetItem<Counter>("Draw Thickness").Value = value;
        }

        private string DrawColor
        {
            get => MenuTab.GetItem<ModeDisplay>("Draw Color").SelectedModeName;
            set => MenuTab.GetItem<ModeDisplay>("Draw Color").SelectedModeName = value;
        }

        private FeatherMode ChampionMode
        {
            get => (FeatherMode)Enum.Parse(typeof(FeatherMode), MenuTab.GetItem<ModeDisplay>("Champion Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Champion Mode").SelectedModeName = value.ToString();
        }

        private int FeathersToHitChampions
        {
            get => MenuTab.GetItem<Counter>("Feathers To Hit Champions").Value;
            set => MenuTab.GetItem<Counter>("Feathers To Hit Champions").Value = value;
        }

        private FeatherMode EpicMonsterMode
        {
            get => (FeatherMode)Enum.Parse(typeof(FeatherMode), MenuTab.GetItem<ModeDisplay>("Champions Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Champions Mode").SelectedModeName = value.ToString();
        }

        private int FeathersToHitEpicMonster
        {
            get => MenuTab.GetItem<Counter>("Feathers To Hit Champions").Value;
            set => MenuTab.GetItem<Counter>("Feathers To Hit Champions").Value = value;
        }

        private FeatherMode TargetsMode
        {
            get => (FeatherMode)Enum.Parse(typeof(FeatherMode), MenuTab.GetItem<ModeDisplay>("Targets Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Targets Mode").SelectedModeName = value.ToString();
        }

        private int FeathersToHitTargets
        {
            get => MenuTab.GetItem<Counter>("Feathers To Hit Targets").Value;
            set => MenuTab.GetItem<Counter>("Feathers To Hit Targets").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Xayah)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Draw Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Draw Feathers", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Draw Thickness", MinValue = 0, MaxValue = 250, Value = 5, ValueFrequency = 5 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Draw Color", ModeNames = ColorConverter.GetColors(), SelectedModeName = "Blue" });
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Champion Mode", ModeNames = ConstructFeatherModeTable(), SelectedModeName = "Stacks" });
            MenuTab.AddItem(new Counter() { Title = "Feathers To Hit Champions", MinValue = 1, MaxValue = 25, Value = 3, ValueFrequency = 1 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Epic Monster Mode", ModeNames = ConstructFeatherModeTable(), SelectedModeName = "Stacks" });
            MenuTab.AddItem(new Counter() { Title = "Feathers To Hit Epic Monster", MinValue = 1, MaxValue = 25, Value = 10, ValueFrequency = 1 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Targets Mode", ModeNames = ConstructFeatherModeTable(), SelectedModeName = "Stacks" });
            MenuTab.AddItem(new Counter() { Title = "Feathers To Hit Targets", MinValue = 1, MaxValue = 25, Value = 10, ValueFrequency = 1 });

        }
    }
}
