using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    public class ErrorManager : MonoBehaviour
    {
        private static ErrorManager instance;

        public static ErrorManager Instance { get { return instance; } }

        private TextMeshProUGUI errorMessage;
        private Image background;
        private Coroutine errorCoroutine;

        private void Awake()
        {
            if(instance != this && instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            
            errorMessage = GetComponentInChildren<TextMeshProUGUI>();

            background = GetComponentInChildren<Image>();
            background.gameObject.SetActive(false);
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        public void StartErrorMessage(string message)
        {
            StartCoroutine(ShowErrorMessage(message));
        }

        public IEnumerator ShowErrorMessage(string message)
        {
            if(instance == null) { yield break; }
            if (errorCoroutine != null) yield return new WaitForSeconds(3f);

            errorMessage.text = message;
            background.gameObject.SetActive(true);
            errorCoroutine = StartCoroutine(DisableErrorMessage());
        }

        IEnumerator DisableErrorMessage()
        {
            yield return new WaitForSeconds(3f);
            background.gameObject.SetActive(false);
            errorCoroutine = null;
        }
    }
}
