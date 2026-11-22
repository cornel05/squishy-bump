#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SquishyBump.Tests.PlayMode
{
    public sealed class RankArenaTests
    {
        private const string Root = "Assets/_Project";
        private GameObject matchObject;
        private GameplayBootstrap app;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1;
            if (matchObject != null) Object.Destroy(matchObject);
            foreach (var owner in Object.FindObjectsByType<GameplayBootstrap>(FindObjectsSortMode.None))
            {
                owner.Session.Leave();
                Object.Destroy(owner.gameObject);
            }
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/MainMenu.unity", LoadSceneMode.Single);
        }

        private static IEnumerator Wait(System.Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Điều kiện runtime không đạt trong thời gian chờ.");
        }

        [UnityTest]
        public IEnumerator T05_Partial_ContactWithoutWobble_CreditsFall_RespawnsAndProtectsFighter()
        {
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Boot.unity", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync($"{Root}/Scenes/Levels/L1.unity", LoadSceneMode.Additive);
            app = Object.FindAnyObjectByType<GameplayBootstrap>();
            app.Settings.Rule = MatchRule.Rank;
            // Lượt kiểm thử không ghi save của người dùng.
            app.Store = null;
            var bounds = Object.FindAnyObjectByType<TrainingRingBounds>();
            var balance = AssetDatabase.LoadAssetAtPath<BalanceProfile>($"{Root}/Data/Balance/BalanceProfile.asset");
            matchObject = new GameObject("Rank runtime test");
            matchObject.SetActive(false);
            var host = matchObject.AddComponent<MatchHost>();
            var roster = new[] { "Apple", "Watermelon", "Peach" }.Select(name =>
                AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Fighters/{name}.prefab")).ToArray();
            host.Configure(app, balance, bounds, Object.FindAnyObjectByType<RingDrop>(), null, roster, 1, true);
            matchObject.SetActive(true);
            Time.timeScale = 4;
            yield return Wait(() => host.Director != null && host.Director.Phase == MatchPhase.Playing);
            var player = host.Fighters[0]; var victim = host.Fighters[1];
            var resolver = matchObject.GetComponent<CollisionResolver>();
            var a = player.Motor.GetComponent<Rigidbody>(); var b = victim.Motor.GetComponent<Rigidbody>();
            a.position = new Vector3(0, 0.8f, 0); b.position = new Vector3(1, 0.8f, 0);
            a.linearVelocity = b.linearVelocity = Vector3.zero;
            // Tiếp xúc nhẹ, cùng vận tốc: không có hit >= 30 N·s nhưng vẫn là chạm cuối.
            yield return new WaitForFixedUpdate();
            Assert.That(resolver.GetWobble(victim.Id), Is.Zero);
            resolver.AddWobble(victim.Id, 5);
            b.position = new Vector3(0, -6, 0);
            yield return Wait(() => host.Director.IsRespawning(victim.Id));
            Assert.That(victim.Out, Is.True);
            Assert.That(host.Director.Lives(victim.Id), Is.EqualTo(2));
            Assert.That(host.Director.Scores.Points(player.Id), Is.EqualTo(1));
            Assert.That(victim.Motor.gameObject.activeSelf, Is.False);
            yield return Wait(() => !victim.Out);
            Assert.That(victim.Motor.transform.position.y, Is.GreaterThan(bounds.KillPlaneY),
                $"Transform hồi sinh còn ở điểm rơi: body={b.position}, transform={victim.Motor.transform.position}");
            Assert.That(host.Director.IsInvulnerable(victim.Id), Is.True);
            Assert.That(resolver.GetWobble(victim.Id), Is.Zero);
            Assert.That(victim.Motor.DashReady, Is.True);
            Assert.That(victim.Motor.IsStunned, Is.False);
            Assert.That(new Vector2(b.position.x, b.position.z).magnitude,
                Is.LessThanOrEqualTo(bounds.OuterRadius - balance.RespawnEdgeMarginMeters));
            var adapter = new FighterPhysicsAdapter(resolver); adapter.Add(victim.Id, victim.Motor);
            var before = b.linearVelocity;
            adapter.AddImpulse(victim.Id, Vector3.right * 100); adapter.AddWobble(victim.Id, 2);
            Assert.That(b.linearVelocity, Is.EqualTo(before));
            Assert.That(resolver.GetWobble(victim.Id), Is.Zero);
            yield return Wait(() => !host.Director.IsInvulnerable(victim.Id));
            // Motor nhận trạng thái bảo vệ mới ở đầu tick kế tiếp.
            yield return new WaitForFixedUpdate();
            Assert.That(victim.Out, Is.False,
                $"Quả vừa hồi sinh lại rơi: lives={host.Director.Lives(victim.Id)}, body={b.position}, ticks={host.Director.ElapsedTicks}");
            Assert.That(victim.Motor.Invulnerable, Is.False, "Cờ physics đã hết bảo vệ cùng Director");
            before = b.linearVelocity;
            adapter.AddImpulse(victim.Id, Vector3.right * 100);
            Assert.That(b.linearVelocity.x, Is.GreaterThan(before.x));
        }
    }
}
#endif
