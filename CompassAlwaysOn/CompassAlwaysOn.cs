using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Modding;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace CompassAlwaysOn
{
    public class CompassAlwaysOn : Mod, ITogglableMod
    {
        internal static CompassAlwaysOn instance;
        public const string EnabledBool = "CompassAlwaysOn.Enabled";
        public const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly List<ILHook> ModInteropHooks = new List<ILHook>();

        public static readonly List<(string, string)> HookableModClasses = new List<(string, string)>()
        {
            // (Type Full Name, Method Name), or (Type Full Name, null) to recursively hook all methods on that type
            ("Hkmp.Game.Client.MapManager, Hkmp", "HeroControllerOnUpdate"),
            ("AdditionalMaps.MonoBehaviours.GameMapHooks, AdditionalMaps", null)
        };

        public override void Initialize()
        {
            instance = this;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IL.GameMap.PositionCompass += ModifyCompassBool;
            IL.GameMap.WorldMap += ModifyCompassBool;

            // Caching the hookable methods turned out to save about 10% of the time, so I don't think it's worth the hassle
            foreach ((string typeName, string methodName) in HookableModClasses)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    HookMethodsOnType(typeName);
                }
                else
                {
                    HookMethod(typeName, methodName);
                }
            }
            sw.Stop();
            Log($"Generated Hook List in {sw.Elapsed.TotalSeconds} seconds.");

            ModHooks.GetPlayerBoolHook += InterpretCompassBool;
        }

        private void HookMethod(string typeName, string methodName)
        {
            Type type = Type.GetType(typeName);
            if (type == null) return;

            MethodInfo method = type.GetMethod(methodName, flags);
            if (method == null) return;

            if (IsHookable(method))
            {
                ILHook hook = new ILHook(method, ModifyCompassBool);
                ModInteropHooks.Add(hook);
                Log($"Hooking {typeName}:{methodName}");
            }
        }

        private void HookMethodsOnType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null) return;

            HookMethodsOnType(type);
        }

        private void HookMethodsOnType(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (IsHookable(method))
                {
                    ILHook hook = new ILHook(method, ModifyCompassBool);
                    ModInteropHooks.Add(hook);
                    Log($"Hooking {type.Name}:{method.Name}");
                }
            }

            foreach (Type nested in type.GetNestedTypes(flags))
            {
                HookMethodsOnType(nested);
            }
        }

        private bool IsHookable(MethodInfo method)
        {
            try
            {
                MethodDefinition def = new DynamicMethodDefinition(method)?.Definition;
                return def?.Body?.Instructions?.Any(i => i.MatchLdstr(nameof(PlayerData.equippedCharm_2))) ?? false;
            }
            catch
            {
                // Probably exception because empty method
                return false;
            }
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
