using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowFPS : MonoBehaviour
{
    public Text text;
    void Start()
    {
        text = GetComponent<Text>();
        QualitySettings.vSyncCount = 0;
    }

    void Update()
    {
        text.text = (1 / Time.deltaTime).ToString();
    }
}
