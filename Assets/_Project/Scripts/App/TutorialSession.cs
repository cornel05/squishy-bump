using System;
using SquishyBump.Core;
using SquishyBump.Match;
using SquishyBump.Physics;
using SquishyBump.Presentation;
using SquishyBump.UI;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>Wiring bài tập trước trận: không có Director, score, hazard hoặc roster bot.</summary>
    public sealed class TutorialSession : IDisposable
    {
        private const ulong PlayerId = 0, ToyId = ulong.MaxValue - 1;
        private readonly GameplayBootstrap app;
        private readonly TutorialProfile profile;
        private readonly TrainingRingBounds arena;
        private readonly PlayerMotor player, toy;
        private readonly CollisionResolver resolver;
        private readonly Func<InputCommand> sample;
        private readonly Action<bool> finished;
        private readonly GameObject root, marker;
        private readonly TutorialPanel panel;
        private readonly Material markerMaterial, toyMaterial;
        private readonly float playerRadius, toyRadius;
        private bool touchedToy, disposed;
        public TutorialFlow Flow { get; }
        public PlayerMotor Player => player;
        public PlayerMotor Toy => toy;
        public TutorialSession(GameplayBootstrap owner, TutorialProfile data, BalanceProfile balance,
            TrainingRingBounds bounds, GameObject prefab, CameraRig camera, Transform parent,
            Func<InputCommand> input, Action<bool> onFinished)
        {
            app = owner; profile = data; arena = bounds; sample = input; finished = onFinished;
            root = new GameObject("Tutorial props (không thuộc roster)"); root.transform.SetParent(parent, false);
            var fruit = UnityEngine.Object.Instantiate(prefab, root.transform);
            player = fruit.GetComponent<PlayerMotor>(); playerRadius = fruit.GetComponent<SphereCollider>().radius;
            ResetPlayer();
            if (camera != null) camera.Follow(player.transform);
            resolver = root.AddComponent<CollisionResolver>(); resolver.Configure(balance); resolver.Register(PlayerId, player);
            var ball = new GameObject("Đồ chơi tập màu cam");
            ball.transform.SetParent(root.transform, false);
            toyRadius = player.Class.Radius;
            ball.AddComponent<SphereCollider>().radius = toyRadius;
            var toyVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            toyVisual.transform.SetParent(ball.transform, false); toyVisual.transform.localScale = Vector3.one * (toyRadius * 2);
            var visualCollider = toyVisual.GetComponent<Collider>(); visualCollider.enabled = false; UnityEngine.Object.Destroy(visualCollider);
            var body = ball.AddComponent<Rigidbody>(); body.mass = player.Class.Mass;
            body.useGravity = true;
            toy = ball.AddComponent<PlayerMotor>(); toy.Configure(balance, player.Class);
            resolver.Register(ToyId, toy);
            toyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            toyMaterial.SetColor("_BaseColor", new Color(1, 0.55f, 0.15f));
            toyVisual.GetComponent<Renderer>().sharedMaterial = toyMaterial;
            ResetToy(); ball.SetActive(false);
            marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name = "Dấu tròn bài tập";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localScale = new Vector3(profile.MarkerRadius * 2, 0.01f, profile.MarkerRadius * 2);
            var markerCollider = marker.GetComponent<Collider>(); markerCollider.enabled = false; UnityEngine.Object.Destroy(markerCollider);
            markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            markerMaterial.SetColor("_BaseColor", new Color(1f, 0.718f, 0.012f)); marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            marker.transform.position = arena.transform.TransformPoint(profile.MoveMarker) + Vector3.up * 0.015f;
            var ui = new GameObject("Tutorial UI"); ui.transform.SetParent(root.transform, false);
            panel = ui.AddComponent<TutorialPanel>();
            var name = player.Class.name;
            var badge = name == "Heavy" ? "[H] Heavy" : name == "Light" ? "[L] Light" : "[M] Normal";
            var description = name == "Heavy" ? "More grip and a lower top speed." :
                name == "Light" ? "Fast, but easier to knock back." : "Balanced speed and grip.";
            panel.Init(prefab.name, badge, description);
            Flow = new TutorialFlow(() => app.Session.CanSimulateGameplay);
            Flow.Changed += OnStep; panel.SkipRequested += Flow.Skip; panel.ClassDismissed += Flow.DismissClassCard;
            app.Clock.Ticked += OnTick; Cursor.lockState = CursorLockMode.None;
        }
        private void OnTick(uint tick)
        {
            if (disposed || !app.Session.CanSimulateGameplay) return;
            var beforeGrounded = player.Grounded; var beforeDash = player.IsDashing;
            var command = sample();
            // Thẻ class không nhận input gameplay; nút UI phát yêu cầu xác nhận riêng.
            if (Flow.Step == TutorialStep.ClassCard) command = new InputCommand(command.Sequence, Vector2.zero, false, false, command.YawDegrees);
            app.TrySimulate(player, command);
            if (toy.gameObject.activeSelf)
            {
                app.TrySimulate(toy, default);
                touchedToy |= (toy.transform.position - player.transform.position).sqrMagnitude <=
                    (playerRadius + toyRadius) * (playerRadius + toyRadius);
            }
            resolver.Step(Time.fixedDeltaTime);
            if (Outside(player.transform.position)) { ResetPlayer(); Flow.PlayerReset(); }
            var toyOut = toy.gameObject.activeSelf && Outside(toy.transform.position);
            if (toyOut && !touchedToy) ResetToy();
            var target = arena.transform.TransformPoint(profile.MoveMarker);
            var offset = player.transform.position - target; offset.y = 0;
            Flow.Observe(offset.sqrMagnitude <= profile.MarkerRadius * profile.MarkerRadius,
                beforeGrounded && command.Jump && player.GetComponent<Rigidbody>().linearVelocity.y > 0,
                player.Grounded, !beforeDash && player.IsDashing, toyOut && touchedToy);
            if (!disposed) panel.Show(Flow.Step, player.DashReady, player.DashCooldownTicks * Time.fixedDeltaTime);
        }
        private bool Outside(Vector3 p)
        {
            var d = p - arena.transform.position;
            return p.y < arena.KillPlaneY || d.x * d.x + d.z * d.z > arena.OuterRadius * arena.OuterRadius;
        }
        private void ResetPlayer() => player.ResetForRespawn(arena.transform.TransformPoint(profile.TrainingPoint) + Vector3.up * playerRadius);
        private void ResetToy() => toy.ResetForRespawn(arena.transform.position +
            Vector3.forward * (arena.OuterRadius - profile.ToyInset) + Vector3.up * toyRadius);
        private void OnStep(TutorialStep step)
        {
            marker.SetActive(step == TutorialStep.Move || step == TutorialStep.Jump || step == TutorialStep.Dash);
            if (step == TutorialStep.Jump) markerMaterial.SetColor("_BaseColor", new Color(0.2f, 0.95f, 0.55f));
            if (step == TutorialStep.Dash)
            {
                marker.transform.position = arena.transform.TransformPoint(profile.DashMarker) + Vector3.up * 0.015f;
                markerMaterial.SetColor("_BaseColor", new Color(1f, 0.718f, 0.012f));
            }
            if (step == TutorialStep.Push)
            {
                toy.gameObject.SetActive(true); ResetToy();
                player.ResetForRespawn(toy.transform.position - Vector3.forward * profile.ApproachGap + Vector3.up * (playerRadius - toyRadius));
                resolver.ResetWobble(PlayerId); resolver.ResetWobble(ToyId);
            }
            if (Flow.Finished) finished(step == TutorialStep.Skipped);
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            app.Clock.Ticked -= OnTick; Flow.Changed -= OnStep;
            panel.SkipRequested -= Flow.Skip; panel.ClassDismissed -= Flow.DismissClassCard;
            // Collider biến mất ngay trước khi roster thật được spawn ở frame kế tiếp.
            root.SetActive(false); UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(markerMaterial); UnityEngine.Object.Destroy(toyMaterial);
        }
    }
}
