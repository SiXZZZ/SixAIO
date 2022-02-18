using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 30 &&
                            _lastAATime > _lastQTime &&
                            DashModeSelected == DashMode.ToMouse &&
                            !Orbwalker.CanBasicAttack &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                ShouldCast = ShouldCastE,
                TargetSelect = (mode) => _eTarget
            };
        }

        private GameObjectBase TargetSelectE()
        {
            var targets = UnitManager.EnemyChampions.Where(x => x.IsAlive &&
                                                                x.Distance <= 550 + x.UnitComponentInfo.UnitBoundingRadius + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                                                                TargetSelector.IsAttackable(x) &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
                                                    .OrderBy(x => x.Health);
            var target = targets.FirstOrDefault(x => CanStun(x));
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

        private bool ShouldCastE(GameObjectBase target, SpellClass spellClass, float damage)
        {
            return UseE && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 90 && target != null;
        }

        private int DistanceToWall(GameObjectBase target)
        {
            for (var i = 0; i < CondemnRange; i += 5)
            {
                if (EngineManager.IsWall(UnitManager.MyChampion.Position.Extend(target.Position, target.Distance + i)))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private bool CanStun(GameObjectBase target)
        {
            var distance = DistanceToWall(target);
            return distance < int.MaxValue && distance > 0;
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
            //foreach (var target in UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x) &&
            //                                                             x.Distance <= 550 + x.UnitComponentInfo.UnitBoundingRadius + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
            //                                                             x.IsAlive &&
            //                                                             CanStun(x)))
            //{
            //    var myPoint = UnitManager.MyChampion.W2S;
            //    var targetPoint = UnitManager.MyChampion.W2S.Extend(target.W2S, target.Distance + DistanceToWall(target));
            //    Oasys.SDK.Rendering.RenderFactory.DrawText("Can stun " + target.UnitComponentInfo.SkinName, 12, target.W2S, Color.Blue);
            //    Oasys.SDK.Rendering.RenderFactory.DrawLine(myPoint.X, myPoint.Y, targetPoint.X, targetPoint.Y, 5, Color.Black);
            //}
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), MenuTab.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private bool UsePushAway
        {
            get => MenuTab.GetItem<Switch>("Use Push Away").IsOn;
            set => MenuTab.GetItem<Switch>("Use Push Away").IsOn = value;
        }

        private int CondemnRange
        {
            get => MenuTab.GetItem<Counter>("Condemn Range").Value;
            set => MenuTab.GetItem<Counter>("Condemn Range").Value = value;
        }

        private int PushAwayRange
        {
            get => MenuTab.GetItem<Counter>("Push Away Range").Value;
            set => MenuTab.GetItem<Counter>("Push Away Range").Value = value;
        }

        private PushAwayMode PushAwayModeSelected
        {
            get => (PushAwayMode)Enum.Parse(typeof(PushAwayMode), MenuTab.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Vayne)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = false });
            MenuTab.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Condemn Range", MinValue = 50, MaxValue = 475, Value = 450, ValueFrequency = 5 });
            MenuTab.AddItem(new Switch() { Title = "Use Push Away", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 550, Value = 150, ValueFrequency = 5 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Everything" });

        }
    }
}
