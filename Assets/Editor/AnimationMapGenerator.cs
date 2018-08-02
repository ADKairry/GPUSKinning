//
// Author : CHEN
// Time  : 2018-7-19
//

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class AnimationMapGenerator
{
    public struct AnimationData
    {
        public readonly string Name;
        public readonly List<AnimationState> States;
        public readonly int MapWidth;

        public readonly Animation Animation;
        public readonly SkinnedMeshRenderer MeshRenderer;

        public AnimationData(GameObject go)
        {
            Animation = go.GetComponentInChildren<Animation>();
            MeshRenderer = go.GetComponentInChildren<SkinnedMeshRenderer>();

            Name = go.name;
            States = new List<AnimationState>(Animation.Cast<AnimationState>());
            MapWidth = Mathf.NextPowerOfTwo(MeshRenderer.sharedMesh.vertexCount);
        }
    }

    public struct AnimationMapData
    {
        public string Name;
        public float AnimationLength;
        public Texture2D Map;

        public AnimationMapData(string name, float length, Texture2D map)
        {
            Name = name;
            AnimationLength = length;
            Map = map;
        }
    }

    /// <summary>
    /// 生成动画贴图
    /// 必须要有Animation和SkinnedMeshRenderer
    /// </summary>
    /// <param name="go">目标GameObject</param>
    public static List<AnimationMapData> GenerateAnimationMaps(GameObject go)
    {
        AnimationData animData = new AnimationData(go);
        List<AnimationMapData> mapData = new List<AnimationMapData>();

        foreach (AnimationState state in animData.States)
        {
            if (!state.clip.legacy)
            {
                Debug.LogError(string.Format("动画片段{0}的导入模式不是Legacy，已被跳过", state.name));
                continue;
            }

            mapData.Add(_bakeAnimationState(animData, state));
        }

        return mapData;
    }

    private static AnimationMapData _bakeAnimationState(AnimationData animData, AnimationState state)
    {
        string name = state.name;
        int frameCount = Mathf.ClosestPowerOfTwo((int)(state.clip.frameRate * state.clip.length));
        float frameDuration = state.length / frameCount;

        Texture2D map = new Texture2D(animData.MapWidth, frameCount, TextureFormat.RGBAHalf, false);
        map.name = name;

        Mesh mesh = new Mesh();
        Animation anim = animData.Animation;
        SkinnedMeshRenderer renderer = animData.MeshRenderer;

        float timePassed = 0;
        anim.Play(state.name);

        for (int i = 0; i < frameCount; i++)
        {
            state.time = timePassed;

            anim.Sample();
            renderer.BakeMesh(mesh);

            for (int j = 0; j < mesh.vertexCount; j++)
            {
                Vector3 vertex = mesh.vertices[j];
                Color color = new Color(vertex.x, vertex.y, vertex.z);
                map.SetPixel(j, i, color);
            }

            timePassed += frameDuration;
        }

        state.time = 0;
        anim.Sample();
        map.Apply();
        return new AnimationMapData(name, state.length, map);
    }
}