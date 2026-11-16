using System;
using SquishyBump.Core;
using SquishyBump.Core.Input;
using SquishyBump.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SquishyBump.App
{
    /// <summary>
    /// The only place that reads this peer's devices for gameplay: turns the Gameplay map into
    /// <see cref="InputCommand"/>s (yaw = camera heading) and feeds mouse look to the camera.
    /// </summary>
    public sealed class LocalPlayerInput : IDisposable
    {
        private readonly SquishyControls controls = new();
        private readonly CameraRig camera;
        private uint sequence;

        public LocalPlayerInput(CameraRig cameraRig, string bindingOverrides = null)
        {
            camera = cameraRig;
            // Saved rebinds (GDD 10.5); empty means the asset's defaults.
            if (!string.IsNullOrEmpty(bindingOverrides)) controls.asset.LoadBindingOverridesFromJson(bindingOverrides);
            controls.Gameplay.Enable();
        }

        /// <summary>While a menu is open the fruit gets no steering or buttons and the camera stays put.</summary>
        public bool Suspended { get; set; }

        /// <summary>Esc this frame (GDD 3.4).</summary>
        public bool MenuPressed => controls.Gameplay.Menu.WasPressedThisFrame();

        /// <summary>Per rendered frame: orbit the camera.</summary>
        public void UpdateLook()
        {
            if (camera != null && !Suspended) camera.Orbit(controls.Gameplay.Look.ReadValue<Vector2>());
        }

        /// <summary>Per host tick: the held state of every input.</summary>
        public InputCommand Sample() => Suspended
            ? new InputCommand(++sequence, Vector2.zero, false, false, camera != null ? camera.Yaw : 0f)
            : new InputCommand(++sequence,
                controls.Gameplay.Move.ReadValue<Vector2>(),
                controls.Gameplay.Jump.IsPressed(),
                controls.Gameplay.Dash.IsPressed(),
                camera != null ? camera.Yaw : 0f);

        public void Dispose()
        {
            controls.Gameplay.Disable();
            controls.Dispose();
        }
    }
}
