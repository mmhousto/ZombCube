using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    /// <summary>
    /// This class set the trigger effects according to the options defined within the inspector
    /// </summary>
    public class GamepadTriggerEffects : MonoBehaviour
    {
        [FormerlySerializedAs("gamepad")]
        [SerializeField]
        SampleGamepad sampleGamepad;

        [Header("Effects")]
        [SerializeField] TriggerEffect leftTriggerEffect;
        [SerializeField] TriggerEffect rightTriggerEffect;

        void OnEnable()
        {
            SetTriggerEffects();
        }

        void SetTriggerEffects()
        {
            sampleGamepad.gamepadDevice.SetTriggerEffect(leftTriggerEffect.GetEffect(TriggerEffectMask.L2));
            sampleGamepad.gamepadDevice.SetTriggerEffect(rightTriggerEffect.GetEffect(TriggerEffectMask.R2));
        }
    }

    /// <summary>
    /// This class wraps all of the types of trigger modes that are possible on the Dualsense Controller
    /// and makes them serializable to be visible in the inspector.
    ///
    /// To use this in the inspector select the mode and then edit the properties of the corropsoinding param
    /// For example TriggerEffectMode.Feedback will use the feedbackParam for it's settings
    /// </summary>
    [Serializable]
    public struct TriggerEffect
    {
        [SerializeField] public TriggerEffectMode mode;
        [SerializeField] public TriggerEffectFeedbackParam feedbackParam;
        [SerializeField] public TriggerEffectWeaponParam weaponParam;
        [SerializeField] public TriggerEffectVibrationParam vibrationParam;
        [SerializeField] public TriggerEffectMultiplePositionFeedbackParam multiplePositionFeedbackParam;
        [SerializeField] public TriggerEffectSlopeFeedbackParam slopeFeedbackParam;
        [SerializeField] public TriggerEffectMultiplePositionVibrationParam multiplePositionVibrationParam;

        [Serializable]
        public struct TriggerEffectWeaponParam
        {
            [Range(2,7)] public byte startPosition; // Position where the stiffness of triger start changing (2~7).
            [Range(1,8)] public byte endPosition;   // Position where the stiffness of trigger finish changing (startPosition+1~8).
            [Range(0,8)] public byte strength;      // Strength of gun trigger (0~8 (0: Same as Off mode)).
        }

        // struct containing trigger effect for the vibration effect type.
        [Serializable]
        public struct TriggerEffectVibrationParam
        {
            [Range(0,9)] public byte position;      // Position where the motor arm start vibrating (0~9).
            [Range(0,8)] public byte amplitude;     // Vibration amplitude (0~8 (0: Same as Off mode)).
            [Range(0,255)] public byte frequency;   // Vibration frequency (0~255[Hz] (0: Same as Off mode)).
        }

        // struct containing trigger effect for the feedback effect type.
        [Serializable]
        public struct TriggerEffectFeedbackParam
        {
	        [Range(0,9)] public byte position;      // Position where the strength of target trigger start changing (0~9).
	        [Range(0,8)] public byte strength;      // Strength that the motor arm pushes back target trigger (0~8 (0: Same as Off mode)).
        }

        [Serializable]
        public struct TriggerEffectSlopeFeedbackParam
        {
            [Range(0,9)] public byte startPosition;     // Position where the strength of target triggerstart changing(0~endPosition)
	        [Range(0,9)] public byte endPosition;       // position where the strength of target trigger finish changing(startPosition+1~9)
	        [Range(1,8)] public byte startStrength;     // strength when trigger's position is startPosition(1~8).
	        [Range(1,8)] public byte endStrength;       // strength when trigger's position is endPosition(1~8).
        }

        [Serializable]
        public struct TriggerEffectMultiplePositionFeedbackParam
        {
	        [Range(0,8)] public byte [] strength; //Array that defines the strength of the feedback at each point in the triggers travel
        }

        [Serializable]
        public struct TriggerEffectMultiplePositionVibrationParam
        {
            [Range(0,255)] public byte frequency; // Vibration frequency (0~255[Hz] (0: Same as Off mode)).
	        [Range(0,8)] public byte [] ampltude; //Array that defines the ampltude of of the vibration at each point in the triggers travel
        }

        void GetTriggerEffectForTrigger(ref TriggerEffectCommand command)
        {
            command.mode = mode;
            switch (mode)
            {
                case TriggerEffectMode.Off:
                    break;
                case TriggerEffectMode.Feedback:
                    command.feedback.strength = feedbackParam.strength;
                    command.feedback.position = feedbackParam.position;
                    break;
                case TriggerEffectMode.Weapon:
                    command.weapon.startPosition = weaponParam.startPosition;
                    command.weapon.endPosition = weaponParam.endPosition;
                    command.weapon.strength = weaponParam.strength;
                    break;
                case TriggerEffectMode.Vibration:
                    command.vibration.position = vibrationParam.position;
                    command.vibration.amplitude = vibrationParam.amplitude;
                    command.vibration.frequency = vibrationParam.frequency;
                    break;
                case TriggerEffectMode.MultiplePositionFeedback:
                    command.multiplePositionFeedback.strength0 = multiplePositionFeedbackParam.strength[0];
                    command.multiplePositionFeedback.strength1= multiplePositionFeedbackParam.strength[1];
                    command.multiplePositionFeedback.strength2= multiplePositionFeedbackParam.strength[2];
                    command.multiplePositionFeedback.strength3= multiplePositionFeedbackParam.strength[3];
                    command.multiplePositionFeedback.strength4= multiplePositionFeedbackParam.strength[4];
                    command.multiplePositionFeedback.strength5= multiplePositionFeedbackParam.strength[5];
                    command.multiplePositionFeedback.strength6= multiplePositionFeedbackParam.strength[6];
                    command.multiplePositionFeedback.strength7= multiplePositionFeedbackParam.strength[7];
                    command.multiplePositionFeedback.strength8= multiplePositionFeedbackParam.strength[8];
                    command.multiplePositionFeedback.strength9= multiplePositionFeedbackParam.strength[9];
                    break;
                case TriggerEffectMode.SlopeFeedback:
                    command.slopeFeedback.startPosition = slopeFeedbackParam.startPosition;
                    command.slopeFeedback.endPosition = slopeFeedbackParam.endPosition;
                    command.slopeFeedback.startStrength = slopeFeedbackParam.startStrength;
                    command.slopeFeedback.endStrength = slopeFeedbackParam.endStrength;
                    break;
                case TriggerEffectMode.MultiplePositionVibration:
                    command.multiplePositionVibration.frequency = multiplePositionVibrationParam.frequency;
                    command.multiplePositionVibration.amplitude0 = multiplePositionVibrationParam.ampltude[0];
                    command.multiplePositionVibration.amplitude1 = multiplePositionVibrationParam.ampltude[1];
                    command.multiplePositionVibration.amplitude2 = multiplePositionVibrationParam.ampltude[2];
                    command.multiplePositionVibration.amplitude3 = multiplePositionVibrationParam.ampltude[3];
                    command.multiplePositionVibration.amplitude4 = multiplePositionVibrationParam.ampltude[4];
                    command.multiplePositionVibration.amplitude5 = multiplePositionVibrationParam.ampltude[5];
                    command.multiplePositionVibration.amplitude6 = multiplePositionVibrationParam.ampltude[6];
                    command.multiplePositionVibration.amplitude7 = multiplePositionVibrationParam.ampltude[7];
                    command.multiplePositionVibration.amplitude8 = multiplePositionVibrationParam.ampltude[8];
                    command.multiplePositionVibration.amplitude9 = multiplePositionVibrationParam.ampltude[9];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public TriggerEffectParam GetEffect(TriggerEffectMask mask)
        {
            TriggerEffectParam triggerEffectParam = new TriggerEffectParam(mask);
            if (mask.HasFlag(TriggerEffectMask.L2))
            {
                GetTriggerEffectForTrigger(ref triggerEffectParam.left);
            }

            if (mask.HasFlag(TriggerEffectMask.R2))
            {
                GetTriggerEffectForTrigger(ref triggerEffectParam.right);
            }

            return triggerEffectParam;
        }
    }
}
