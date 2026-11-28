using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// A virtual gamepad paired to one player's PlayerInput, so a bot plays through the game's normal
    /// controller bindings (PlayerActionControls): left stick move, right stick look, right trigger fire,
    /// South jump, West reload / hold to buy, D-pad up/right/down/left for weapon slots 1-4.
    /// </summary>
    public class VirtualGamepad
    {
        public readonly Gamepad Device;
        public PlayerInput PairedTo { get; private set; }

        public Vector2 Move;
        public Vector2 Look;
        public bool Fire;
        public bool Jump;
        public bool West;
        /// <summary>Weapon slot to press this frame (0-3), or -1.</summary>
        public int DpadSlot = -1;

        public VirtualGamepad(string name)
        {
            Device = InputSystem.AddDevice<Gamepad>(name);
        }

        /// <summary>Pairs the pad with a player's PlayerInput and stops it switching to the keyboard or mouse.</summary>
        public void PairWith(PlayerInput input)
        {
            if (input == null || input == PairedTo) return;
            PairedTo = input;
            input.neverAutoSwitchControlSchemes = true;
            try { input.SwitchCurrentControlScheme("Gamepad", Device); }
            catch (System.Exception e) { Debug.LogWarning("[ZCBOT] Couldn't pair the virtual gamepad: " + e.Message); }

            // A PlayerInput that lost its input user (e.g. its devices were removed) can't switch schemes; restarting
            // it gives it a new user, then pair again.
            if (!System.Linq.Enumerable.Contains(input.devices, Device))
            {
                input.enabled = false;
                input.enabled = true;
                input.neverAutoSwitchControlSchemes = true;
                try { input.SwitchCurrentControlScheme("Gamepad", Device); }
                catch (System.Exception e) { Debug.LogWarning("[ZCBOT] Couldn't pair the virtual gamepad: " + e.Message); }
            }
        }

        /// <summary>Sends this frame's state to the Input System (stick values are pre-scaled for the stick deadzone).</summary>
        public void Send()
        {
            var state = new GamepadState
            {
                leftStick = ThroughDeadzone(Move),
                rightStick = ThroughDeadzone(Look),
                rightTrigger = Fire ? 1f : 0f,
            };
            if (Jump) state = state.WithButton(GamepadButton.South);
            if (West) state = state.WithButton(GamepadButton.West);
            switch (DpadSlot)
            {
                case 0: state = state.WithButton(GamepadButton.DpadUp); break;
                case 1: state = state.WithButton(GamepadButton.DpadRight); break;
                case 2: state = state.WithButton(GamepadButton.DpadDown); break;
                case 3: state = state.WithButton(GamepadButton.DpadLeft); break;
            }
            InputSystem.QueueStateEvent(Device, state);
            DpadSlot = -1;   // D-pad presses last one frame
        }

        public void Release()
        {
            Move = Look = Vector2.zero;
            Fire = Jump = West = false;
            DpadSlot = -1;
            if (Device != null && Device.added) InputSystem.QueueStateEvent(Device, new GamepadState());
        }

        public void Remove()
        {
            if (Device != null && Device.added) InputSystem.RemoveDevice(Device);
        }

        // Sticks have a radial deadzone (values below min read as 0, above max as 1). Map the value we want
        // the game to see back to the raw stick value that produces it.
        private static Vector2 ThroughDeadzone(Vector2 wanted)
        {
            float magnitude = Mathf.Clamp01(wanted.magnitude);
            if (magnitude < 0.001f) return Vector2.zero;
            float min = InputSystem.settings.defaultDeadzoneMin;
            float max = InputSystem.settings.defaultDeadzoneMax;
            float raw = magnitude >= 0.999f ? 1f : Mathf.Lerp(min, max, magnitude) + 0.001f;
            return wanted / wanted.magnitude * raw;
        }
    }
}
