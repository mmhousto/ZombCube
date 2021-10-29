using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class LocalPlayerControl : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (IsLocalPlayer) return;

        var cam = GetComponentInChildren<Camera>();

        cam.enabled = false;
        // or a bit more radical
        Destroy(cam);

        // same for all other components that you don't want a remote player to have
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
