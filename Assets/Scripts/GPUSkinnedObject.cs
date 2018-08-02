//
// Author : CHEN
// Time  : 2018-7-19
//

using System;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinnedObject : MonoBehaviour
{
    [Serializable]
    public struct AnimationInfomation
    {
        public string Name;
        public float Length;
        public int Index;
    }

    public AnimationInfomation[] AnimationInfomations;
    public string DefaultAnimation;

    protected MeshRenderer MeshRenderer;
    protected Dictionary<string, int> AnimIndexDic;
    protected Dictionary<string, float> AnimLengthDic;

    private string _animName;
    private float _animSpeed = 1;
    private WrapMode _animMode = WrapMode.Once;
    private float _animLength = 0;
    private float _animTimePassed;

    private int _animTimeShaderPropHash;

    private MaterialPropertyBlock _materialPropertyBlock;

    protected virtual void Init()
    {
        _animTimePassed = 0;
        MeshRenderer = GetComponentInChildren<MeshRenderer>();
        AnimIndexDic = new Dictionary<string, int>();
        AnimLengthDic = new Dictionary<string, float>();
        for (int i = 0; i < AnimationInfomations.Length; i++)
        {
            AnimationInfomation info = AnimationInfomations[i];
            AnimIndexDic.Add(info.Name, info.Index);
            AnimLengthDic.Add(info.Name, info.Length);
        }

        _materialPropertyBlock = new MaterialPropertyBlock();
        _animName = DefaultAnimation;
        _animLength = AnimLengthDic[DefaultAnimation];
        _animTimeShaderPropHash = Shader.PropertyToID("_AnimTime");
    }

    protected void Awake()
    {
        Init();
    }

    protected void Update()
    {
        if (_animSpeed != 0)
        {
            _animTimePassed += Time.deltaTime * _animSpeed;
            if (_animLength != 0 && _animTimePassed >= _animLength)
            {
                if (_animMode == WrapMode.Once)
                {
                    PlayAnimation(DefaultAnimation, WrapMode.Loop, 1, 0);
                    return;
                }

                _animTimePassed = 0;
            }

            _materialPropertyBlock.SetFloat(_animTimeShaderPropHash, _animTimePassed);
            MeshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }

    public virtual void PlayAnimation(string anim, WrapMode wrap, float speed, float startTime)
    {
        if (string.IsNullOrEmpty(anim))
        {
            return;
        }

        if (anim == _animName)
        {
            _animSpeed = speed;
            _animMode = wrap;
            _animTimePassed = startTime;
            return;
        }

        int animIndex;
        if (AnimIndexDic.TryGetValue(anim, out animIndex))
        {
            _animName = anim;
            _animSpeed = speed;
            _animMode = wrap;
            _animLength = AnimLengthDic[anim];
            _animTimePassed = startTime;

            _materialPropertyBlock.SetFloat("_Index", animIndex);
            _materialPropertyBlock.SetFloat("_AnimLength", AnimLengthDic[anim]);
            MeshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }

    public virtual void StopAnimation()
    {
        _animSpeed = 0;
    }

    public virtual void ResumeAnimation()
    {
        _animSpeed = 1;
    }

    public virtual string CurrentAnimation
    {
        get { return _animName; }
    }

    public virtual float CurrentAnimationLength
    {
        get { return _animLength; }
    }
}