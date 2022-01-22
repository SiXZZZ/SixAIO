using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using System;

namespace SixAIO.Models
{
    internal abstract class Champion
    {
        internal Spell SpellQ;
        internal Spell SpellW;
        internal Spell SpellE;
        internal Spell SpellR;

        internal Tab MenuTab => MenuManagerProvider.GetTab($"SIXAIO - {GetType().Name}");

        internal bool UseQ
        {
            get => MenuTab.GetItem<Switch>("Use Q").IsOn;
            set => MenuTab.GetItem<Switch>("Use Q").IsOn = value;
        }
        internal bool UseW
        {
            get => MenuTab.GetItem<Switch>("Use W").IsOn;
            set => MenuTab.GetItem<Switch>("Use W").IsOn = value;
        }

        internal bool UseE
        {
            get => MenuTab.GetItem<Switch>("Use E").IsOn;
            set => MenuTab.GetItem<Switch>("Use E").IsOn = value;
        }

        internal bool UseR
        {
            get => MenuTab.GetItem<Switch>("Use R").IsOn;
            set => MenuTab.GetItem<Switch>("Use R").IsOn = value;
        }

        internal static Champion GetChampion(string champion)
        {
            var type = Type.GetType($"SixAIO.Champions.{champion}");
            return (Champion)Activator.CreateInstance(type);
        }

        internal virtual void OnCoreMainInput() { }

        internal virtual void OnCoreHarassInput() { }

        internal virtual void OnCoreMainTick() { }

        internal virtual void OnCoreMainInputRelease() { }

        internal virtual void OnCoreLaneClearInput() { }

        internal virtual void OnCoreLastHitInput() { }

        internal virtual void OnCoreRender() { }

        internal virtual void OnCreateObject(AIBaseClient obj) { }

        internal virtual void OnDeleteObject(AIBaseClient obj) { }

        internal virtual void InitializeMenu() { }

        internal virtual void OnGameMatchComplete() { }
    }
}
