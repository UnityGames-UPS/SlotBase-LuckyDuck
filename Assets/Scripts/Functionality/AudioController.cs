using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource bg_adudio;
    [SerializeField] internal AudioSource audioPlayer_wl;
    [SerializeField] internal AudioSource audioPlayer_button;
    [SerializeField] internal AudioSource audioSpin_button;
    [SerializeField] private AudioClip[] clips;

    private List<AudioSource> allSources;
    private readonly Dictionary<AudioSource, bool> preFocusMuteState = new Dictionary<AudioSource, bool>();
    private bool isForceMuted = false;

    private void Awake()
    {
        allSources = new List<AudioSource> { bg_adudio, audioPlayer_wl, audioPlayer_button, audioSpin_button };
    }

    private void Start()
    {
        if (bg_adudio) bg_adudio.Play();
        audioPlayer_button.clip = clips[clips.Length-1];
        audioSpin_button.clip = clips[clips.Length-2];
    }

    // Focus-driven mute. Called from BOTH the JS path (UIManager.OnFocusChanged) and the
    // native path (OnApplicationFocus below). This game keeps the user's chosen setting on each
    // AudioSource's own .mute flag (per-category sliders), so capture/restore is per-source.
    internal void SetMuteAll(bool forceMute)
    {
        // Reentrancy guard: a duplicate call for the same direction must not re-capture the
        // already-forced state as the user's "restore to" value.
        if (forceMute == isForceMuted) return;
        isForceMuted = forceMute;

        foreach (var source in allSources)
        {
            if (source == null) continue;
            if (forceMute)
            {
                preFocusMuteState[source] = source.mute;
                source.mute = true;
            }
            else
            {
                source.mute = preFocusMuteState.TryGetValue(source, out bool prevMuted) ? prevMuted : source.mute;
            }
        }
    }

    // Native/editor focus path — calls the SAME method the WebGL OnFocusChanged path calls.
    private void OnApplicationFocus(bool focus)
    {
        SetMuteAll(!focus);
    }



    internal void PlayWLAudio(string type)
    {
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "spin":
                index = 0;
                audioPlayer_wl.loop = true;
                break;
            case "win":
                index = 1;
                break;
            case "megaWin":
                index = 2;
                break;
        }
        StopWLAaudio();
        audioPlayer_wl.clip = clips[index];
        audioPlayer_wl.Play();

    }

 

    internal void PlayButtonAudio()
    {
        audioPlayer_button.Play();
    }

    internal void PlaySpinButtonAudio()
    {
        audioSpin_button.Play();
    }

    internal void StopWLAaudio()
    {
        audioPlayer_wl.Stop();
        audioPlayer_wl.loop = false;
    }



    internal void StopBgAudio()
    {
        bg_adudio.Stop();
    }

    internal void ChangeVol ( float value,string type)
    {
        // An explicit user interaction proves the game really has focus — drop any stale
        // forced-mute so a missed focus-regain signal can't keep the sound button dead.
        if (isForceMuted)
        {
            isForceMuted = false;
            preFocusMuteState.Clear();
        }

        switch (type)
        {
            case "bg":
                bg_adudio.mute = (value<0.1);
                bg_adudio.volume=value;
                break;
            case "button":
                audioPlayer_button.mute=(value<0.1);
                audioSpin_button.volume=value;
                break;
            case "wl":
                audioPlayer_wl.mute=(value<0.1);
                audioPlayer_wl.volume=value;
                break;
            case "all":
                audioPlayer_wl.mute = (value<0.1);
                bg_adudio.mute = (value<0.1);
                audioPlayer_button.mute = (value<0.1);
                audioSpin_button.mute = (value<0.1);

                audioPlayer_wl.volume = (value);
                bg_adudio.volume = (value);
                audioPlayer_button.volume = (value);
                audioSpin_button.volume = (value);

                break;
        }
    }

}
