using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackController : MonoBehaviour
{
    public AudioSource[] AudioSources;
    [SerializeField] Transform playHead;

    [Header("Sliders")]
    public Slider LeftSlider;
    public Slider RightSlider;

    Looper looper;

    Coroutine playHeadRoutine = null;
    WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    private void Start()
    {
        looper = FindFirstObjectByType<Looper>();
        looper.OnPlaybackStart += StartPlayHead;
        looper.OnPlaybackEnd += EndPlayHead;
    }

    private void OnDestroy()
    {
        looper.OnPlaybackStart -= StartPlayHead;
        looper.OnPlaybackEnd -= EndPlayHead;
    }

    private void StartPlayHead(double unadjustedDuration)
    {
        playHeadRoutine = StartCoroutine(PlayHeadRoutine(unadjustedDuration));
    }

    private void EndPlayHead()
    {
        if (playHeadRoutine != null) StopCoroutine(playHeadRoutine);
        SetPlayheadPosition(0);
    }

    private IEnumerator PlayHeadRoutine(double unadjustedDuration)
    {
        while (true)
        {
            for (int i = 0; i < AudioSources.Length; i++)
            {
                AudioSource source = AudioSources[i];
                if(source.isPlaying && source.time > LeftSlider.value * unadjustedDuration)
                {
                    SetPlayheadPosition(source.time, (float)unadjustedDuration);
                }
            }
            yield return endOfFrame;
        }
    }

    private void SetPlayheadPosition(float time, float duration = 1)
    {
        playHead.localPosition = new Vector3(((time / duration) * 1024) - 1024 / 2, 0, 0);
    }
}
