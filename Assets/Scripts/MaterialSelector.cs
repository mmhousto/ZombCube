using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSelector : MonoBehaviour
{
    private static MaterialSelector _instnace;

    public static MaterialSelector Instance { get { return _instnace; } }

    public Material[] materials;

    private void Awake()
    {
        if (_instnace != null && _instnace != this)
        {
            Destroy(this);
        }
        else
        {
            _instnace = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
