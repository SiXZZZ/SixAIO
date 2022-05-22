using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Vayne : Champion
    {
        private float _lastAATime = 0f;
        private float _lastQTime = 0f;
        private static GameObjectBase _eTarget;

        public Vayne()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            _lastAATime > _lastQTime &&
                            DashModeSelected == DashMode.ToMouse &&
                            !Orbwalker.CanBasicAttack &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                ShouldCast = ShouldCastE,
                TargetSelect = (mode) => _eTarget
            };
        }

        private GameObjectBase TargetSelectE()
        {
            var targets = UnitManager.EnemyChampions.Where(x => x.IsAlive &&
                                                                x.Distance <= 550 + x.UnitComponentInfo.UnitBoundingRadius + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                                                                TargetSelector.IsAttackable(x) &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                                    .OrderBy(x => x.Health);
            var target = targets.FirstOrDefault(CanStun);
            if (target != null)
            {
                return target;
            }

            if (UsePushAway)
            {
                return PushAwayModeSelected switch
                {
                    PushAwayMode.Melee => targets.FirstOrDefault(x => x.CombatType == CombatTypes.Melee && x.Distance < PushAwayRange),
                    PushAwayMode.LowerThanMyRange => targets.FirstOrDefault(x => x.AttackRange < UnitManager.MyChampion.AttackRange && x.Distance < PushAwayRange),
                    PushAwayMode.DashNearMe => targets.FirstOrDefault(x => x.AIManager.IsDashing && UnitManager.MyChampion.DistanceTo(x.AIManager.NavEndPosition) < PushAwayRange),
                    PushAwayMode.Everything => targets.FirstOrDefault(x => x.Distance < PushAwayRange),
                    _ => null,
                };
            }

            return null;
        }

        private bool ShouldCastE(Orbwalker.OrbWalkingModeType mode, GameObjectBase target, SpellClass spellClass, float damage)
        {
            return UseE && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 90 && target != null;
        }

        private int DistanceToWall(GameObjectBase target)
        {
            for (var i = 0; i < CondemnRange; i += 5)
            {
                var pos = UnitManager.MyChampion.Position.Extend(target.Position, target.Distance + i);
                if (pos.IsValid() && EngineManager.IsWall(pos))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private bool CanStun(GameObjectBase target)
        {
            var distance = DistanceToWall(target);
            return distance <= CondemnRange && distance > 0;
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            _lastAATime = gameTime;
            if (target != null)
            {
                SpellQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (UseE && SpellE.SpellClass.IsSpellReady && UnitManager.MyChampion.Mana > 90)
            {
                _eTarget = TargetSelectE();
            }
        }

        internal override void OnCoreRender()
        {
            if (DrawE && SpellE.SpellClass.IsSpellReady)
            {
                foreach (var target in UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x) &&
                                                                         x.Distance <= 550 + x.UnitComponentInfo.UnitBoundingRadius + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                                                                         x.IsAlive))
                {
                    var myPoint = UnitManager.MyChampion.W2S;
                    var targetPoint = UnitManager.MyChampion.Position.Extend(target.Position, target.Distance + CondemnRange);
                    var targetPointW2S = targetPoint.ToW2S();
                    if (targetPointW2S.IsValid() && myPoint.IsValid())
                    {
                        Oasys.SDK.Rendering.RenderFactory.DrawLine(target.W2S.X, target.W2S.Y, targetPointW2S.X, targetPointW2S.Y, 5, Color.Black);
                    }
                }
            }
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private bool DrawE
        {
            get => ESettings.GetItem<Switch>("Draw E").IsOn;
            set => ESettings.GetItem<Switch>("Draw E").IsOn = value;
        }

        private bool UsePushAway
        {
            get => ESettings.GetItem<Switch>("Use Push Away").IsOn;
            set => ESettings.GetItem<Switch>("Use Push Away").IsOn = value;
        }

        private int CondemnRange
        {
            get => ESettings.GetItem<Counter>("Condemn Range").Value;
            set => ESettings.GetItem<Counter>("Condemn Range").Value = value;
        }

        private int PushAwayRange
        {
            get => ESettings.GetItem<Counter>("Push Away Range").Value;
            set => ESettings.GetItem<Counter>("Push Away Range").Value = value;
        }

        private PushAwayMode PushAwayModeSelected
        {
            get => (PushAwayMode)Enum.Parse(typeof(PushAwayMode), ESettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Vayne)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = false });
            QSettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "Condemn Range", MinValue = 50, MaxValue = 475, Value = 450, ValueFrequency = 25 });
            ESettings.AddItem(new Switch() { Title = "Use Push Away", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 550, Value = 150, ValueFrequency = 25 });
            ESettings.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Everything" });

        }
    }
}
