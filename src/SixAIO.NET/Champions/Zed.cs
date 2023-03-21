using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Zed : Champion
    {
        private Spell SpellQKill;
        private Spell SpellQShadow1Kill;
        private Spell SpellQShadow2Kill;

        private Spell SpellEKill;
        private Spell SpellEShadow1Kill;
        private Spell SpellEShadow2Kill;

        private Spell SpellQShadow1;
        private Spell SpellEShadow1;
        private GameObjectBase _shadow1;
        private float _shadow1SetGameTime;

        private Spell SpellQShadow2;
        private Spell SpellEShadow2;
        private GameObjectBase _shadow2;
        private float _shadow2SetGameTime;

        public Zed()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ,
                MinimumMana = () => 80f - SpellQ.SpellClass.Level * 5f,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellQShadow1 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                From = () => _shadow1.Position,
                Range = () => 900,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ && IsValidShadow(_shadow1),
                MinimumMana = () => 80f - SpellQShadow1.SpellClass.Level * 5f,
                TargetSelect = (mode) => SpellQShadow1.GetTargets(mode).FirstOrDefault()
            };
            SpellQShadow2 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                From = () => _shadow2.Position,
                Range = () => 900,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ && IsValidShadow(_shadow2),
                MinimumMana = () => 80f - SpellQShadow2.SpellClass.Level * 5f,
                TargetSelect = (mode) => SpellQShadow2.GetTargets(mode).FirstOrDefault()
            };
            SpellQKill = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ && AllowQIfCanKill,
                MinimumMana = () => 80f - SpellQ.SpellClass.Level * 5f,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => x.Health <= QDamage(x)).FirstOrDefault()
            };
            SpellQShadow1Kill = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                From = () => _shadow1.Position,
                Range = () => 900,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ && IsValidShadow(_shadow1),
                MinimumMana = () => 80f - SpellQShadow1.SpellClass.Level * 5f,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => x.Health <= QDamage(x)).FirstOrDefault()
            };
            SpellQShadow2Kill = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                From = () => _shadow2.Position,
                Range = () => 900,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ && IsValidShadow(_shadow2),
                MinimumMana = () => 80f - SpellQShadow2.SpellClass.Level * 5f,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => x.Health <= QDamage(x)).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsEnabled = () => UseE,
                Range = () => 300,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellEShadow1 = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && IsValidShadow(_shadow1),
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance(_shadow1.Position) <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellEShadow2 = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && IsValidShadow(_shadow2),
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance(_shadow2.Position) <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellEKill = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && AllowEIfCanKill,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Health <= EDamage(x) && x.Distance <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellEShadow1Kill = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && IsValidShadow(_shadow1) && AllowEIfCanKill,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Health <= EDamage(x) && x.Distance(_shadow1.Position) <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellEShadow2Kill = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && IsValidShadow(_shadow2) && AllowEIfCanKill,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Health <= EDamage(x) && x.Distance(_shadow2.Position) <= 300 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
        }

        private float QDamage(GameObjectBase target)
        {
            if (target is null)
            {
                return 0f;
            }

            var baseDamage = 35 + 35 * SpellQ.SpellClass.Level;
            var scaleDamage = 1.1f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            var totalDamage = baseDamage + scaleDamage;
            var actualDamage = DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, totalDamage);

            return actualDamage;
        }

        private float EDamage(GameObjectBase target)
        {
            if (target is null)
            {
                return 0f;
            }

            var baseDamage = 45 + 20 * SpellE.SpellClass.Level;
            var scaleDamage = 0.65f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            var totalDamage = baseDamage + scaleDamage;
            var actualDamage = DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, totalDamage);

            return actualDamage;
        }

        private void CastCombo()
        {
            var hittableECounter = new List<Spell>();
            if (SpellE.CanExecuteCastSpell())
            {
                hittableECounter.Add(SpellE);
            }
            if (SpellEShadow1.CanExecuteCastSpell())
            {
                hittableECounter.Add(SpellEShadow1);
            }
            if (SpellEShadow2.CanExecuteCastSpell())
            {
                hittableECounter.Add(SpellEShadow2);
            }

            var hittableQCounter = new List<Spell>();
            if (SpellQ.CanExecuteCastSpell())
            {
                hittableQCounter.Add(SpellQ);
            }
            if (SpellQShadow1.CanExecuteCastSpell())
            {
                hittableQCounter.Add(SpellQShadow1);
            }
            if (SpellQShadow2.CanExecuteCastSpell())
            {
                hittableQCounter.Add(SpellQShadow2);
            }

            if (hittableECounter.Count >= MinimumHittableE)
            {
                hittableECounter.ForEach(x => x.ExecuteCastSpell());
            }

            if (hittableQCounter.Count >= MinimumHittableQ)
            {
                hittableQCounter.ForEach(x => x.ExecuteCastSpell());
            }
            
            if (SpellEKill.ExecuteCastSpell() ||
                SpellEShadow1Kill.ExecuteCastSpell() ||
                SpellEShadow2Kill.ExecuteCastSpell())
            {
            }

            if (SpellQKill.ExecuteCastSpell() ||
                SpellQShadow1Kill.ExecuteCastSpell() ||
                SpellQShadow2Kill.ExecuteCastSpell())
            {
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            CastCombo();
        }

        internal override void OnCoreMainTick()
        {
            if (ComboOnTick)
            {
                CastCombo();
            }
        }

        private bool IsValidShadow(GameObjectBase obj)
        {
            return obj is not null &&
                   obj.IsAlive &&
                   obj.Mana == 200 &&
                   obj.Name == "Shadow" &&
                   obj.ModelName == "ZedShadow" &&
                   obj.UnitComponentInfo.SkinName == "zedshadow";
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsValidShadow(obj))
            {
                if (!IsValidShadow(_shadow1) ||
                    GameEngine.GameTime > _shadow1SetGameTime + 7.5f)
                {
                    _shadow1 = obj;
                    _shadow1SetGameTime = GameEngine.GameTime;
                }
                else if (!IsValidShadow(_shadow2) ||
                        GameEngine.GameTime > _shadow2SetGameTime + 7.5f)
                {
                    _shadow2 = obj;
                    _shadow2SetGameTime = GameEngine.GameTime;
                }
            }
        }

        private bool ComboOnTick
        {
            get => MenuTab.GetItem<Switch>("Combo On Tick").IsOn;
            set => MenuTab.GetItem<Switch>("Combo On Tick").IsOn = value;
        }

        private int MinimumHittableQ
        {
            get => QSettings.GetItem<Counter>("Minimum Hittable Q").Value;
            set => QSettings.GetItem<Counter>("Minimum Hittable Q").Value = value;
        }

        private bool AllowQIfCanKill
        {
            get => QSettings.GetItem<Switch>("Allow Q If Can Kill").IsOn;
            set => QSettings.GetItem<Switch>("Allow Q If Can Kill").IsOn = value;
        }

        private int MinimumHittableE
        {
            get => ESettings.GetItem<Counter>("Minimum Hittable E").Value;
            set => ESettings.GetItem<Counter>("Minimum Hittable E").Value = value;
        }

        private bool AllowEIfCanKill
        {
            get => ESettings.GetItem<Switch>("Allow E If Can Kill").IsOn;
            set => ESettings.GetItem<Switch>("Allow E If Can Kill").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Zed)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddItem(new Switch() { Title = "Combo On Tick", IsOn = true });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            QSettings.AddItem(new Counter() { Title = "Minimum Hittable Q", MinValue = 1, MaxValue = 3, Value = 2, ValueFrequency = 1 });
            QSettings.AddItem(new Switch() { Title = "Allow Q If Can Kill", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "Minimum Hittable E", MinValue = 1, MaxValue = 3, Value = 1, ValueFrequency = 1 });
            ESettings.AddItem(new Switch() { Title = "Allow E If Can Kill", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E);

        }
    }
}
