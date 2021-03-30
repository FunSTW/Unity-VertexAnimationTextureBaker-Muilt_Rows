/*
 * Created by jiadong chen
 * http://www.chenjd.me
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class AnimMapBakerWindow : EditorWindow {

    private enum SaveStrategy
    {
        AnimMap,//only anim map
        Mat,//with shader
        Prefab//prefab with mat
    }

    public enum Direction
    {
        X =  0, 
        Y =  1, 
        Z =  2,
        reverseX = 3,
        reverseY = 4,
        reverseZ = 5
    }

    #region FIELDS

    private static GameObject _targetGo;
    private static GameObject _oldTargetGo;
    private static AnimMapBaker _baker;
    private static bool _multipleRows = true;
    private static string _path = "_Export";
    private static string _subPath = "SubPath";
    private static string _shader = "funs/VATSimple";
    private static SaveStrategy _stratege = SaveStrategy.Prefab;
    private static Direction _X = Direction.X;
    private static Direction _Y = Direction.Y;
    private static Direction _Z = Direction.Z;
    private static Shader _animMapShader;

    private static bool showTransform = true,showAdvanced;
    #endregion


    #region  METHODS

    [MenuItem("Window/VAT AnimMap Baker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AnimMapBakerWindow));
        ResetAll();
    }

    public static void ResetAll() { 
        _baker = new AnimMapBaker();
        _animMapShader = Shader.Find(_shader);
    }

    private void OnGUI()
    {
        _targetGo = (GameObject)EditorGUILayout.ObjectField(_targetGo, typeof(GameObject), true);
        if(_oldTargetGo != _targetGo) { 
            _oldTargetGo = _targetGo;
            ResetAll();
        }
        _subPath = _targetGo == null ? _subPath : _targetGo.name;

        EditorGUILayout.HelpBox(string.Format($"Output Path : {Path.Combine(_path, _subPath)}"), MessageType.Info);
        _path = EditorGUILayout.TextField(_path);
        _subPath = EditorGUILayout.TextField(_subPath);

        _stratege = (SaveStrategy)EditorGUILayout.EnumPopup("output type:", _stratege);

        _multipleRows = EditorGUILayout.Toggle("Set VAT to multiple rows", _multipleRows);

        showTransform = EditorGUILayout.Foldout(showTransform, "Bake Transform");
        if(showTransform) {
            EditorGUI.indentLevel += 2;
            _X = (Direction)EditorGUILayout.EnumPopup("Right", _X);
            _Y = (Direction)EditorGUILayout.EnumPopup("Up", _Y);
            _Z = (Direction)EditorGUILayout.EnumPopup("Forward", _Z);
            EditorGUI.indentLevel -= 2;
        }
        int[] xyz = new int[3];
        SetXYZInfo(xyz, _X, _Y, _Z);

        int count = 0;
        int curClipFrame = 0;
        int bakedColume = 0;
        int bakedRow = 0;
        MessageType messageType = MessageType.Warning;
        if(_targetGo) {
            SkinnedMeshRenderer smr = _targetGo.GetComponentInChildren<SkinnedMeshRenderer>();
            Animation anim = _targetGo.GetComponent<Animation>();

            count = smr ? smr.sharedMesh.vertexCount : 0;
            if(anim != null) {
                List<AnimationState> _animClips = new List<AnimationState>(anim.Cast<AnimationState>());
                curClipFrame = (int)(_animClips[0].clip.frameRate * _animClips[0].length);
            }

            bakedColume = Mathf.NextPowerOfTwo(count);
            bakedRow = Mathf.ClosestPowerOfTwo(curClipFrame);

            if(_multipleRows) {
                bakedColume = (int)Mathf.Log(Mathf.NextPowerOfTwo(count), 2);
                bakedRow = (int)Mathf.Log(Mathf.ClosestPowerOfTwo(curClipFrame), 2);
                int colHighOffset = (bakedColume - bakedRow) / 2;
                if(colHighOffset > 0) {
                    bakedColume -= colHighOffset;
                    bakedRow += colHighOffset;
                }
                bakedColume = (int)Mathf.Pow(2, bakedColume);
                bakedRow = (int)Mathf.Pow(2, bakedRow);
            }

            if(smr && anim) {
                messageType = MessageType.Info;
            }
        }
        bool clickBakeBTN = !GUILayout.Button("Bake");

        string smrS = count != 0 ? $"Vertex Count : {count}" : "SkinnedMeshRenderer Missing";
        string aniS = curClipFrame != 0 ? $"Animation Frame Count : {curClipFrame}" : "Animation Missing";

        EditorGUILayout.HelpBox(string.Format($"{smrS}\n{aniS}" +
            $"\nBaked Texture Output Size : {bakedColume} x {bakedRow}"),
            messageType);

        EditorGUILayout.Space(10);
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
        if(showAdvanced) {
            EditorGUI.indentLevel += 2;
            _shader = EditorGUILayout.TextField("Shader Find", _shader);
            EditorGUI.indentLevel -= 2;
        }


        if(clickBakeBTN) return;

        if(_targetGo == null)
        {
            EditorUtility.DisplayDialog("err", "targetGo is null！", "OK");
            return;
        }

        if(_baker == null)
        {
            _baker = new AnimMapBaker();
        }

        _baker.SetAnimData(_targetGo, _multipleRows, xyz);

        List<BakedData> list = _baker.Bake();

        if (list == null) return;
        foreach (var t in list)
        {
            var data = t;
            Save(ref data);
        }
    }

    private void SetXYZInfo(int[] xyz, Direction X, Direction Y, Direction Z) {
        xyz[0] = (int)X;
        xyz[1] = (int)Y;
        xyz[2] = (int)Z;
    }

    private void Save(ref BakedData data)
    {
        switch(_stratege)
        {
            case SaveStrategy.AnimMap:
                SaveAsAsset(ref data);
                break;
            case SaveStrategy.Mat:
                SaveAsMat(ref data);
                break;
            case SaveStrategy.Prefab:
                SaveAsPrefab(ref data);
                break;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private Texture2D SaveAsAsset(ref BakedData data)
    {
        var folderPath = CreateFolder();
        var animMap = new Texture2D(data.AnimMapWidth, data.AnimMapHeight, TextureFormat.RGBAHalf, false);
        animMap.LoadRawTextureData(data.RawAnimMap);
        AssetDatabase.CreateAsset(animMap, Path.Combine(folderPath, data.Name + ".asset"));
        return animMap;
    }

    private Material SaveAsMat(ref BakedData data)
    {
        if(_animMapShader == null)
        {
            EditorUtility.DisplayDialog("err", "shader is null!!", "OK");
            return null;
        }

        if(_targetGo == null || !_targetGo.GetComponentInChildren<SkinnedMeshRenderer>())
        {
            EditorUtility.DisplayDialog("err", "SkinnedMeshRender is null!!", "OK");
            return null;
        }

        var smr = _targetGo.GetComponentInChildren<SkinnedMeshRenderer>();
        var mat = new Material(_animMapShader);
        var animMap = SaveAsAsset(ref data);
        mat.SetTexture("_MainTex", smr.sharedMaterial.mainTexture);
        mat.SetTexture("_AnimMap", animMap);
        mat.SetFloat("_AnimLen", data.AnimLen);
        mat.SetInt("_AnimOffsetYPixel", data.AnimOffsetYPixel);
        if(data.VATMultipleRows) {
            mat.EnableKeyword("VATMultipleRows_ON");
            mat.SetInt("_VATMultipleRows", 1);
        }

        var folderPath = CreateFolder();
        AssetDatabase.CreateAsset(mat, Path.Combine(folderPath, data.Name + ".mat"));

        return mat;
    }

    private void SaveAsPrefab(ref BakedData data)
    {
        var mat = SaveAsMat(ref data);

        if(mat == null)
        {
            EditorUtility.DisplayDialog("err", "mat is null!!", "OK");
            return;
        }

        var go = new GameObject();
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshFilter>().sharedMesh = _targetGo.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

        var folderPath = CreateFolder();
        PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(folderPath, data.Name + ".prefab")
            .Replace("\\", "/"));

        DestroyImmediate(go);
    }

    private static string CreateFolder()
    {
        var folderPath = Path.Combine("Assets/" + _path,  _subPath);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/" + _path, _subPath);
        }
        return folderPath;
    }

    #endregion


}
