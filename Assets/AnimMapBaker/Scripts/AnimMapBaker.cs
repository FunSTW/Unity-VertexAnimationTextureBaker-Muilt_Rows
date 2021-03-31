/*
 * Created by jiadong chen
 * http://www.chenjd.me
 * 
 * 用来烘焙动作贴图。烘焙对象使用animation组件，并且在导入时设置Rig为Legacy
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

/// <summary>
/// 保存需要烘焙的动画的相关数据
/// </summary>
public struct AnimData
{
    #region FIELDS

    private int _vertexCount;
    private int _mapWidth;
    private readonly List<AnimationState> _animClips;
    private string _name;
    private bool _multipleRows;
    private int[] _XYZ;

    private Animation _animation;
    private SkinnedMeshRenderer _skin;

    public List<AnimationState> AnimationClips => _animClips;
    public int MapWidth => _mapWidth;
    public string Name => _name;
    public bool MultipleRows => _multipleRows;
    public int[] XYZ => _XYZ;


    #endregion

    public AnimData(Animation anim, SkinnedMeshRenderer smr, string goName,bool multipleRows, int[] XYZ)
    {
        _vertexCount = smr.sharedMesh.vertexCount;
        _mapWidth = Mathf.NextPowerOfTwo(_vertexCount);
        _animClips = new List<AnimationState>(anim.Cast<AnimationState>());
        _animation = anim;
        _skin = smr;
        _name = goName;
        _multipleRows = multipleRows;
        _XYZ = XYZ;
    }

    #region METHODS

    public void AnimationPlay(string animName)
    {
        _animation.Play(animName);
    }

    public void SampleAnimAndBakeMesh(ref Mesh m)
    {
        SampleAnim();
        BakeMesh(ref m);
    }

    private void SampleAnim()
    {
        if (_animation == null)
        {
            Debug.LogError("animation is null!!");
            return;
        }

        _animation.Sample();
    }

    private void BakeMesh(ref Mesh m)
    {
        if (_skin == null)
        {
            Debug.LogError("skin is null!!");
            return;
        }

        _skin.BakeMesh(m);
    }


    #endregion

}



/// <summary>
/// 烘焙后的数据
/// </summary>
public struct BakedData
{
    #region FIELDS

    private readonly string _name;
    private readonly float _animLen;
    private readonly byte[] _rawAnimMap;
    private readonly int _animMapWidth;
    private readonly int _animMapHeight;
    private readonly bool _VATMultipleRows;
    private readonly int _animOffsetYPixel;

    #endregion

    public BakedData(string name, float animLen, Texture2D animMap,bool VATMultipleRows, int animOffsetYPixel)
    {
        _name = name;
        _animLen = animLen;
        _animMapHeight = animMap.height;
        _animMapWidth = animMap.width;
        _rawAnimMap = animMap.GetRawTextureData();
        _VATMultipleRows = VATMultipleRows;
        _animOffsetYPixel = animOffsetYPixel;
    }

    public int AnimMapWidth => _animMapWidth;

    public string Name => _name;

    public float AnimLen => _animLen;

    public byte[] RawAnimMap => _rawAnimMap;

    public int AnimMapHeight => _animMapHeight;

    public bool VATMultipleRows => _VATMultipleRows;

    public int AnimOffsetYPixel => _animOffsetYPixel;

}

/// <summary>
/// 烘焙器
/// </summary>
public class AnimMapBaker{

    #region FIELDS

    private AnimData? _animData = null;
    private Mesh _bakedMesh;
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<BakedData> _bakedDataList = new List<BakedData>();
    #endregion

    #region METHODS

    public void SetAnimData(GameObject go,bool multipleRows,int[] XYZ)
    {
        if(go == null)
        {
            Debug.LogError("go is null!!");
            return;
        }

        var anim = go.GetComponent<Animation>();
        var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();

        if(anim == null || smr == null)
        {
            Debug.LogError("anim or smr is null!!");
            return;
        }
        _bakedMesh = new Mesh();
        _animData = new AnimData(anim, smr, go.name, multipleRows, XYZ);
    }

    public List<BakedData> Bake()
    {
        if(_animData == null)
        {
            Debug.LogError("bake data is null!!");
            return _bakedDataList;
        }

        //每一个动作都生成一个动作图
        foreach (var t in _animData.Value.AnimationClips)
        {
            if(!t.clip.legacy)
            {
                Debug.LogError(string.Format($"{t.clip.name} is not legacy!!"));
                continue;
            }
            BakePerAnimClip(t, _animData.Value.MultipleRows, _animData.Value.XYZ);
        }

        return _bakedDataList;
    }

    private void BakePerAnimClip(AnimationState curAnim,bool multipleRows,int[] xyz)
    {
        var curClipFrame = 0;
        float sampleTime = 0;
        float perFrameTime = 0;

        //總幀數（Frame*Sec）轉換成2的冪
        curClipFrame = Mathf.ClosestPowerOfTwo((int)(curAnim.clip.frameRate * curAnim.length));
        //總秒數/總幀數 per frame sec
        perFrameTime = curAnim.length / curClipFrame;

        int col = _animData.Value.MapWidth;
        int row = curClipFrame;

        if(multipleRows) {
            //總秒數/(總幀數+Padding) per frame sec
            perFrameTime = curAnim.length / (curClipFrame - 2);

            //先不管頂點多還是動畫比較長 目前假設頂點永遠大於動畫 _animData.Value.MapWidth已經被NextPowerOfTwo過了不多處理
            col = (int)Mathf.Log(_animData.Value.MapWidth, 2); //256 8
            row = (int)Mathf.Log(curClipFrame, 2);

            //這邊無條件去位(轉型) 寬 > 高
            int colHighOffset = (col - row) / 2;
            //寬(頂點) 不會小於 高(動畫) 因為我們不會裁掉動畫
            if(colHighOffset > 0) {
                col -= colHighOffset;
                row += colHighOffset;
            }

            col = (int)Mathf.Pow(2, col);
            row = (int)Mathf.Pow(2, row);
        }

        var animMap = new Texture2D(col ,row, TextureFormat.RGBAHalf, true);
        animMap.name = string.Format($"{_animData.Value.Name}_{curAnim.name}");
        _animData.Value.AnimationPlay(curAnim.name);

        //Animation Row
        for (var i = 0; i < curClipFrame; i++)
        {
            curAnim.time = sampleTime;
            if(multipleRows) {
                if(i == 0) {
                    //第一排   採樣 最後一幀
                    curAnim.time = curAnim.length - perFrameTime;
                } else if(i == curClipFrame) {
                    //最後一排 採樣 第一幀
                    curAnim.time = 0;
                }
            }

            _animData.Value.SampleAnimAndBakeMesh(ref _bakedMesh);
            int vertexCount = _bakedMesh.vertexCount;

            //Vertex Column
            for(var j = 0; j < vertexCount; j++)
            {
                int pixelX = j;
                int pixelY = i;

                if(multipleRows) {
                    int high =   j / col;
                    pixelX = j % col;
                    pixelY = i + (curClipFrame * high);
                }
                
                var vertex = _bakedMesh.vertices[j];
                Color color = GetFixVertex(vertex, xyz);
                animMap.SetPixel(pixelX, pixelY, color);
            }


            if(multipleRows) {
                bool skipFrameAndDrawPadding = (i == 0 || i == curClipFrame);
                if(skipFrameAndDrawPadding) continue;
            }
            
            sampleTime += perFrameTime;
        }
        animMap.Apply();

        _bakedDataList.Add(new BakedData(animMap.name, curAnim.clip.length, animMap, multipleRows, curClipFrame));
    }

    private Color GetFixVertex(Vector3 vertex , int[] xyz) {
        Color color = new Color();
        for(int i = 0; i < 3; i++) {
            color[i] = GetVertex(vertex, xyz[i]);
        }
        return color;
    }
    private float GetVertex(Vector3 vertex,int i) {
        float f = 0;
        if(i == 0)      f =  vertex.x;
        else if(i == 1) f =  vertex.y;
        else if(i == 2) f =  vertex.z;
        else if(i == 3) f = -vertex.x;
        else if(i == 4) f = -vertex.y;
        else if(i == 5) f = -vertex.z;
        return f;
    }

    #endregion

}
