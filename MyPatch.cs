using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using Straitjacket.Harmony;

namespace NewSubnautica
{
    [HarmonyPatch(typeof(Stalker))]
    [HarmonyPatch("InitializeOnce")]
    class MyPatch0 : Creature
    {
        static void Postfix(Stalker __instance)
        {
            __instance.gameObject.AddComponent<StalkerPatch>();
        }
    }
}
