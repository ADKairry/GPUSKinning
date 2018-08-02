//
// Author : CHEN
// Time  : 2018-7-19
//

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

internal class AnimationMapGeneratorWindow : EditorWindow
{
    [MenuItem("GPUSkinning/Generate Animation Map")]
    [MenuItem("Assets/Generate Animation Map")]
    public static void GenerateAnimationMap()
    {
        Object obj = Selection.activeObject;
        GameObject go = obj as GameObject;

        AnimationMapGeneratorWindow window = GetWindow<AnimationMapGeneratorWindow>();
        window.titleContent = new GUIContent("GPUSkinning");
        window._targetGameObject = go;

        string path = go != null ? AssetDatabase.GetAssetPath(go) : string.Empty;
        path = string.IsNullOrEmpty(path) ? path : path.Remove(path.LastIndexOf('/') + 1) + "AnimationMap";
        window._path = path;
    }

    [MenuItem("GPUSkinning/Generate Animation Map", true)]
    [MenuItem("Assets/Generate Animation Map", true)]
    public static bool Validation()
    {
        Object obj = Selection.activeObject;
        GameObject go = obj as GameObject;

        if (go != null)
        {
            Animation[] animations = go.GetComponentsInChildren<Animation>();
            if (animations == null || animations.Length > 1)
            {
                return false;
            }

            SkinnedMeshRenderer[] renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers == null || renderers.Length > 1)
            {
                return false;
            }
        }

