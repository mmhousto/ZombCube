using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class PSNotifyPremium : MonoBehaviour
    {
#if UNITY_PS5 && !UNITY_EDITOR
        // Start is called before the first frame update
        void Start()
        {
            InvokeRepeating(nameof(NotifyPremium), 1f, 1f);
        }

        void NotifyPremium()
        {
            PSFeatureGating.NotifyPremium();
        }
#endif
    }
}
