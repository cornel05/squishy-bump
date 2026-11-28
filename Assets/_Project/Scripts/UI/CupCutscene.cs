using System;
using SquishyBump.UI.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SquishyBump.UI
{
    /// <summary>Storyboard Single Player B01/B09, chạy bằng thời gian thực.</summary>
    public sealed class CupCutscene : MonoBehaviour
    {
        private struct Shot
        {
            public string Heading, Subtitle, Art;
            public Shot(string heading, string subtitle, string art)
            {
                Heading = heading;
                Subtitle = subtitle;
                Art = art;
            }
        }

        private Shot[] shots;
        private Text heading, subtitle;
        private Image art;
        private RectTransform cast;
        private CanvasGroup picture;
        private Action finished;
        private GameObject previousSelection;
        private float secondsPerShot, elapsed;
        private int current = -1;
        private bool done;

        public static void Opening(Transform parent, Action finished)
        {
            Show(parent, "B01 Opening", 11f, null, finished, new[]
            {
                new Shot("Closing time", "The shop closes. Six squishies wake up and hop off their shelf.", "bg_start"),
                new Shot("Squishy Cup night", "The Golden Squish is waiting. Everyone squeezes in for a photo, then tumbles!", "medal_1"),
                new Shot("To the Training Ring", "Six squishies, five play areas. Who will stay on top to the end?", "banner_round_1")
            });
        }

        public static void Ending(Transform parent, string winner, int? winnerSlot, int levelsCleared)
        {
            var result = string.IsNullOrEmpty(winner) ? "The leaders share first place." : winner + " wins the Squishy Cup!";
            var cleared = levelsCleared == 5 ? "Five play areas completed. " : "";
            Show(parent, "B09 Ending", 7.5f, winnerSlot, null, new[]
            {
                new Shot("Cup result", cleared + result, winnerSlot.HasValue ? $"avatar_{winnerSlot.Value}" : "medal_1"),
                new Shot("One last photo", "The friends gather around the Golden Squish for one more photo.", "medal_1"),
                new Shot("See you next Squishy Cup!", "One little bump, one last tumble. At sunrise, everyone hops back onto the shelf.", "bg_start")
            });
        }

        private static void Show(Transform parent, string name, float duration, int? winnerSlot, Action onFinished, Shot[] sequence)
        {
            var canvas = UiFactory.Canvas(name, 100);
            canvas.transform.SetParent(parent, false);
            var player = canvas.gameObject.AddComponent<CupCutscene>();
            player.shots = sequence;
            player.secondsPerShot = duration / sequence.Length;
            player.finished = onFinished;
            player.Build(winnerSlot);
            player.RenderShot(0);
        }

        private void Build(int? winnerSlot)
        {
            var background = UiFactory.FullScreen(transform, "bg_start");
            background.color = new Color(.18f, .13f, .27f);
            if (background.sprite == null) background.color = new Color(.08f, .04f, .14f);
            background.raycastTarget = true;

            var imageLayer = UiFactory.Layer(transform, "Storyboard");
            picture = imageLayer.gameObject.AddComponent<CanvasGroup>();
            art = UiFactory.Place(imageLayer, "logo", new Vector2(.5f, .62f), Vector2.zero, new Vector2(760, 440));
            cast = UiFactory.Layer(imageLayer, "Six squishies");
            for (var slot = 0; slot < 6; slot++)
            {
                var size = winnerSlot == slot ? 112 : 86;
                UiFactory.Place(cast, $"avatar_{slot}", new Vector2(.5f, .34f),
                    new Vector2((slot - 2.5f) * 120, 0), new Vector2(size, size));
            }
            heading = MakeText(transform, "Shot title", .82f, 54, 94);
            subtitle = MakeText(transform, "Subtitle", .21f, 34, 112);

            var skip = UiFactory.Button(transform, "Skip cutscene", Finish);
            var rect = (RectTransform)skip.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .09f);
            rect.sizeDelta = new Vector2(290, 76);
            skip.navigation = new Navigation { mode = Navigation.Mode.None };
            if (EventSystem.current != null)
            {
                previousSelection = EventSystem.current.currentSelectedGameObject;
                skip.Select();
            }
        }

        private static Text MakeText(Transform parent, string name, float height, int size, float boxHeight)
        {
            var text = UiFactory.Outlined(UiFactory.Label(parent, "", size));
            text.name = name;
            text.raycastTarget = false;
            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(.14f, height);
            rect.anchorMax = new Vector2(.86f, height);
            rect.sizeDelta = new Vector2(0, boxHeight);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 24;
            text.resizeTextMaxSize = size;
            return text;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            var index = (int)(elapsed / secondsPerShot);
            if (index >= shots.Length)
            {
                Finish();
                return;
            }
            if (index != current) RenderShot(index);
            var progress = (elapsed - index * secondsPerShot) / secondsPerShot;
            picture.alpha = Mathf.Clamp01(Mathf.Min(progress, 1f - progress) * 8f);
            cast.anchoredPosition = new Vector2(0, Mathf.Sin(progress * Mathf.PI) * 28f);
        }

        private void RenderShot(int index)
        {
            current = index;
            var shot = shots[index];
            heading.text = shot.Heading;
            subtitle.text = shot.Subtitle;
            art.sprite = UiFactory.Kit(shot.Art);
            art.enabled = art.sprite != null;
        }

        private void Finish()
        {
            if (done) return;
            done = true;
            gameObject.SetActive(false);
            if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            Destroy(gameObject);
            finished?.Invoke();
        }
    }
}
