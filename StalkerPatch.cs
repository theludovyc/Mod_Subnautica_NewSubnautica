using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Straitjacket.Harmony;

namespace NewSubnautica
{
    class StalkerPatch : MonoBehaviour
    {
        Stalker c;

        bool b = false;

        void blop()
        {
            Debugger.Log("action : " + c.GetBestAction());
        }

        void Start()
        {
            c = GetComponent<Stalker>();

            foreach (var v in GetComponents<AggressiveWhenSeeTarget>())
            {
                Destroy(v);
            }

            Destroy(GetComponent<PrisonPredatorSwimToPlayer>());

            Destroy(GetComponent<AttackLastTarget>());

            foreach (var v in GetComponents<MoveTowardsTarget>())
            {
                Destroy(v);
            }

            var co0 = GetComponent<CollectShiny>();

            var co1 = gameObject.AddComponent<CollectShinyV2>();

            co1.mouth = co0.mouth;
            co1.shinyTargetAttach = co0.shinyTargetAttach;
            co1.eventQualifier = co0.eventQualifier;
            co1.swimVelocity = co0.swimVelocity;
            co1.swimInterval = co0.swimInterval;
            co1.updateTargetInterval = co0.updateTargetInterval;

            Destroy(co0);

            gameObject.AddComponent<AggressiveWhenSeePlayer>();

           var apwa = gameObject.AddComponent<AttackPlayerWhenAggressive>();

            Debugger.Log("apwa : " + apwa.aggressionThreshold);

            /*var ma0 = GetComponent<MeleeAttack>();

            var ma1 = gameObject.AddComponent<MeleeAttackV2>();
                
            ma1.copyMeleeAttack(ma0);

            Destroy(ma0);*/
        }

        void Update()
        {
            if (!b)
            {
                c.ScanCreatureActions();

                String s = "";

                foreach (var v in GetComponents<Component>())
                {
                    s += v + "\n";
                }

                Debugger.Log(s);

                //InvokeRepeating("blop", 0, 1);

                b = true;

                Destroy(this);
            }
        }
    }
}
