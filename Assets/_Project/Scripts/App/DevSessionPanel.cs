using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>
    /// Sandbox-only buttons to try LAN sync before the real menus (SB-31) and lobby land:
    /// Single Player, Host LAN, Join an address, Start match on the host.
    /// </summary>
    public sealed class DevSessionPanel : MonoBehaviour
    {
        [SerializeField] private GameplayBootstrap bootstrap;
        [SerializeField] private MatchHost match;
        [SerializeField] private ushort port = 7777;
        private string address = "127.0.0.1";

        private void OnGUI()
        {
            var session = bootstrap.Session;
            GUILayout.BeginArea(new Rect(10, 10, 260, 200), GUI.skin.box);
            GUILayout.Label($"Session: {session.State}  peers: {session.Peers.Count}");
            if (session.State == SessionState.Idle)
            {
                if (GUILayout.Button("Single Player") && session.Host(SessionMode.SinglePlayer)) match.StartMatch();
                if (GUILayout.Button("Host LAN")) session.Host(SessionMode.Lan);
                address = GUILayout.TextField(address);
                if (GUILayout.Button("Join")) session.Join(address, port);
            }
            else
            {
                if (session.State == SessionState.Hosting && !match.Started && GUILayout.Button("Start match"))
                    match.StartMatch();
                if (match.Started) GUILayout.Label($"Phase: {match.Director.Phase}");
                if (GUILayout.Button("Leave")) session.Leave();
            }
            GUILayout.EndArea();
        }
    }
}
