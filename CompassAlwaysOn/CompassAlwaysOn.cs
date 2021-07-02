using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;

namespace CompassAlwaysOn
{
    public class CompassAlwaysOn : Mod, ITogglableMod
    {
        internal static CompassAlwaysOn instance;

        public override void Initialize()
        {
            instance = this;

            On.GameMap.PositionCompass += GameMap_PositionCompass;
            On.GameMap.WorldMap += GameMap_WorldMap;
        }

        public void Unload()
        {
            On.GameMap.PositionCompass -= GameMap_PositionCompass;
            On.GameMap.WorldMap -= GameMap_WorldMap;
        }

        public override string GetVersion()
        {
            return "0.2";
        }

        private void GameMap_PositionCompass(On.GameMap.orig_PositionCompass orig, GameMap self, bool posShade)
        {
            bool tmp = PlayerData.instance.GetBoolInternal(nameof(PlayerData.equippedCharm_2));
            PlayerData.instance.SetBoolInternal(nameof(PlayerData.equippedCharm_2), true);

            orig(self, posShade);

            PlayerData.instance.SetBoolInternal(nameof(PlayerData.equippedCharm_2), tmp);
        }

        // Act as if compass is equipped when they scroll to an area where they don't have the map
        private void GameMap_WorldMap(On.GameMap.orig_WorldMap orig, GameMap self)
        {
            bool tmp = PlayerData.instance.GetBoolInternal(nameof(PlayerData.equippedCharm_2));
            PlayerData.instance.SetBoolInternal(nameof(PlayerData.equippedCharm_2), true);

            orig(self);

            PlayerData.instance.SetBoolInternal(nameof(PlayerData.equippedCharm_2), tmp);
        }
    }
}
