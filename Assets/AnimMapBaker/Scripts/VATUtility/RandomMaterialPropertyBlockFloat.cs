using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMaterialPropertyBlockFloat : MonoBehaviour
{
    MaterialPropertyBlock prop = null;
    MeshRenderer meshRenderer = null;
    [SerializeField] string floatReferenceString = "_Diverse";
    [SerializeField] Vector2 randomRange = new Vector2(0, 1);
    public void Start() {
        meshRenderer = GetComponent<MeshRenderer>();
        prop = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(prop);
        prop.SetFloat(floatReferenceString, GetRandomValueFromVector2(randomRange));
        meshRenderer.SetPropertyBlock(prop);
    }
    static float GetRandomValueFromVector2(Vector2 vector2) {
        float value = Remap01(Random.value, vector2.x, vector2.y);
        return value;
    }

    static float Remap01(float x, float s1, float s2) {
        return x * (s2 - s1) + s1;
    }
}
