using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>Một mẫu input của người chơi hoặc bot, trước khi host kiểm tra và xử lý.</summary>
    public readonly struct InputCommand
    {
        public InputCommand(uint sequence, Vector2 move, bool jump, bool dash, float yawDegrees)
        {
            Sequence = sequence;
            Move = move;
            Jump = jump;
            Dash = dash;
            YawDegrees = yawDegrees;
        }

        public uint Sequence { get; }
        public Vector2 Move { get; }
        public bool Jump { get; }
        public bool Dash { get; }
        public float YawDegrees { get; }

        // Phần đồng bộ của host còn phải kiểm tra sequence, tần suất và hồi chiêu.
        public bool HasValidValues =>
            !float.IsNaN(YawDegrees) && !float.IsInfinity(YawDegrees) &&
            !float.IsNaN(Move.x) && !float.IsInfinity(Move.x) &&
            !float.IsNaN(Move.y) && !float.IsInfinity(Move.y) && Move.sqrMagnitude <= 1f;
    }

    public interface IFighterMotor
    {
        void Step(InputCommand command, float fixedDeltaTime);
    }
}
