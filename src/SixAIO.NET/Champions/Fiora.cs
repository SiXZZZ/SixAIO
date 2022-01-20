using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    //internal class Fiora : Champion
    //{
    //    private static BuffEntry GetUltBuff() => UnitManager.MyChampion.BuffManager.GetBuffByName("rivenwindslashready", false, true);

    //    private static bool IsUltActive()
    //    {
    //        var buff = GetUltBuff();
    //        return buff != null && buff.IsActive;
    //    }

    //    private static float UltTimeLeft()
    //    {
    //        var buff = GetUltBuff();
    //        return buff != null && buff.IsActive ? buff.EndTime - GameEngine.GameTime : 0;
    //    }

    //    private int _lastQCharge = -1;
    //    private float _lastQChargeTime = 0;
    //    private float _lastAATime = 0f;
    //    private float _lastQTime = 0f;

    //    public Fiora()
    //    {
    //        Spell.OnSpellCast += Spell_OnSpellCast;
    //        Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
    //        SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
    //        {
    //            CastTime = () => 0f,
    //            ShouldCast = (target, spellClass, damage) =>
    //                        UseQ &&
    //                        spellClass.IsSpellReady &&
    //                        _lastAATime > _lastQTime + 0.333f &&
    //                        _lastAATime > _lastQChargeTime + 0.333f &&
    //                        target != null,
    //            TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange + 300 && TargetSelector.IsAttackable(x))
    //        };
    //        SpellE = new Spell(CastSlot.E, SpellSlot.E)
    //        {
    //            CastTime = () => 0f,
    //            ShouldCast = (target, spellClass, damage) =>
    //                        UseE &&
    //                        spellClass.IsSpellReady &&
    //                        !Orbwalker.CanBasicAttack &&
    //                        target != null,
    //            TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange + 50 && TargetSelector.IsAttackable(x))
    //        };
    //        SpellR = new Spell(CastSlot.R, SpellSlot.R)
    //        {
    //            ShouldCast = (target, spellClass, damage) =>
    //                        UseR &&
    //                        spellClass.IsSpellReady &&
    //                        target != null &&
    //                        (target.Health < GetRDamage(target, spellClass) ||
    //                        (UltTimeLeft() > 0 && UltTimeLeft() < 1f) ||
    //                        (GetMissingHealthPercent(target) < 75.0f)),
    //            TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 800 && TargetSelector.IsAttackable(x) &&
    //                                                                                x.Health < GetRDamage(x, UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R)))
    //        };
    //    }

    //    private float GetMissingHealthPercent(GameObjectBase target)
    //    {
    //        var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
    //        return missingHealthPercent;
    //    }

    //    private float GetRDamage(GameObjectBase target, SpellClass spellClass)
    //    {
    //        if (target == null)
    //        {
    //            return 0;
    //        }
    //        var extraDamagePercent = GetMissingHealthPercent(target) * 2.667f;
    //        if (extraDamagePercent > 200f)
    //        {
    //            extraDamagePercent = 200f;
    //        }
    //        return DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * ((1 + (extraDamagePercent / 100f)) *
    //               ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) + 50 + 50 * spellClass.Level));
    //    }

    //    private void Spell_OnSpellCast(Spell spell)
    //    {
    //        if (spell.SpellSlot == SpellSlot.Q)
    //        {
    //            _lastQTime = GameEngine.GameTime;
    //        }

    //        if (spell.SpellSlot == SpellSlot.E)
    //        {
    //            SpellQ.ExecuteCastSpell();
    //            SpellR.ExecuteCastSpell();
    //        }

    //        if (spell.SpellSlot == SpellSlot.Q)
    //        {
    //            SpellR.ExecuteCastSpell();
    //        }
    //    }

    //    private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
    //    {
    //        _lastAATime = gameTime;
    //        if (target != null)
    //        {
    //            SpellQ.ExecuteCastSpell();
    //        }
    //    }

    //    internal override void OnCoreMainInput()
    //    {
    //        if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
    //        {
    //            return;
    //        }
    //    }

    //    internal override void OnCoreMainTick()
    //    {
    //        if (_lastQCharge != UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Charges)
    //        {
    //            _lastQCharge = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Charges;
    //            _lastQChargeTime = GameEngine.GameTime;
    //        }
    //    }

    //    internal override void InitializeMenu()
    //    {
    //        MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Fiora)}"));
    //        MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
    //        MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
    //        MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
    //    }
    //}
}