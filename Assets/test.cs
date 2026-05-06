using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class test : MonoBehaviour
{
    public GameObject prefab;
    // Start is called before the first frame update
    public Button myButton;
    void Start()
    {
       
    }

    // Update is called once per frame
    
    void Update()
    {
        GameObject[] gos = new GameObject[prefab.transform.childCount];
        for (int i = 0; i < gos.Length; i++)
        {
            gos[i] = prefab.transform.GetChild(i).gameObject;
            gameObject.transform.GetChild(i).transform.position = gos[i].transform.position;
            gameObject.transform.GetChild(i).transform.rotation = gos[i].transform.rotation;
            gameObject.transform.GetChild(i).transform.localScale = gos[i].transform.localScale;
        }
    }
}
