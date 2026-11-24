using System;
using System.Collections.Generic;
using System.Linq;
using SquishyBump.Core;
using SquishyBump.Net.Sync;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using SquishyBump.UI;
using Unity.Netcode;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>Adapter scene LOB01. Host mô phỏng avatar và kiểm tra pick volumes, client chỉ gửi input/ready.</summary>
    public sealed class LobbyController : MonoBehaviour
    {
        private sealed class Avatar
        { public int Fruit; public GameObject Object; public PlayerMotor Motor; public Rigidbody Body; public float Radius; }
        private GameplayBootstrap app;
        private MatchSetup setup;
        private bool single;
        private string address;
        private Action<LobbyState> launch;
        private Action leave;
        private LobbyCoordinator coordinator;
        private LocalPlayerInput input;
        private CameraRig rig;
        private LobbyPanel panel;
        private Transform layout, desk;
        private BoxCollider[] picks;
        private Bounds floor;
        private SnapshotBuffer buffer;
        private readonly List<InputCommand> recent = new();
        private readonly Dictionary<ulong, Avatar> avatars = new();
        private readonly Dictionary<ulong, InputInbox> inboxes = new();
        private Material neutralMaterial;
        private bool launching;
        public LobbyCoordinator Coordinator => coordinator;
        public IReadOnlyDictionary<ulong, PlayerMotor> Motors => avatars.ToDictionary(p => p.Key, p => p.Value.Motor);
        public void Init(GameplayBootstrap owner, MatchSetup data, bool isSingle, string hostAddress,
            Action<LobbyState> onLaunch, Action onLeave)
        { app = owner; setup = data; single = isSingle; address = hostAddress; launch = onLaunch; leave = onLeave; }
        private void Start()
        {
            if (setup.Lobby == null || setup.Lobby.WalkingClass == null)
                throw new InvalidOperationException("Run ProjectScaffold.Run to assign LobbyProfile/WalkingClass.");
            layout = gameObject.scene.GetRootGameObjects().Single(g => g.name == "Store aisle").transform;
            floor = layout.Find("Floor").GetComponent<Collider>().bounds; desk = layout.Find("Host desk");
            var stands = layout.Find("Fruit stands");
            picks = Enumerable.Range(0, stands.childCount).Select(i => stands.GetChild(i).Find("Pick volume").GetComponent<BoxCollider>()).ToArray();
            rig = MakeCamera(); input = new LocalPlayerInput(rig, app.Saved.Bindings);
            neutralMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            neutralMaterial.SetColor("_BaseColor", Color.gray);
            panel = gameObject.AddComponent<LobbyPanel>(); panel.Init(layout.Find("Info board"), layout.Find("Controls board"), single,
                setup.Levels.Select(level => level.ArenaName).ToArray());
            MakeDisplays(stands);
            coordinator = new LobbyCoordinator(app.Session, app.Clock, app.LobbySync, Mathf.RoundToInt(setup.Balance.FixedStepHz),
                Mathf.RoundToInt(setup.Lobby.CountdownSeconds * setup.Balance.FixedStepHz), app.Settings.Rule, app.LobbySelection);
            coordinator.Changed += OnState; coordinator.StartRequested += Launch;
            panel.ReadyRequested += coordinator.SetReady;
            panel.ConfigureRequested += ConfigureAtDesk; panel.LeaveRequested += Leave;
            panel.RoomRequested += ConfigureRoomAtDesk; panel.PasswordRequested += SetPasswordAtDesk;
            panel.StartSingleRequested += StartSingle; panel.LookRequested += LockLook;
            app.Sync.InputReceived += OnInput; app.Sync.SnapshotReceived += OnSnapshot; app.Clock.Ticked += OnTick;
            buffer = new SnapshotBuffer(1f / setup.Balance.FixedStepHz, setup.Balance.InterpolationBufferSeconds);
            if (coordinator.State != null) OnState(coordinator.State);
            Cursor.lockState = CursorLockMode.None;
        }
        private CameraRig MakeCamera()
        {
            var go = new GameObject("Lobby follow camera"); go.SetActive(false); go.transform.SetParent(transform, false);
            var camera = go.AddComponent<CameraRig>();
            var output = gameObject.scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).First(c => c != null);
            camera.Configure(setup.Presentation, output); go.SetActive(true); return camera;
        }
        private void MakeDisplays(Transform stands)
        {
            for (var i = 0; i < setup.Roster.Length; i++)
            {
                var display = Instantiate(setup.Roster[i], transform);
                display.name = $"Display {setup.Roster[i].name}";
                display.transform.position = stands.GetChild(i).position + Vector3.up * (0.6f + setup.Roster[i].GetComponent<SphereCollider>().radius);
                foreach (var c in display.GetComponentsInChildren<Collider>()) c.enabled = false;
                foreach (var b in display.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;
                display.GetComponent<Rigidbody>().isKinematic = true;
                var definition = display.GetComponent<FighterProfileApplier>().Fruit;
                var fighterClass = definition.Class;
                panel.RegisterStand(i, stands.GetChild(i), definition.DisplayName, fighterClass.name, new LobbyPanel.ClassStats(
                    fighterClass.Mass, fighterClass.MaxSpeed, setup.Balance.DashDeltaV(fighterClass), fighterClass.Traction));
            }
        }
        private ulong LocalId => app.GetComponent<NetworkManager>().LocalClientId;
        private void OnState(LobbyState state)
        {
            app.SetLobbySelection(state);
            var chosen = state.Players.Where(p => p.Id == LocalId).Select(p => p.Fruit).DefaultIfEmpty(-1).First();
            if (chosen >= 0 && chosen < setup.Roster.Length && app.Saved.LastFruit != setup.Roster[chosen].name)
            { app.Saved.LastFruit = setup.Roster[chosen].name; app.WriteSave(); }
        }
        private void OnInput(ulong id, IReadOnlyList<InputCommand> commands)
        { if (inboxes.TryGetValue(id, out var inbox)) inbox.Offer(commands); }
        private void OnSnapshot(WorldSnapshot state) => buffer?.Add(state, Time.time);
        private void OnTick(uint tick)
        {
            if (app == null || !app.Session.CanSimulateGameplay || coordinator.Room.Started || launching) return;
            foreach (var id in avatars.Keys.Where(id => !coordinator.Room.Players.ContainsKey(id)).ToArray())
            { Destroy(avatars[id].Object); avatars.Remove(id); inboxes.Remove(id); }
            foreach (var slot in coordinator.Room.Players.OrderBy(p => p.Key))
            {
                var fruit = slot.Value.Fruit ?? -1;
                var avatar = EnsureAvatar(slot.Key, fruit, true);
                var cmd = slot.Key == 0 ? input.Sample() : inboxes[slot.Key].Take();
                app.TrySimulate(avatar.Motor, new InputCommand(cmd.Sequence, cmd.Move, false, false, cmd.YawDegrees));
                var p = avatar.Body.position;
                p.x = Mathf.Clamp(p.x, floor.min.x + avatar.Radius, floor.max.x - avatar.Radius);
                p.z = Mathf.Clamp(p.z, floor.min.z + avatar.Radius, floor.max.z - avatar.Radius);
                if (p.y < setup.Balance.KillPlaneY) { p = Spawn(slot.Key, avatar.Radius); avatar.Body.linearVelocity = Vector3.zero; }
                avatar.Body.position = p;
                for (var i = 0; i < picks.Length; i++)
                    if (picks[i].bounds.Contains(p)) coordinator.ClaimFromHostPosition(slot.Key, i);
            }
            var every = Mathf.Max(1, Mathf.RoundToInt(setup.Balance.InputRateHz / setup.Balance.SnapshotRateHz));
            if (tick % every == 0)
                app.Sync.BroadcastSnapshot(new WorldSnapshot(tick, 0, avatars.Select(p => new FighterSnapshot((ushort)p.Key,
                    p.Value.Fruit < 0 ? byte.MaxValue : (byte)p.Value.Fruit, p.Value.Body.position, p.Value.Body.linearVelocity, 0, 1, 0,
                    inboxes.TryGetValue(p.Key, out var box) ? box.LastProcessed : 0)).ToArray()));
        }
        private Vector3 Spawn(ulong id, float radius)
        {
            var ordered = coordinator.Room.Players.Keys.OrderBy(x => x).ToList();
            return setup.Lobby.Spawn(Mathf.Max(0, ordered.IndexOf(id))) + Vector3.up * radius;
        }
        private Avatar EnsureAvatar(ulong id, int fruit, bool host)
        {
            var position = Vector3.zero;
            if (avatars.TryGetValue(id, out var current))
            {
                if (current.Fruit == fruit) return current;
                position = current.Object.transform.position; current.Object.SetActive(false); Destroy(current.Object);
            }
            GameObject go;
            if (fruit >= 0)
            { go = Instantiate(setup.Roster[fruit], transform); }
            else
            {
                go = new GameObject("Unselected lobby avatar"); go.transform.SetParent(transform, false);
                go.AddComponent<SphereCollider>().radius = setup.Lobby.WalkingClass.Radius;
                go.AddComponent<Rigidbody>().mass = setup.Lobby.WalkingClass.Mass;
                go.AddComponent<PlayerMotor>().Configure(setup.Balance, setup.Lobby.WalkingClass);
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere); visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = Vector3.one * (2 * setup.Lobby.WalkingClass.Radius);
                var extra = visual.GetComponent<Collider>(); extra.enabled = false; Destroy(extra);
                visual.GetComponent<Renderer>().sharedMaterial = neutralMaterial;
            }
            var avatar = new Avatar { Object = go, Fruit = fruit, Motor = go.GetComponent<PlayerMotor>(),
                Body = go.GetComponent<Rigidbody>(), Radius = go.GetComponent<SphereCollider>().radius };
            go.layer = CollisionResolver.FighterLayer;
            UnityEngine.Physics.IgnoreLayerCollision(CollisionResolver.FighterLayer, CollisionResolver.FighterLayer, true);
            avatar.Body.position = current == null && host ? Spawn(id, avatar.Radius) : position;
            if (!host)
            {
                avatar.Motor.enabled = false; avatar.Body.isKinematic = true;
                foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            }
            avatars[id] = avatar;
            if (host && id != 0 && !inboxes.ContainsKey(id)) inboxes[id] = new InputInbox();
            if (id == LocalId) rig.Follow(go.transform);
            return avatar;
        }
        private void FixedUpdate()
        {
            if (input == null || app == null || app.Session.CanSimulateGameplay || app.Session.State != SessionState.Joined) return;
            var cmd = input.Sample(); recent.Add(new InputCommand(cmd.Sequence, cmd.Move, false, false, cmd.YawDegrees));
            if (recent.Count > SyncCodec.CommandsPerPacket) recent.RemoveAt(0);
            app.Sync.SendInput(recent);
        }
        private void Update()
        {
            if (input == null || app == null) return;
            if (input.MenuPressed) Cursor.lockState = CursorLockMode.None;
            if (Cursor.lockState == CursorLockMode.Locked) input.UpdateLook();
            if (!app.Session.CanSimulateGameplay && buffer.Newest != null)
            {
                var ids = buffer.Newest.Fighters.Select(f => (ulong)f.Id).ToHashSet();
                foreach (var id in avatars.Keys.Where(id => !ids.Contains(id)).ToArray()) { Destroy(avatars[id].Object); avatars.Remove(id); }
                foreach (var f in buffer.Newest.Fighters)
                {
                    var avatar = EnsureAvatar(f.Id, f.Fruit == byte.MaxValue ? -1 : f.Fruit, false);
                    if (buffer.TrySample(f.Id, Time.time, out var p, out _)) avatar.Object.transform.position = p;
                }
            }
            Announce();
            panel.Show(coordinator.State, LocalId, address, Mathf.RoundToInt(setup.Balance.FixedStepHz),
                i => setup.Roster[i].name, AtDesk());
        }
        private bool AtDesk() => app.Session.CanSimulateGameplay && avatars.TryGetValue(0, out var avatar) &&
            Vector3.Distance(avatar.Body.position, desk.position) <= setup.Lobby.DeskReach;
        private void ConfigureAtDesk(MatchRule? rule, int? level)
        {
            if (launching || !AtDesk()) return;
            var current = coordinator.State;
            coordinator.Configure(rule ?? current.Rule, single ? 5 : level ?? current.FinalLevel);
        }
        // Room options exist only for a LAN host standing at the desk; single player never opens a room.
        private void ConfigureRoomAtDesk(int? capacity, bool? isPublic)
        {
            if (launching || single || !AtDesk()) return;
            var current = coordinator.State;
            coordinator.ConfigureRoom(capacity ?? current.Capacity, isPublic ?? current.Public);
        }
        private void SetPasswordAtDesk(string password)
        { if (!launching && !single && AtDesk()) app.Session.Password = password; }
        private float nextBeacon;
        // Public LAN rooms announce themselves about once a second until the match starts.
        private void Announce()
        {
            if (single || launching || !app.Session.CanSimulateGameplay || Time.unscaledTime < nextBeacon) return;
            nextBeacon = Time.unscaledTime + 1f;
            var state = coordinator.State;
            if (state == null || !state.Public || state.Started || state.Players.Count >= state.Capacity) return;
            app.Discovery.Announce(Environment.MachineName, state.Players.Count, state.Capacity, state.Rule,
                !string.IsNullOrEmpty(app.Session.Password));
        }
        private void StartSingle()
        { if (single && app.Session.CanSimulateGameplay && coordinator.State.Players[0].Fruit >= 0) Launch(coordinator.State); }
        private void Launch(LobbyState state)
        { if (launching) return; launching = true; app.SetLobbySelection(state); launch(state); }
        private void Leave() => leave();
        private void LockLook() => Cursor.lockState = CursorLockMode.Locked;
        private void OnDestroy()
        {
            if (app != null)
            { app.Clock.Ticked -= OnTick; app.Sync.InputReceived -= OnInput; app.Sync.SnapshotReceived -= OnSnapshot; }
            if (coordinator != null) { coordinator.Changed -= OnState; coordinator.StartRequested -= Launch; coordinator.Dispose(); }
            input?.Dispose(); Destroy(neutralMaterial);
        }
    }
}
