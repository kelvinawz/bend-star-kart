using UnityEngine;
using UnityEngine.InputSystem;

namespace BendStarKart
{
    public sealed class KartInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputAction accelerate;
        private InputAction brake;
        private InputAction steer;
        private InputAction drift;
        private InputAction nitro;
        private InputAction reset;
        private InputAction pause;
        private InputAction tuning;

        public float Throttle => accelerate == null ? 0f : accelerate.ReadValue<float>();
        public float Brake => brake == null ? 0f : brake.ReadValue<float>();
        public float Steer => steer == null ? 0f : steer.ReadValue<float>();
        public bool Drift => drift != null && drift.IsPressed();
        public bool ThrottlePressed => accelerate != null && accelerate.WasPressedThisFrame();
        public bool ThrottleReleased => accelerate != null && accelerate.WasReleasedThisFrame();
        public bool NitroPressed => nitro != null && nitro.WasPressedThisFrame();
        public bool ResetPressed => reset != null && reset.WasPressedThisFrame();
        public bool PausePressed => pause != null && pause.WasPressedThisFrame();
        public bool TuningPressed => tuning != null && tuning.WasPressedThisFrame();

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("KartInput 缺少 Input Actions 资源；请重新生成测试赛道。", this);
                enabled = false;
                return;
            }
            accelerate = actions.FindAction("Drive/Accelerate", true);
            brake = actions.FindAction("Drive/Brake", true);
            steer = actions.FindAction("Drive/Steer", true);
            drift = actions.FindAction("Drive/Drift", true);
            nitro = actions.FindAction("Drive/Nitro", true);
            reset = actions.FindAction("Drive/Reset", true);
            pause = actions.FindAction("Drive/Pause", true);
            tuning = actions.FindAction("Drive/Tuning", true);
        }

        private void OnEnable() { if (actions != null) actions.Enable(); }
        private void OnDisable() { if (actions != null) actions.Disable(); }

        public void SetActions(InputActionAsset asset) { actions = asset; }
    }
}
