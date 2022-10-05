using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Logic;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SharpDX;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;
using Orbwalker = Oasys.SDK.Orbwalker;
using TargetSelector = Oasys.SDK.TargetSelector;

namespace SixAIO.Champions
{
    internal sealed class Vayne : Champion
    {
        public Vayne()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            !Orbwalker.CanBasicAttack &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCancelBasicAttack = () => EAllowCancelBasicAttack,
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => TargetSelectE()
            };
        }

        //private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        //{
        //    if (keyBeingPressed == Keys.T && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
        //    {
        //        SpellE.ExecuteCastSpell();
        //    }
        //}

        private GameObjectBase TargetSelectE()
        {
            var targets = UnitManager.EnemyChampions.Where(x => x.IsAlive &&
                                                                x.Distance <= 550 + x.UnitComponentInfo.UnitBoundingRadius + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                                                                TargetSelector.IsAttackable(x) &&
                                                                !TargetSelector.IsInvulnerable(x, DamageType.Magical, false))
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

        private int DistanceToWall(GameObjectBase target)
        {
            var enemyPos = EB.Prediction.Position.PredictUnitPosition(target, 250);
            var enemyPosW2s = enemyPos.To3DWorld().ToW2S();

            for (var i = 0; i < CondemnRange; i += 10)
            {
                if (UseAdvancedE)
                {
                    if (CheckPositions(enemyPos.To3DWorld(), target.Distance + i).Count(x => x.IsValid() && EngineManager.IsWall(x)) >= 3)
                    {
                        return i;
                    }
                }
                else
                {
                    var pos = UnitManager.MyChampion.Position.Extend(enemyPos.To3DWorld(), target.Distance + i);
                    if (pos.IsValid() && EngineManager.IsWall(pos))
                    {
                        return i;
                    }
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
            if (target != null)
            {
                SpellQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        private Vector3[] CheckPositions(Vector3 targetPos, float range)
        {
            var endDirection = UnitManager.MyChampion.Position + (targetPos - UnitManager.MyChampion.Position).Normalized();
            var extendedSpellEndPos = UnitManager.MyChampion.Position.Extend(endDirection, range);

            var result = new Vector3[] { targetPos, extendedSpellEndPos };

            var bSquared = Math.Pow(range, 2) + Math.Pow(range, 2) - (2 * range * range * Math.Cos(12));
            var b = Math.Sqrt(bSquared);

            if (!targetPos.IsZero && !extendedSpellEndPos.IsZero)
            {
                var sWidth = (float)(b / 1.5f);
                var posDir = new float[2] { (targetPos.X - extendedSpellEndPos.X), (targetPos.Z - extendedSpellEndPos.Z) };

                posDir[0] /= (float)range; //dirX  /= Dist
                posDir[1] /= (float)range; //dirY /= Dist

                var eLeft = new Vector3(extendedSpellEndPos.X + (sWidth / 2) * posDir[1], targetPos.Y, extendedSpellEndPos.Z - (sWidth / 2) * posDir[0]);
                var eRight = new Vector3(extendedSpellEndPos.X - (sWidth / 2) * posDir[1], targetPos.Y, extendedSpellEndPos.Z + (sWidth / 2) * posDir[0]);
                result = new Vector3[] { targetPos, eLeft, eRight, extendedSpellEndPos };
            }

            return result;
        }

        private static void DrawVaynePositions(Vector3 start, Vector3 eLeft, Vector3 eRight)
        {
            var startW2S = LeagueNativeRendererManager.WorldToScreenSpell(start);
            var eLeftW2S = LeagueNativeRendererManager.WorldToScreenSpell(eLeft);
            var eRightW2S = LeagueNativeRendererManager.WorldToScreenSpell(eRight);
            var leftColor = EngineManager.IsWall(eLeft) ? Color.Blue : Color.Red;
            var rightColor = EngineManager.IsWall(eRight) ? Color.Blue : Color.Red;
            Oasys.SDK.Rendering.RenderFactory.DrawLine(eLeftW2S.X, eLeftW2S.Y, eRightW2S.X, eRightW2S.Y, 3, Color.Blue); //Base end
            Oasys.SDK.Rendering.RenderFactory.DrawLine(startW2S.X, startW2S.Y, eLeftW2S.X, eLeftW2S.Y, 3, leftColor); // Left line
            Oasys.SDK.Rendering.RenderFactory.DrawLine(startW2S.X, startW2S.Y, eRightW2S.X, eRightW2S.Y, 3, rightColor); //Right line
        }

        internal override void OnCoreRender()
        {
            if (DrawE && SpellE.SpellClass.IsSpellReady)
            {
                foreach (var target in UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x) &&
                                                                         x.Distance <= 550 + x.UnitComponentInfo.UnitBoundingRadius + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                                                                         x.IsAlive))
                {
                    //for (var i = 0; i < CondemnRange; i += 5)
                    //{
                    //    var positionses = CheckPositions(target.Position, target.Distance + i);
                    //    DrawVaynePositions(positionses[0], positionses[1], positionses[2]);
                    //    //Logger.Log($"{i} = {EngineManager.IsWall(positionses[0])} - {EngineManager.IsWall(positionses[1])} - {EngineManager.IsWall(positionses[2])} - {EngineManager.IsWall(positionses[3])}");
                    //}
                    //var positions = CheckPositions(target.Position, target.Distance + CondemnRange);
                    //DrawVaynePositions(positions[0], positions[1], positions[2]);
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

        private bool UseAdvancedE
        {
            get => ESettings.GetItem<Switch>("Use Advanced E").IsOn;
            set => ESettings.GetItem<Switch>("Use Advanced E").IsOn = value;
        }

        private bool EAllowCancelBasicAttack
        {
            get => ESettings.GetItem<Switch>("E Allow Cancel Basic Attack").IsOn;
            set => ESettings.GetItem<Switch>("E Allow Cancel Basic Attack").IsOn = value;
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
            ESettings.AddItem(new Switch() { Title = "Use Advanced E", IsOn = false });
            ESettings.AddItem(new Switch() { Title = "E Allow Cancel Basic Attack", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Draw E", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "Condemn Range", MinValue = 50, MaxValue = 475, Value = 450, ValueFrequency = 25 });
            ESettings.AddItem(new Switch() { Title = "Use Push Away", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 550, Value = 150, ValueFrequency = 25 });
            ESettings.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Everything" });

        }
    }
}
