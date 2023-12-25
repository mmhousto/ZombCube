using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class SwapManager : MonoBehaviour
    {
        public bool isSwapping;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void OnSwapWeapons(InputValue context)
        {
            SwapInput(context.isPressed);
        }

        public virtual void SwapInput(bool newValue)
        {
            isSwapping = newValue;
        }
    }
}
