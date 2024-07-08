
using System;
using UnityEngine;
#if UNITY_PS5 || UNITY_PS4
using UnityEngine.InputSystem.PS5.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.PS5;
#endif
using UnityEngine.Serialization;


namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    /// <summary>
    /// This class deals with settings the haptic triggers on the controller, playing controller audio and controller
    /// vibration
    /// <remarks>Haptic Triggers are only implemented on device and will not work in the editor</remarks>
    /// </summary>
    public class GamepadHaptics : MonoBehaviour
    {
        [FormerlySerializedAs("gamepad")]
        [SerializeField] SampleGamepad sampleGamepad;
        [SerializeField] SpriteRenderer speakerVisual;
        [SerializeField] Gradient speakerVolumeGradient;

        [Header("Settings")]
        [SerializeField] bool enableAudio;
        [SerializeField] bool enableVibration;

        [Header("Trigger Audio")]
        [SerializeField] HapticAudio leftTriggerAudio;
        [SerializeField] HapticAudio rightTriggerAudio;

        [Header("Touchpad Audio")]
        [SerializeField] HapticAudio touch0Audio;
        [SerializeField] HapticAudio touch1Audio;

        void Awake()
        {
            leftTriggerAudio.CreateAudioSources(gameObject);
            rightTriggerAudio.CreateAudioSources(gameObject);
            touch0Audio.CreateAudioSources(gameObject);
            touch1Audio.CreateAudioSources(gameObject);
        }

        void Start()
        {
            sampleGamepad.gamepadControls.Main.LeftTrigger.started += StartLeftTriggerAudio;
            sampleGamepad.gamepadControls.Main.RightTrigger.started += StartRightTriggerAudio;
            sampleGamepad.gamepadControls.Main.RightTrigger.canceled += StopRightTriggerAudio;

            sampleGamepad.gamepadControls.Main.TouchpadTouch0.performed += context => PlayTouchPadAudio(0, context);
            sampleGamepad.gamepadControls.Main.TouchpadTouch1.performed += context => PlayTouchPadAudio(1, context);
        }

        void Update()
        {
            speakerVisual.color = speakerVolumeGradient.Evaluate(GamepadOutputVolume());
        }

        void StartRightTriggerAudio(InputAction.CallbackContext obj)
        {
            rightTriggerAudio.Start(sampleGamepad.gamepadDevice.slotIndex, enableAudio, enableVibration);
        }

        void StopRightTriggerAudio(InputAction.CallbackContext obj)
        {
            rightTriggerAudio.Stop();
        }

        void StartLeftTriggerAudio(InputAction.CallbackContext obj)
        {
            leftTriggerAudio.Start(sampleGamepad.gamepadDevice.slotIndex, enableAudio, enableVibration);
        }

        void PlayTouchPadAudio(int touchID, InputAction.CallbackContext obj)
        {
            HapticAudio audio;
            switch (touchID)
            {
                case 0:
                    audio = touch0Audio;
                    break;
                case 1:
                    audio = touch1Audio;
                    break;
                default:
                    Debug.LogError($"Touch {touchID} is not valid, cannot play touchpad audio");
                    return;
            }

            Vector2 touchPos = obj.ReadValue<Vector2>();

            //Ignore -1, -1 values as they are used when the touch pad is not being touched
            if (touchPos.x < 0f || touchPos.y < 0f)
            {
                audio.Stop();
                return;
            }


            audio.StartIfNotPlaying(sampleGamepad.gamepadDevice.slotIndex, enableAudio, enableVibration);

            AudioSource clipSource = audio.clipSource;
            AudioSource vibrationSource = audio.vibrationSource;

            float pitch = (1f - touchPos.y) + 0.5f;
            float clipVol = 0.25f;
            float vibrationVol = 1f - touchPos.y;
            float pan = touchPos.x * 2.0f - 1.0f;

            clipSource.pitch = pitch;
            clipSource.volume = clipVol;

            vibrationSource.pitch = pitch;
            vibrationSource.volume = vibrationVol;
            vibrationSource.panStereo = pan;
        }

        float GamepadOutputVolume()
        {
            return Mathf.Max(leftTriggerAudio.GetClipSourceVolumeFromOutput(),
                rightTriggerAudio.GetClipSourceVolumeFromOutput(),
                touch0Audio.GetClipSourceVolumeFromOutput(),
                touch1Audio.GetClipSourceVolumeFromOutput()
            );
        }

        [Serializable]
        public struct HapticAudio
        {
            public LoopableClip audioClip;
            public LoopableClip vibrationClip;

            [HideInInspector] public AudioSource clipSource;
            [HideInInspector] public AudioSource vibrationSource;

            const int k_QSamples = 1024;
            static float[] s_Samples = new float[k_QSamples]; // audio samples for monitoring volume.

            public void CreateAudioSources(GameObject sourcesGameObject)
            {
                if (clipSource == null)
                {
                    clipSource = sourcesGameObject.AddComponent<AudioSource>();
                    clipSource.clip = audioClip.clip;
                    clipSource.loop = audioClip.loop;
                    clipSource.gamepadSpeakerOutputType = GamepadSpeakerOutputType.Speaker;
                }

                if (vibrationSource == null)
                {
                    vibrationSource = sourcesGameObject.AddComponent<AudioSource>();
                    vibrationSource.clip = vibrationClip.clip;
                    vibrationSource.loop = vibrationClip.loop;
                    vibrationSource.gamepadSpeakerOutputType = GamepadSpeakerOutputType.Vibration;
                }
            }

            public void Start(int slot, bool audio, bool vibration)
            {
                if (audio && clipSource)
                {
                    clipSource.PlayOnGamepad(slot);
                }

                if (vibration && vibrationSource)
                {
                    vibrationSource.PlayOnGamepad(slot);
                }
            }

            public void StartIfNotPlaying(int slot, bool audio, bool vibration)
            {
                if (audio && clipSource && !clipSource.isPlaying)
                {
                    clipSource.PlayOnGamepad(slot);
                }

                if (vibration && vibrationSource && !vibrationSource.isPlaying)
                {
                    vibrationSource.PlayOnGamepad(slot);
                }
            }

            public void Stop()
            {
                clipSource.Stop();
                vibrationSource.Stop();
            }

            public float GetClipSourceVolumeFromOutput()
            {
                float volume = 0.0f;

                if (clipSource == null || !clipSource.isPlaying)
                {
                    return 0.0f;
                }

                if (clipSource.time > 0f)
                {
                    clipSource.GetOutputData(s_Samples, 0); // fill array with samples
                    int i;
                    var sum = 0f;

                    for (i = 0; i < k_QSamples; i++)
                        sum += s_Samples[i] * s_Samples[i]; // sum squared samples

                    volume = Mathf.Sqrt(sum / k_QSamples); // rms = square root of average
                    volume *= clipSource.volume;
                }

                return volume;
            }
        }

        [Serializable]
        public struct LoopableClip
        {
            public AudioClip clip;
            public bool loop;
        }
    }
}
