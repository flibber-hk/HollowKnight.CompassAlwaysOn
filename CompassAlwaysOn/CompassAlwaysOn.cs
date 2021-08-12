using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace CompassAlwaysOn
{
    public class CompassAlwaysOn : Mod, ITogglableMod
    {
        internal static CompassAlwaysOn instance;
        public const string EnabledBool = "CompassAlwaysOn_enabled";

        public override void Initialize()
        {
            instance = this;

            IL.GameMap.PositionCompass += ModifyCompassBool;
            IL.GameMap.WorldMap += ModifyCompassBool;

            ModHooks.Instance.GetPlayerBoolHook += InterpretCompassBool;
        }

        private bool InterpretCompassBool(string originalSet)
        {
            if (originalSet != EnabledBool) return PlayerData.instance.GetBoolInternal(originalSet);

            return true;
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

            ModHooks.Instance.GetPlayerBoolHook -= InterpretCompassBool;
        }

        public override string GetVersion()
        {
            return "0.5";
        }
    }
}
