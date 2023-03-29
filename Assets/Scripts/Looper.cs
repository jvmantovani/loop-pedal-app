using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Looper : MonoBehaviour
{
    [SerializeField] AudioSource[] audioSources;
    [SerializeField] Image waveformImage;
    [SerializeField] GameObject feedbackLight;

    bool recording = false;
    double recStart;
    double recEnd;
    double clipRealDuration;

    AudioClip recordClip;
    Coroutine loopRoutine = null;

    [Header("Wave Selector")]
    [SerializeField] Slider leftSlider;
    [SerializeField] Slider rightSlider;
    double clipAdjustedStartTime = 0;
    double clipAdjustedDuration;

    [Header("Settings - Waveformer")]
    public int width = 1024;
    public int height = 64;
    public Color background = Color.black;
    public Color foreground = Color.yellow;

    private int samplesize;
    private float[] samples = null;
    private float[] waveform = null;
    private float arrowoffsetx;

    private void Start()
    {
        int minFreq;
        int maxFreq;

        for (int i = 0; i < Microphone.devices.Length; i++)
        {
            Microphone.GetDeviceCaps(Microphone.devices[i], out minFreq, out maxFreq);
            Debug.Log($"Device #{i} : {Microphone.devices[i]}");
            Debug.Log($"with minFreq={minFreq} / maxFreq={maxFreq}");
            Debug.Log("==========================================");
        }
    }

    // Button Event
    public void OnPedalPress()
    {
        if(!recording)
        {
            recording = true;
            recStart = AudioSettings.dspTime;

            if (loopRoutine != null) StopCoroutine(loopRoutine);
            audioSources[0].Stop();
            audioSources[1].Stop();

            feedbackLight.SetActive(true);
            recordClip = Microphone.Start(Microphone.devices[0], false, 60, 44100);
        }
        else
        {
            recording = false;
            recEnd = AudioSettings.dspTime;
            clipRealDuration = recEnd - recStart;

            feedbackLight.SetActive(false);
            Microphone.End(Microphone.devices[0]);

            waveformImage.sprite = GetWaveformSprite(recordClip);
            waveformImage.gameObject.SetActive(true);

            audioSources[0].clip = recordClip;
            audioSources[1].clip = recordClip;

            PlayLoop(recEnd);
        }
    }

    private void PlayLoop(double startDspTime)
    {
        double loopDuration = clipAdjustedDuration > 0 ? clipAdjustedDuration : clipRealDuration;
        loopRoutine = StartCoroutine(LoopRecordingRoutine(startDspTime, loopDuration));
    }

    private IEnumerator LoopRecordingRoutine(double start, double loopDuration)
    {
        while (audioSources[0].isPlaying) yield return null;

        audioSources[0].time = (float)clipAdjustedStartTime;            //put head on correct start time
        audioSources[0].PlayScheduled(start);                           //schedule play
        audioSources[0].SetScheduledEndTime(start + 1*loopDuration);    //schedule end

        while(audioSources[1].isPlaying) yield return null;

        audioSources[1].time = (float)clipAdjustedStartTime;            //put head on correct start time
        audioSources[1].PlayScheduled(start + 1*loopDuration);          //schedule play
        audioSources[1].SetScheduledEndTime(start + 2*loopDuration);    //schedule end

        loopRoutine = StartCoroutine(LoopRecordingRoutine(start + 2*loopDuration, loopDuration));
    }

    public void OnWaveSelectorSliderChange(bool isLeftSlider = true)
    {
        StopAllAudios();

        clipAdjustedStartTime = (double)leftSlider.normalizedValue * 0.5f * clipRealDuration;
        clipAdjustedDuration = clipRealDuration - clipAdjustedStartTime - (double)rightSlider.normalizedValue * 0.5f * clipRealDuration;

        PlayLoop(AudioSettings.dspTime + 0.5f);
    }

    private void StopAllAudios()
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        audioSources[0].Stop();
        audioSources[1].Stop();
    }

    private Sprite GetWaveformSprite(AudioClip clip)
    {
        int halfheight = height / 2;
        float heightscale = (float)height * 0.75f;

        // get the sound data
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        waveform = new float[width];

        samplesize = clip.samples * clip.channels;
        samples = new float[samplesize];
        clip.GetData(samples, 0);


        int clipRealSamples = Mathf.CeilToInt((float)(clipRealDuration / clip.length) * clip.samples);
        int realSampleSize = clipRealSamples * clip.channels;
        float[] realSamples = new float[realSampleSize];
        for (int i = 0; i < realSampleSize; i++)
        {
            realSamples[i] = samples[i];
        }


        //int packsize = (samplesize / width);
        //for (int w = 0; w < width; w++)
        //{
        //    waveform[w] = Mathf.Abs(samples[w * packsize]);
        //}
        int packsize = (realSampleSize / width);
        for (int w = 0; w < width; w++)
        {
            waveform[w] = Mathf.Abs(realSamples[w * packsize]);
        }

        // map the sound data to texture
        // 1 - clear
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, background);
            }
        }

        // 2 - plot
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < waveform[x] * heightscale; y++)
            {
                tex.SetPixel(x, halfheight + y, foreground);
                tex.SetPixel(x, halfheight - y, foreground);
            }
        }

        tex.Apply();

        Rect rect = new Rect(Vector2.zero, new Vector2(width, height));
        return Sprite.Create(tex, rect, Vector2.zero);
    }
}
