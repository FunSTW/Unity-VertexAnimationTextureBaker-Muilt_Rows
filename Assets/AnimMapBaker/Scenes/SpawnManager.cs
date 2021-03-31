using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// demo使用，分配大量的活动的角色
/// </summary>
public class SpawnManager : MonoBehaviour {

    #region 字段

    public GameObject spawnPrefab;
    public int gridWidth;
    public int gridHeight;
    public float padding = 2;

    #endregion


    #region 方法


    void Start()
    {
        for(int i = 0; i < gridWidth; i++)
        {
            for(int j = 0; j < gridHeight; j++)
            {
                int x = i - gridWidth/2;
                int y = j - gridHeight / 2;
                Instantiate<GameObject>(spawnPrefab, new Vector3(x * padding, 0, y * padding), Quaternion.identity);
            }
        }
    }


    #endregion

}
