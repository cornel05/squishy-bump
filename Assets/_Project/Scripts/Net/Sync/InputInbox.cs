using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Net.Sync
{
    /// <summary>
    /// Host side, one per remote fighter. Keeps each Sequence once (packets repeat the last three),
    /// drops corrupt commands, and hands out one command per host tick. A flood faster than 60 Hz
    /// only fills the bounded queue: the oldest commands are dropped, so a client cannot speed itself up.
    /// When starved, the last command repeats with no steering, keeping held buttons held.
    /// </summary>
    public sealed class InputInbox
    {
        /// <summary>About 100 ms of input at 60 Hz.</summary>
        public const int Capacity = 6;

        private readonly Queue<InputCommand> queue = new();
        private uint newestQueued;
        private bool any;
        private InputCommand last;

        /// <summary>Sequence of the last command handed to the motor (acknowledged in snapshots).</summary>
        public uint LastProcessed { get; private set; }
        public int Pending => queue.Count;

        public void Offer(IEnumerable<InputCommand> commands)
        {
            foreach (var c in commands)
            {
                if (!c.HasValidValues || any && c.Sequence <= newestQueued) continue;
                newestQueued = c.Sequence;
                any = true;
                queue.Enqueue(c);
                while (queue.Count > Capacity) queue.Dequeue();
            }
        }

        public InputCommand Take()
        {
            if (queue.Count > 0)
            {
                last = queue.Dequeue();
                LastProcessed = last.Sequence;
                return last;
            }
            return new InputCommand(last.Sequence, Vector2.zero, last.Jump, last.Dash, last.YawDegrees);
        }
    }
}
