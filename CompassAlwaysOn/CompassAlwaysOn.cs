using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Modding;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;

namespace CompassAlwaysOn
{
    public class CompassAlwaysOn : Mod, ITogglableMod
    {
        internal static CompassAlwaysOn instance;
        public const string EnabledBool = "CompassAlwaysOn_enabled";


        private static ILHook _HKMPHook;
        public override void Initialize()
        {
            instance = this;

            IL.GameMap.PositionCompass += ModifyCompassBool;
            IL.GameMap.WorldMap += ModifyCompassBool;

            if (ModHooks.GetMod("HKMP") is Mod hkmp)
            {
                Log("Attempting to hook HKMP");
                Type HkmpMapManager = Type.GetType("Hkmp.Game.Client.MapManager, Hkmp");
                MethodInfo HkmpHeroUpdateMethod = HkmpMapManager?.GetMethod("HeroControllerOnUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                if (HkmpHeroUpdateMethod != null)
                {
                    Log("Hooking HKMP");
                    _HKMPHook = new ILHook(HkmpHeroUpdateMethod, ModifyCompassBool);
                }
            }

            ModHooks.GetPlayerBoolHook += InterpretCompassBool;
        }

        private bool InterpretCompassBool(string name, bool orig)
        {
            return orig || (name == EnabledBool);
        }

        // Easiest to simply force the relevant methods to request our own bool for compass
        private void ModifyCompassBool(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext
            (
                i => i.MatchLdstr("equippedCharm_2")
            ))
            {
                cursor.Remove();
                cursor.Emit(OpCodes.Ldstr, EnabledBool);
            }
        }

        public void Unload()
        {
            IL.GameMap.PositionCompass -= ModifyCompassBool;
            IL.GameMap.WorldMap -= ModifyCompassBool;
            _HKMPHook?.Dispose();

            ModHooks.GetPlayerBoolHook -= InterpretCompassBool;
        }

        public override string GetVersion()
        {
            return "1.0";
        }
    }
}
