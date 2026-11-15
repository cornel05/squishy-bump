using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>
    /// Một mẫu input của người chơi hoặc bot cho một tick. Constructor kẹp Move về độ dài 1
    /// và đưa yaw về [0, 360) (GDD 10.4), nên host dựng lại lệnh từ gói mạng là đã được kẹp.
    /// Jump và Dash là trạng thái đang giữ ở tick đó: motor tự phát hiện cạnh nhấn.
    /// Host xử lý mỗi Sequence đúng một lần, vì mỗi gói lặp lại ba lệnh gần nhất.
    /// </summary>
    public readonly struct InputCommand
    {
        public InputCommand(uint sequence, Vector2 move, bool jump, bool dash, float yawDegrees)
        {
            Sequence = sequence;
            Move = Vector2.ClampMagnitude(move, 1f);
            Jump = jump;
            Dash = dash;
            YawDegrees = Mathf.Repeat(yawDegrees, 360f);
        }

        public uint Sequence { get; }
        public Vector2 Move { get; }
        public bool Jump { get; }
        public bool Dash { get; }
        public float YawDegrees { get; }

        // Chỉ loại NaN và vô cực. Phần đồng bộ của host còn phải kiểm tra sequence, tần suất và hồi chiêu.
        public bool HasValidValues => IsFinite(YawDegrees) && IsFinite(Move.x) && IsFinite(Move.y);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public interface IFighterMotor
    {
        void Step(InputCommand command, float fixedDeltaTime);
    }
}
