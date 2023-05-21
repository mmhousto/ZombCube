using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Com.GCTC.ZombCube
{
    public class NetworkHealthPack : MonoBehaviourPunCallbacks
    {
        SpriteRenderer healthPack;
        public bool isUsable;
        public string contextPrompt = "Hold 'E' for HP [Cost: 500]";

        // Start is called before the first frame update
        void Start()
        {
            healthPack = GetComponent<SpriteRenderer>();
            isUsable = true;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void StartResetHealthPack()
        {
            StartCoroutine(ResetHealthPack());
        }

        IEnumerator ResetHealthPack()
        {
            photonView.RPC("RPC_DisablePack", RpcTarget.AllBuffered);
            
            yield return new WaitForSeconds(120);
            photonView.RPC("RPC_EnablePack", RpcTarget.AllBuffered);
            
        }

        [PunRPC]
        public void RPC_DisablePack()
        {
            isUsable = false;
            healthPack.enabled = false;
        }

        [PunRPC]
        public void RPC_EnablePack()
        {
            healthPack.enabled = true;
            isUsable = true;
        }
    }
}

