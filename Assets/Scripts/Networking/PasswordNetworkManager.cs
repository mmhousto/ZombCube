using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MLAPI;
using System;
using System.Text;
using MLAPI.SceneManagement;

public class PasswordNetworkManager : MonoBehaviour
{

    [SerializeField] private static TMP_InputField passwordInputField;

    private void Start()
    {
        passwordInputField = GetComponent<TMP_InputField>();
    }

}
