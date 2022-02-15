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

        internal Oasys.SDK.Prediction.MenuSelected.HitChance QHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), MenuTab.GetItem<ModeDisplay>("Q HitChance").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Q HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseW
        {
            get => MenuTab.GetItem<Switch>("Use W").IsOn;
            set => MenuTab.GetItem<Switch>("Use W").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance WHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), MenuTab.GetItem<ModeDisplay>("W HitChance").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("W HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseE
        {
            get => MenuTab.GetItem<Switch>("Use E").IsOn;
            set => MenuTab.GetItem<Switch>("Use E").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance EHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), MenuTab.GetItem<ModeDisplay>("E HitChance").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("E HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseR
        {
            get => MenuTab.GetItem<Switch>("Use R").IsOn;
            set => MenuTab.GetItem<Switch>("Use R").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance RHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), MenuTab.GetItem<ModeDisplay>("R HitChance").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("R HitChance").SelectedModeName = value.ToString();
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
