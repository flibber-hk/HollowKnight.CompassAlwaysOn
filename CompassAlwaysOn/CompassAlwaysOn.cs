using System;
using System.Collections.Generic;
using System.Reflection;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace CompassAlwaysOn
{
    public class CompassAlwaysOn : Mod, ITogglableMod
    {
        internal static CompassAlwaysOn instance;
        public const string EnabledBool = "CompassAlwaysOn.Enabled";


        private static readonly List<ILHook> ModInteropHooks = new List<ILHook>();

        public static readonly List<(string, string, string)> HookableModClasses = new List<(string, string, string)>()
        {
            // (Mod Name, Type Full Name, Method Name)
            ("HKMP", "Hkmp.Game.Client.MapManager, Hkmp", "HeroControllerOnUpdate"),
            ("Additional Maps", "AdditionalMaps.MonoBehaviours.GameMapHooks, AdditionalMaps", "NewPositionCompass"),
            ("Additional Maps", "AdditionalMaps.MonoBehaviours.GameMapHooks, AdditionalMaps", "<NewWorldMap>b__0") // TODO: find which method to hook at runtime
        };

        public override void Initialize()
        {
            instance = this;

            IL.GameMap.PositionCompass += ModifyCompassBool;
            IL.GameMap.WorldMap += ModifyCompassBool;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach ((string modName, string typeName, string methodName) in HookableModClasses)
            {
                if (ModHooks.GetMod(modName) is Mod _)
                {
                    Log("Attempting to hook " + modName);
                    Type type = Type.GetType(typeName);
                    MethodInfo method = type?.GetMethod(methodName, flags);
                    if (method != null)
                    {
                        Log($"Hooking {typeName}::{methodName}");
                        ModInteropHooks.Add(new ILHook(method, ModifyCompassBool));
                    }
                    else
                    {
                        if (type == null)
                        {
                            LogError($"Type not found: {typeName}");
                        }
                        else
                        {
                            LogError($"Method not found: {methodName}");
                        }
                    }
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
                i => i.MatchLdstr(nameof(PlayerData.equippedCharm_2)),
                i => i.MatchCallOrCallvirt<PlayerData>(nameof(PlayerData.GetBool))
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
            foreach (ILHook hook in ModInteropHooks)
            {
                hook?.Dispose();
            }
            ModInteropHooks.Clear();

            ModHooks.GetPlayerBoolHook -= InterpretCompassBool;
        }

        public override string GetVersion()
        {
            return "1.1";
        }
        public override int LoadPriority()
        {
            // Mods loading before this one can simply add tuples to HookableModClasses, if they want to
            return 1023;
        }
    }
}
