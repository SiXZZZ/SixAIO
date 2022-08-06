using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Logic;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Models;
using System.Collections.Generic;
using System.Linq;
using Group = Oasys.Common.Menu.Group;
using Orbwalker = Oasys.SDK.Orbwalker;
using TargetSelector = Oasys.SDK.TargetSelector;

namespace SixAIO.Champions
{
    internal class Fiora : Champion
    {
        private static List<AIBaseClient> FioraPassives = new List<AIBaseClient>();
        private static List<AIBaseClient> FioraActivePassives => FioraPassives.Where(IsFioraPassive).ToList();

        public static Vector3 Passivepos(GameObjectBase target)
        {
            var passive = FioraActivePassives.Where(x => x.Position.Distance(target.Position) <= 50).FirstOrDefault();
            var position = target.Position;
            if (passive == null)
            {
                return Vector3.Zero;
            }

            if (passive.Name.Contains("NE"))
            {
                var pos = new Vector2
                {
                    X = position.To2D().X,
                    Y = position.To2D().Y + 150
                };
                return pos.To3D();
            }
            if (passive.Name.Contains("SE"))
            {
                var pos = new Vector2
                {
                    X = position.To2D().X - 150,
                    Y = position.To2D().Y
                };
                return pos.To3D();
            }
            if (passive.Name.Contains("NW"))
            {
                var pos = new Vector2
                {
                    X = position.To2D().X + 150,
                    Y = position.To2D().Y
                };
                return pos.To3D();
            }
            if (passive.Name.Contains("SW"))
            {
                var pos = new Vector2
                {
                    X = position.To2D().X,
                    Y = position.To2D().Y - 150
                };
                return pos.To3D();
            }

            return Vector3.Zero;
        }

        public static bool HasPassive(GameObjectBase target)
        {
            return FioraActivePassives.Any(x => x.Position.Distance(target.Position) <= 50);
        }

        public Fiora()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellE.ExecuteCastSpell();
        }

        internal override void OnCoreMainInput()
        {
            var target = UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x) && HasPassive(x)).OrderBy(x => x.Health).FirstOrDefault();
            if (target != null)
            {
                SpellCastProvider.CastSpell(CastSlot.Q, Passivepos(target));
            }
        }

        internal override void OnCoreMainTick()
        {
            foreach (var item in FioraPassives.Where(x => !IsFioraPassive(x)))
            {
                FioraPassives.Remove(item);
            }
        }

        private static bool IsFioraPassive(GameObjectBase obj)
        {
            var name = obj.Name;
            return name.Contains("Fiora") &&
                   (name.Contains("Passive") || name.Contains("R_Mark") || name.Contains("_R")) &&
                   (name.Contains("NE") || name.Contains("SE") || name.Contains("NW") || name.Contains("SW"));
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsFioraPassive(obj))
            {
                FioraPassives.Add(obj);
                //Logger.Log($"Added: {obj.Name}");
            }
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            if (IsFioraPassive(obj))
            {
                FioraPassives.Remove(obj);
                //Logger.Log($"Removed: {obj.Name}");
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Fiora)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

        }
    }
}
