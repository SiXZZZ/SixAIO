﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using SharpDX;
using System;

namespace SixAIO.Models
{
    public class Spell : SDKSpell
    {
        public Spell(CastSlot castSlot, SpellSlot spellSlot) : base(castSlot, spellSlot)
        {
        }

        public Func<bool> ShouldDraw { get; set; } = () => false;

        public Func<Color> DrawColor { get; set; } = () => Color.White;

        public void DrawRange()
        {
            if (UnitManager.MyChampion.IsAlive && ShouldDraw())
            {
                Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, Range(), DrawColor(), 1);
            }
        }
    }
}