        return true;
    }

    private enum EGenerationType
    {
        Texture,
        Material,
        Prefab
    }

    private GameObject _targetGameObject;
    private string _path = "";
    private string _shaderPath = "GPUSKinning/GPUSkinnedObject";
    private string _defaultAnim = "idle";
    private string _folderName = "AnimationMap";
    private EGenerationType _generationType = EGenerationType.Prefab;

    private Vector3 _rootOffset;
    private Quaternion _rootRotation;

    private enum EScriptType
    {
        Default,
    }

    private EScriptType _scriptType = EScriptType.Default;

    private List<AnimationMapGenerator.AnimationMapData> _mapDataList;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(4, 4, Screen.width - 8, Screen.height - 8));

        int areaWidth = Screen.width - 8;
        int areaHeight = 175;

        GUILayout.BeginArea(new Rect(0, 0, areaWidth, areaHeight));
        GUI.color = Color.cyan;
        GUILayout.Label("Source Model:"); GUI.color = Color.white;
        _targetGameObject = (GameObject)EditorGUILayout.ObjectField(_targetGameObject, typeof(GameObject), true);
        if (_targetGameObject != null)
        {
            Texture2D previewTex = AssetPreview.GetAssetPreview(_targetGameObject);
            if (previewTex != null)
            {
                GUILayout.BeginArea(new Rect(Screen.width / 2 - previewTex.width / 2, 40, previewTex.width, previewTex.height), previewTex);
                GUILayout.EndArea();
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, areaHeight, areaWidth, 300));

        GUI.color = Color.cyan;
        GUILayout.Label("Shader Path:");
        GUI.color = Color.white;
        GUI.enabled = false;
        _shaderPath = EditorGUILayout.TextField(_shaderPath);
        GUI.enabled = true;

        GUILayout.Space(4);

        GUI.color = Color.cyan;
        GUILayout.Label("Default Animation:");
        GUI.color = Color.white;
        _defaultAnim = EditorGUILayout.TextField(_defaultAnim);

        GUILayout.Space(4);

        GUI.color = Color.cyan;
        GUILayout.Label("AnimationMap Save Path:");
        GUI.color = Color.white;
        _path = GUILayout.TextField(_path);
        _folderName = _path.Substring(_path.LastIndexOf("/", StringComparison.Ordinal) + 1);

        GUILayout.Space(16);

        _generationType = (EGenerationType)EditorGUILayout.EnumPopup("Generation Type:", _generationType);

        GUI.enabled = _generationType == EGenerationType.Prefab;

        _scriptType = (EScriptType)EditorGUILayout.EnumPopup("Script Type:", _scriptType);

        GUI.enabled = true;

        GUI.enabled = _targetGameObject != null;

        GUILayout.Space(16);

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Generate"))
        {
            switch (_generationType)
            {
                case EGenerationType.Texture:
                    _saveAsTexture();
                    break;
                case EGenerationType.Material:
                    Dictionary<string, int> indexDic;
                    Dictionary<string, float> lengthDic;
                    _saveAsMaterial(out indexDic, out lengthDic);
                    break;
                case EGenerationType.Prefab:
                    _saveAsPrefab();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        GUI.enabled = true;
        GUILayout.EndArea();
        GUILayout.EndArea();
    }

    private Texture2D[] _saveAsTexture()
    {
        _createDirectory();

        SkinnedMeshRenderer smr = _targetGameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        Transform root = smr.transform;
        _rootOffset = root.localPosition;
        _rootRotation = root.localRotation;

        _mapDataList = AnimationMapGenerator.GenerateAnimationMaps(_targetGameObject);
        Texture2D[] texList = new Texture2D[_mapDataList.Count];

        for (int i = 0; i < _mapDataList.Count; i++)
        {
            AnimationMapGenerator.AnimationMapData mapData = _mapDataList[i];
            texList[i] = mapData.Map;
            if (_generationType == EGenerationType.Texture)
            {
                AssetDatabase.CreateAsset(mapData.Map, string.Format("{0}/{1}.asset", _path, mapData.Name));
            }
        }

        return texList;
    }

    private Material _saveAsMaterial(out Dictionary<string, int> indexDic, out Dictionary<string, float> lengthDic)
    {
        Shader shader = Shader.Find(_shaderPath);
        if (shader == null)
        {
            Debug.LogError("Cannot load shader at " + _shaderPath);
            indexDic = null;
            lengthDic = null;
            return null;
        }

        Texture2D[] textureList = _saveAsTexture();

        int maxWidth = 0;
        int maxHeight = 0;
        for (int i = 0; i < _mapDataList.Count; i++)
        {
            Texture2D texture = textureList[i];
            if (texture.width > maxWidth)
            {
                maxWidth = texture.width;
            }
            if (texture.height > maxHeight)
            {
                maxHeight = texture.height;
            }
        }

        indexDic = new Dictionary<string, int>();
        lengthDic = new Dictionary<string, float>();

        Texture2DArray texArray = new Texture2DArray(maxWidth, maxHeight, _mapDataList.Count, TextureFormat.RGBAFloat, false);
        for (int i = 0; i < _mapDataList.Count; i++)
        {
            AnimationMapGenerator.AnimationMapData mapData = _mapDataList[i];
            Texture2D texture = textureList[i];
            texArray.SetPixels(texture.GetPixels(), i);
            indexDic.Add(mapData.Name, i);
            lengthDic.Add(mapData.Name, mapData.AnimationLength);
        }

        Material mat = new Material(shader);
        mat.SetTexture("_MainTex", _targetGameObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.mainTexture);
        mat.SetTexture("_AnimMapArray", texArray);
        mat.SetFloat("_AnimLength", _mapDataList[0].AnimationLength);
        mat.SetFloat("_Index", 0);
        mat.enableInstancing = true;

        AssetDatabase.CreateAsset(texArray, string.Format("{0}/{1}.asset", _path, "AnimationMap"));
        AssetDatabase.CreateAsset(mat, string.Format("{0}/{1}.mat", _path, "AnimationMaterial"));

        return mat;
    }

    private void _saveAsPrefab()
    {
        Dictionary<string, int> indexDic;
        Dictionary<string, float> lengthDic;
        Material material = _saveAsMaterial(out indexDic, out lengthDic);

        GameObject root = new GameObject("Root");
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        renderer.material = material;
        root.AddComponent<MeshFilter>().sharedMesh = _targetGameObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        root.transform.localPosition = _rootOffset;
        root.transform.localRotation = _rootRotation;

        GameObject go = new GameObject(_targetGameObject.name + "_GPUSkinned");
        root.transform.SetParent(go.transform, false);

        Type scriptType;
        switch (_scriptType)
        {
            case EScriptType.Default:
                scriptType = typeof(GPUSkinnedObject);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        GPUSkinnedObject objectView = (GPUSkinnedObject)root.AddComponent(scriptType);
        objectView.AnimationInfomations = new GPUSkinnedObject.AnimationInfomation[indexDic.Count];
        int i = 0;
        foreach (KeyValuePair<string, int> pair in indexDic)
        {
            string animName = pair.Key;
            int index = pair.Value;
            float length = lengthDic[animName];
            objectView.AnimationInfomations[i] = new GPUSkinnedObject.AnimationInfomation
            {
                Index = index,
                Length = length,
                Name = animName
            };
            i++;
        }
        objectView.DefaultAnimation = _defaultAnim;

        string baseFolder = _path.Remove(_path.Length - _folderName.Length - 1);
        PrefabUtility.CreatePrefab(string.Format("{0}/{1}.prefab", baseFolder, _targetGameObject.name + "_GPUSkinned"), go);
    }

    private void _createDirectory()
    {
        if (!AssetDatabase.IsValidFolder(_path))
        {
            string baseFolder = _path.Remove(_path.Length - _folderName.Length - 1);
            AssetDatabase.CreateFolder(baseFolder, _folderName);
        }
    }
}