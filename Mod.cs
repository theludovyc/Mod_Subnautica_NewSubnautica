using System;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace NewSubnautica
{
    public class Mod
    {
        public static void Load()
        {
            try
            {
                HarmonyInstance.Create("subnautica.newsubnautica.mod").PatchAll(Assembly.GetExecutingAssembly());
            }catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
