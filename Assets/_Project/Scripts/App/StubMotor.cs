using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>Motor tạm để kiểm chứng đường truyền input trong phiên host.</summary>
    public sealed class StubMotor : IFighterMotor
    {
        public InputCommand LastCommand { get; private set; }
        public Vector3 Position { get; private set; }

        public void Step(InputCommand command, float fixedDeltaTime)
        {
            LastCommand = command;
            var direction = new Vector3(command.Move.x, 0f, command.Move.y);
            Position += Quaternion.Euler(0f, command.YawDegrees, 0f) * direction * fixedDeltaTime;
        }
    }
}
