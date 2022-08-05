using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK.Tools;
using System;
using System.Windows.Forms;

namespace SixAIO.Models
{
    internal abstract class Champion
    {
        internal Spell SpellQ;
        internal Spell SpellQSemiAuto;
        internal Spell SpellW;
        internal Spell SpellWSemiAuto;
        internal Spell SpellE;
        internal Spell SpellESemiAuto;
        internal Spell SpellR;
        internal Spell SpellRSemiAuto;

        internal Tab MenuTab => MenuManagerProvider.GetTab($"SIXAIO - {GetType().Name}");
        internal Group BasicAttackSettings => MenuTab.GetGroup("Basic Attack Settings");
        internal Group QSettings => MenuTab.GetGroup("Q Settings");
        internal Group WSettings => MenuTab.GetGroup("W Settings");
        internal Group ESettings => MenuTab.GetGroup("E Settings");
        internal Group RSettings => MenuTab.GetGroup("R Settings");

        internal bool UseQ
        {
            get => QSettings.GetItem<Switch>("Use Q").IsOn;
            set => QSettings.GetItem<Switch>("Use Q").IsOn = value;
        }

        internal bool UseQLaneclear
        {
            get => QSettings.GetItem<Switch>("Use Q Laneclear").IsOn;
            set => QSettings.GetItem<Switch>("Use Q Laneclear").IsOn = value;
        }

        internal bool UseQLasthit
        {
            get => QSettings.GetItem<Switch>("Use Q Lasthit").IsOn;
            set => QSettings.GetItem<Switch>("Use Q Lasthit").IsOn = value;
        }

        internal bool UseQHarass
        {
            get => QSettings.GetItem<Switch>("Use Q Harass").IsOn;
            set => QSettings.GetItem<Switch>("Use Q Harass").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance QHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), QSettings.GetItem<ModeDisplay>("Q HitChance").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Q HitChance").SelectedModeName = value.ToString();
        }

        internal bool AllowQCastOnMinimap
        {
            get => QSettings.GetItem<Switch>("Allow Q cast on minimap").IsOn;
            set => QSettings.GetItem<Switch>("Allow Q cast on minimap").IsOn = value;
        }


        internal int QMinMana
        {
            get => QSettings.GetItem<Counter>("Q Min Mana").Value;
            set => QSettings.GetItem<Counter>("Q Min Mana").Value = value;
        }
        internal bool UseSemiAutoQ
        {
            get => QSettings.GetItem<Switch>("Use Semi Auto Q").IsOn;
            set => QSettings.GetItem<Switch>("Use Semi Auto Q").IsOn = value;
        }

        public Keys SemiAutoQKey => QSettings.GetItem<KeyBinding>("Semi Auto Q Key").SelectedKey;

