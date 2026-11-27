using System;
using SquishyBump.Core;
using SquishyBump.UI.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SquishyBump.UI
{
    /// <summary>Chỉ hiển thị và phát yêu cầu; App/Match mới chuyển bước.</summary>
    public sealed class TutorialPanel : MonoBehaviour
    {
        private Text prompt, feedback;
        private Button next;
        private string fruit, badge, description;
        public string PromptText => prompt.text;
        public event Action SkipRequested, ClassDismissed;
        public void Init(string fruitName, string classBadge, string classDescription)
        {
            fruit = fruitName; badge = classBadge; description = classDescription;
            var canvas = UiFactory.Canvas("L1 Tutorial", 20).transform; canvas.SetParent(transform, false);
            var page = UiFactory.Page(canvas, "Tutorial");
            page.anchorMin = page.anchorMax = new Vector2(0, 0); page.pivot = new Vector2(0, 0);
            page.anchoredPosition = new Vector2(28, 28); page.sizeDelta = new Vector2(720, 330);
            prompt = UiFactory.Label(page, "", 28); prompt.GetComponent<LayoutElement>().minHeight = 110;
            feedback = UiFactory.Label(page, "", 24); feedback.GetComponent<LayoutElement>().minHeight = 36;
            next = UiFactory.Button(page, "Continue", () => ClassDismissed?.Invoke());
            UiFactory.Button(page, "Skip tutorial", () => SkipRequested?.Invoke());
            Show(TutorialStep.Move, true);
        }
        public void Show(TutorialStep step, bool dashReady, float cooldownSeconds = 0)
        {
            next.gameObject.SetActive(step == TutorialStep.ClassCard);
            switch (step)
            {
                case TutorialStep.Move: prompt.text = "1/5 - MOVE\nWASD: roll to the glowing marker."; break;
                case TutorialStep.Jump: prompt.text = "2/5 - JUMP\nSpace: jump and land."; break;
                case TutorialStep.Dash: prompt.text = "3/5 - DASH\nShift: dash toward the marker. Wait for cooldown before dashing again."; break;
                case TutorialStep.ClassCard: prompt.text = $"4/5 - {fruit}\n{badge} - {description}"; break;
                case TutorialStep.Push: prompt.text = "5/5 - PUSH THE TOY\nPush the orange toy off the mat. If you fall, try again."; break;
            }
            feedback.text = dashReady ? "Dash: READY - Match timer paused" : $"Dash: {cooldownSeconds:0.0} s - Match timer paused";
        }
    }
}
