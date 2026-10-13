using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace CampusPulse
{
    public struct CameraCommand
    {
        public Vector2 Pan;
        public float Orbit, Zoom;
        public bool Reset;
    }

    public sealed class StrategicCamera : MonoBehaviour
    {
        public CinemachineCamera VirtualCamera;
        public Vector3 Overview = new Vector3(0, 0, 0);
        public float OverviewDistance = 105;
        public Vector2 Bounds = new Vector2(75, 65);
        public float Distance { get; private set; } = 105;
        public Vector3 Target { get; private set; }
        float yaw = -28, desiredDistance = 105, desiredYaw = -28;
        Vector3 desiredTarget;
        public void ResetView()
        {
            desiredTarget = Overview; desiredDistance = OverviewDistance; desiredYaw = -28;
        }
        public void Focus(FacilityView facility)
        {
            desiredTarget = facility.transform.position + facility.FocusOffset;
            desiredDistance = 46;
        }
        void Start()
        {
            ResetView(); Target = desiredTarget; Distance = desiredDistance;
        }
        public void Apply(CameraCommand command, float delta)
        {
            if (command.Reset) ResetView();
            var right = Quaternion.Euler(0, desiredYaw, 0) * Vector3.right;
            var forward = Quaternion.Euler(0, desiredYaw, 0) * Vector3.forward;
            desiredTarget += (right * command.Pan.x + forward * command.Pan.y) * delta * desiredDistance * .5f;
            desiredTarget = new Vector3(Mathf.Clamp(desiredTarget.x, -Bounds.x, Bounds.x), Mathf.Clamp(desiredTarget.y, 0, 12), Mathf.Clamp(desiredTarget.z, -Bounds.y, Bounds.y));
            desiredYaw += command.Orbit * delta * 65;
            desiredDistance = Mathf.Clamp(desiredDistance - command.Zoom * 6, 30, 170);
        }
        void LateUpdate()
        {
            var k = 1 - Mathf.Exp(-7 * Time.unscaledDeltaTime);
            Target = Vector3.Lerp(Target, desiredTarget, k);
            yaw = Mathf.LerpAngle(yaw, desiredYaw, k);
            Distance = Mathf.Lerp(Distance, desiredDistance, k);
            var rotation = Quaternion.Euler(48, yaw, 0);
            VirtualCamera.transform.SetPositionAndRotation(Target - rotation * Vector3.forward * Distance, rotation);
        }
        public static CameraCommand ReadDesktop(bool pointerBlocked)
        {
            var k = Keyboard.current; var m = Mouse.current;
            var command = new CameraCommand();
            if (k != null)
            {
                command.Pan = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
                command.Pan = Vector2.ClampMagnitude(command.Pan, 1);
                command.Orbit = (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0);
                command.Reset = k.homeKey.wasPressedThisFrame;
            }
            if (m != null && !pointerBlocked)
            {
                command.Zoom = m.scroll.ReadValue().y / 120f;
                if (m.rightButton.isPressed) command.Orbit += m.delta.ReadValue().x * .06f;
                if (m.middleButton.isPressed) command.Pan += -m.delta.ReadValue() * .035f;
            }
            return command;
        }
    }
}