        internal Oasys.SDK.Prediction.MenuSelected.HitChance SemiAutoQHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), QSettings.GetItem<ModeDisplay>("Semi Auto Q HitChance").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Semi Auto Q HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseW
        {
            get => WSettings.GetItem<Switch>("Use W").IsOn;
            set => WSettings.GetItem<Switch>("Use W").IsOn = value;
        }

        internal bool UseWLaneclear
        {
            get => WSettings.GetItem<Switch>("Use W Laneclear").IsOn;
            set => WSettings.GetItem<Switch>("Use W Laneclear").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance WHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), WSettings.GetItem<ModeDisplay>("W HitChance").SelectedModeName);
            set => WSettings.GetItem<ModeDisplay>("W HitChance").SelectedModeName = value.ToString();
        }

        internal bool AllowWCastOnMinimap
        {
            get => WSettings.GetItem<Switch>("Allow W cast on minimap").IsOn;
            set => WSettings.GetItem<Switch>("Allow W cast on minimap").IsOn = value;
        }

        internal int WMinMana
        {
            get => WSettings.GetItem<Counter>("W Min Mana").Value;
            set => WSettings.GetItem<Counter>("W Min Mana").Value = value;
        }
        internal bool UseSemiAutoW
        {
            get => WSettings.GetItem<Switch>("Use Semi Auto W").IsOn;
            set => WSettings.GetItem<Switch>("Use Semi Auto W").IsOn = value;
        }

        public Keys SemiAutoWKey => WSettings.GetItem<KeyBinding>("Semi Auto W Key").SelectedKey;

        internal Oasys.SDK.Prediction.MenuSelected.HitChance SemiAutoWHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), WSettings.GetItem<ModeDisplay>("Semi Auto W HitChance").SelectedModeName);
            set => WSettings.GetItem<ModeDisplay>("Semi Auto W HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseE
        {
            get => ESettings.GetItem<Switch>("Use E").IsOn;
            set => ESettings.GetItem<Switch>("Use E").IsOn = value;
        }

        internal bool UseELaneclear
        {
            get => ESettings.GetItem<Switch>("Use E Laneclear").IsOn;
            set => ESettings.GetItem<Switch>("Use E Laneclear").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance EHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), ESettings.GetItem<ModeDisplay>("E HitChance").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("E HitChance").SelectedModeName = value.ToString();
        }

        internal bool AllowECastOnMinimap
        {
            get => ESettings.GetItem<Switch>("Allow E cast on minimap").IsOn;
            set => ESettings.GetItem<Switch>("Allow E cast on minimap").IsOn = value;
        }

        internal int EMinMana
        {
            get => ESettings.GetItem<Counter>("E Min Mana").Value;
            set => ESettings.GetItem<Counter>("E Min Mana").Value = value;
        }
        internal bool UseSemiAutoE
        {
            get => ESettings.GetItem<Switch>("Use Semi Auto E").IsOn;
            set => ESettings.GetItem<Switch>("Use Semi Auto E").IsOn = value;
        }

        public Keys SemiAutoEKey => ESettings.GetItem<KeyBinding>("Semi Auto E Key").SelectedKey;

        internal Oasys.SDK.Prediction.MenuSelected.HitChance SemiAutoEHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), ESettings.GetItem<ModeDisplay>("Semi Auto E HitChance").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Semi Auto E HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseR
        {
            get => RSettings.GetItem<Switch>("Use R").IsOn;
            set => RSettings.GetItem<Switch>("Use R").IsOn = value;
        }

        internal bool UseRLaneclear
        {
            get => RSettings.GetItem<Switch>("Use R Laneclear").IsOn;
            set => RSettings.GetItem<Switch>("Use R Laneclear").IsOn = value;
        }

        internal Oasys.SDK.Prediction.MenuSelected.HitChance RHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), RSettings.GetItem<ModeDisplay>("R HitChance").SelectedModeName);
            set => RSettings.GetItem<ModeDisplay>("R HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseSemiAutoR
        {
            get => RSettings.GetItem<Switch>("Use Semi Auto R").IsOn;
            set => RSettings.GetItem<Switch>("Use Semi Auto R").IsOn = value;
        }

        public Keys SemiAutoRKey => RSettings.GetItem<KeyBinding>("Semi Auto R Key").SelectedKey;

        internal Oasys.SDK.Prediction.MenuSelected.HitChance SemiAutoRHitChance
        {
            get => (Oasys.SDK.Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Oasys.SDK.Prediction.MenuSelected.HitChance), RSettings.GetItem<ModeDisplay>("Semi Auto R HitChance").SelectedModeName);
            set => RSettings.GetItem<ModeDisplay>("Semi Auto R HitChance").SelectedModeName = value.ToString();
        }

        internal bool AllowRCastOnMinimap
        {
            get => RSettings.GetItem<Switch>("Allow R cast on minimap").IsOn;
            set => RSettings.GetItem<Switch>("Allow R cast on minimap").IsOn = value;
        }

        internal int RMinMana
        {
            get => RSettings.GetItem<Counter>("R Min Mana").Value;
            set => RSettings.GetItem<Counter>("R Min Mana").Value = value;
        }

        internal static Champion GetChampion(string champion)
        {
            Logger.Log($"Creating: {champion}");
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
