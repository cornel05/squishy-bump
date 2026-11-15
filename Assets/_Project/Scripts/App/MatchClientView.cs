using System;
using System.Collections.Generic;
using SquishyBump.Core;
using SquishyBump.Net.Sync;
using SquishyBump.Presentation;
using Unity.Netcode;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>
    /// Client side of a match: uploads local input at 60 Hz (last three commands per packet) and shows every
    /// fighter from host snapshots, 100 ms behind (GDD 10.4). View fighters are kinematic and never simulated:
    /// the client writes no gameplay state. No prediction yet (SB-22), so the own fruit is interpolated too.
    /// </summary>
    public sealed class MatchClientView : IDisposable
    {
        private readonly GameplayBootstrap bootstrap;
        private readonly GameObject[] prefabs;
        private readonly CameraRig cameraRig;
        private readonly LocalPlayerInput input;
        private readonly SnapshotBuffer buffer;
        private readonly List<InputCommand> recent = new();
        private readonly Dictionary<ushort, GameObject> views = new();

        public MatchClientView(GameplayBootstrap bootstrap, GameObject[] prefabs, CameraRig cameraRig,
            LocalPlayerInput input, BalanceProfile balance)
        {
            this.bootstrap = bootstrap;
            this.prefabs = prefabs;
            this.cameraRig = cameraRig;
            this.input = input;
            buffer = new SnapshotBuffer(1f / balance.FixedStepHz, balance.InterpolationBufferSeconds);
            bootstrap.Sync.SnapshotReceived += OnSnapshot;
        }

        public IReadOnlyDictionary<ushort, GameObject> Views => views;
        public SnapshotBuffer Buffer => buffer;

        private void OnSnapshot(WorldSnapshot snapshot) => buffer.Add(snapshot, Time.time);

        public void FixedUpdate()
        {
            recent.Add(input.Sample());
            if (recent.Count > SyncCodec.CommandsPerPacket) recent.RemoveAt(0);
            bootstrap.Sync.SendInput(recent);
        }

        public void Update(float now)
        {
            var newest = buffer.Newest;
            if (newest == null) return;
            var own = (ushort)NetworkManager.Singleton.LocalClientId;
            foreach (var f in newest.Fighters)
            {
                if (!views.TryGetValue(f.Id, out var view))
                {
                    if (f.Fruit >= prefabs.Length) continue;
                    view = UnityEngine.Object.Instantiate(prefabs[f.Fruit], f.Position, Quaternion.identity);
                    view.GetComponent<Rigidbody>().isKinematic = true;
                    foreach (var c in view.GetComponents<Collider>()) c.enabled = false;
                    views[f.Id] = view;
                    if (f.Id == own && cameraRig != null) cameraRig.Follow(view.transform);
                }
                if (buffer.TrySample(f.Id, now, out var position, out _)) view.transform.position = position;
            }
        }

        public void Dispose()
        {
            bootstrap.Sync.SnapshotReceived -= OnSnapshot;
            foreach (var v in views.Values)
                if (v != null) UnityEngine.Object.Destroy(v);
        }
    }
}
