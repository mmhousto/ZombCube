using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_PS5
using UnityEngine.PS5;
using PSInput = UnityEngine.PS5.PS5Input;
#endif

#if UNITY_EDITOR && UNITY_PS5
using DualSenseGamepad = UnityEngine.InputSystem.PS5.DualSenseGamepadPC;
#elif UNITY_PS5
using DualSenseGamepad = UnityEngine.InputSystem.PS5.DualSenseGamepad;
#endif

namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    /// <summary>
    /// This class displays all of the buttons/triggers/touches when they are pressed by the player, they will lightup white
    /// when pressed. It will also show and set the color of the light bar when any of the Cross, Square, Circle or Triangle
    /// buttons are pressed
    /// </summary>
    public class GamepadDisplay : MonoBehaviour
    {
        [FormerlySerializedAs("gamepad")]
        [SerializeField] SampleGamepad sampleGamepad;

        [SerializeField] GameObject controllerModel;

        [Header("Buttons")]
        [SerializeField]
        ButtonVisuals visuals;
        [SerializeField]
        ButtonImages controlImages;

        [Header("Lightbar")]
        [SerializeField] Color startLightbarColor;
        [SerializeField] Color uninitalizedLightbarColor;
        [SerializeField] LightbarVisuals lightbarVisuals;

        [Header("Username")]
        [SerializeField] TextMesh usernameTextBox;

        //Colour Lerp Routine
        Coroutine m_LerpLightbarColorCR;

        public void Initalize()
        {
            //Init touch pad images so they start in the correct state without yet being performed.
            //(-1,-1) is the value supplied from the pad when the touchpad does not have a valid touch position
            UpdateTouchpadTouch(controlImages.touch0, new Vector2(-1,-1));
            UpdateTouchpadTouch(controlImages.touch1, new Vector2(-1,-1));

            sampleGamepad.gamepadControls.Main.PressTriangle.started += _ => { StartLerpToColor(Color.blue); };
            sampleGamepad.gamepadControls.Main.PressCircle.started += _ => { StartLerpToColor(Color.red); };
            sampleGamepad.gamepadControls.Main.PressCross.started += _ => { StartLerpToColor(Color.magenta); };
            sampleGamepad.gamepadControls.Main.PressSquare.started += _ => { StartLerpToColor(Color.green); };

            sampleGamepad.gamepadControls.Main.TouchpadTouch0.performed += context =>
            {
                UpdateTouchpadTouch(controlImages.touch0, context.ReadValue<Vector2>());
            };

            sampleGamepad.gamepadControls.Main.TouchpadTouch1.performed += context =>
            {
                UpdateTouchpadTouch(controlImages.touch1, context.ReadValue<Vector2>());
            };

            UpdateUsernameOutput();
            SetLightbarColor(uninitalizedLightbarColor);

#if UNITY_PS5_ENHANCED_INPUT_SYSTEM
            sampleGamepad.gamepadDevicePS5?.SetTiltCorrectionState(true);
#endif
        }

        void OnEnable()
        {
            SetLightbarColor(startLightbarColor);
            UpdateUsernameOutput();
        }

        void OnDisable()
        {
            UpdateUsernameOutput();
            SetLightbarColor(uninitalizedLightbarColor);
        }

        void Update()
        {
            UpdateButtons();
            UpdateStickPositions();
            UpdateTriggerColours();
            UpdateGyro();
        }


        public void UpdateUsernameOutput()
        {
            if (sampleGamepad.gamepadDevice == null)
            {
                usernameTextBox.text = "DISCONNECTED";
                return;
            }

            PSInput.RefreshUsersDetails(sampleGamepad.gamepadDevice.slotIndex);
            PSInput.LoggedInUser user = PSInput.GetUsersDetails(sampleGamepad.gamepadDevice.slotIndex);

            string connectionTypeStr = string.Empty;
            if (sampleGamepad.gamepadDevicePS5 != null)
            {
                #if UNITY_PS5_ENHANCED_INPUT_SYSTEM
                connectionTypeStr = sampleGamepad.gamepadDevicePS5.connectionType.ToString();
                #else
                PS5Input.GetPadControllerInformation(sampleGamepad.gamepadDevice.slotIndex, out _, out _, out _, out _, out _, out PS5Input.ConnectionType type);
                connectionTypeStr = type.ToString();
                #endif
            }

            usernameTextBox.text = $"{user.userName}\n{connectionTypeStr}";
        }

        void UpdateButtons()
        {
            //Control Pad (X,O,Sq,X)
            UpdateButtonDraw(controlImages.triangle, sampleGamepad.gamepadControls.Main.PressTriangle.IsPressed());
            UpdateButtonDraw(controlImages.circle, sampleGamepad.gamepadControls.Main.PressCircle.IsPressed());
            UpdateButtonDraw(controlImages.square, sampleGamepad.gamepadControls.Main.PressSquare.IsPressed());
            UpdateButtonDraw(controlImages.cross, sampleGamepad.gamepadControls.Main.PressCross.IsPressed());

            //Dpad
            Vector2 dpadValue = sampleGamepad.gamepadControls.Main.Dpad.ReadValue<Vector2>().normalized;
            const float dPadDeadzone = 0.2f;
            UpdateButtonDraw(controlImages.dPadUp, dpadValue.y > dPadDeadzone);
            UpdateButtonDraw(controlImages.dPadDown, dpadValue.y < -dPadDeadzone);
            UpdateButtonDraw(controlImages.dPadLeft, dpadValue.x < -dPadDeadzone);
            UpdateButtonDraw(controlImages.dPadRight, dpadValue.x > dPadDeadzone);

            //Extra buttons (options, start, share)
            UpdateButtonDraw(controlImages.share, sampleGamepad.gamepadControls.Main.PressShare.IsPressed());
            UpdateButtonDraw(controlImages.options, sampleGamepad.gamepadControls.Main.PressOptions.IsPressed());
            UpdateButtonDraw(controlImages.playstation, sampleGamepad.gamepadControls.Main.PressPlaystationButton.IsPressed());

            //Bumpers
            UpdateButtonDraw(controlImages.l1, sampleGamepad.gamepadControls.Main.LeftBumper.IsPressed());
            UpdateButtonDraw(controlImages.r1, sampleGamepad.gamepadControls.Main.RightBumper.IsPressed());

            //Press thumbsticks
            UpdateButtonDraw(controlImages.leftStick, sampleGamepad.gamepadControls.Main.PressLeftStick.IsPressed());
            UpdateButtonDraw(controlImages.rightStick, sampleGamepad.gamepadControls.Main.PressRightStick.IsPressed());

            UpdateButtonDraw(controlImages.touchpad, sampleGamepad.gamepadControls.Main.PressTouchpad.IsPressed());
        }

        void UpdateGyro()
        {
            if (sampleGamepad.gamepadDevice?.orientation == null)
            {
                return;
            }

            controllerModel.transform.localRotation = sampleGamepad.gamepadDevice.orientation.ReadValue();
        }

        void UpdateStickPositions()
        {
            UpdateStickPosition(controlImages.leftStick, sampleGamepad.gamepadControls.Main.LeftStickPos);
            UpdateStickPosition(controlImages.rightStick, sampleGamepad.gamepadControls.Main.RightStickPos);
        }

        void UpdateStickPosition(SpriteRenderer stickSprite, InputAction control)
        {
            const float moveScale = 0.4f;
            Vector2 controlPos = control.ReadValue<Vector2>();
            stickSprite.transform.localPosition = new Vector3(controlPos.x, controlPos.y) * moveScale;
        }

        void UpdateTriggerColours()
        {
            UpdateTriggerColour(controlImages.l2, sampleGamepad.gamepadControls.Main.LeftTrigger);
            UpdateTriggerColour(controlImages.r2, sampleGamepad.gamepadControls.Main.RightTrigger);
        }

        void UpdateTriggerColour(SpriteRenderer stickSprite, InputAction control)
        {
            float controlPos = control.ReadValue<float>();
            stickSprite.color = visuals.triggerGradient.Evaluate(controlPos);
        }

        void UpdateTouchpadTouch(SpriteRenderer touchSprite, Vector2 touchPos)
        {
            //Ignore -1, -1 values as they are used when the touch pad is not being touched
            if (touchPos.x < 0 || touchPos.y < 0)
            {
                touchSprite.enabled = false;
                return;
            }

            //Remap range of 0 to 1 from touch position to the size of the touch pad in local space so the touches
            //on the image are in the correct position
            const float touchPadXSize = 3.57f;
            const float touchpadYSize = 1.35f;
            float x = touchPos.x.Remap(0, 1, 0, touchPadXSize);
            float y = touchPos.y.Remap(0, 1, 0, touchpadYSize);

            touchSprite.enabled = true;
            touchSprite.transform.localPosition = new Vector3(x, -y, -0.1f);

        }

        void StartLerpToColor(Color newColour)
        {
            const float timeToLerp = 0.5f;
            if (m_LerpLightbarColorCR != null)
            {
                StopCoroutine(m_LerpLightbarColorCR);
            }

            m_LerpLightbarColorCR = StartCoroutine(LerpToColor(newColour, timeToLerp));
        }

        IEnumerator LerpToColor(Color newColour, float time)
        {
            float elapsedTime = 0f;
            time = Mathf.Max(0f, time);
            Color startColour = sampleGamepad.gamepadDevice.lightBarColor;
            while (elapsedTime < time)
            {
                SetLightbarColor(Color.Lerp(startColour, newColour, elapsedTime / time));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void SetLightbarColor(Color color)
        {
            lightbarVisuals.SetColor(color);
            sampleGamepad.gamepadDevice?.SetLightBarColor(color);
        }

        void UpdateButtonDraw(SpriteRenderer image, bool isPressed)
        {
            if (image == null || visuals == null)
            {
                return;
            }
            image.color = isPressed ? visuals.inputOn : visuals.inputOff;
        }
    }
}
