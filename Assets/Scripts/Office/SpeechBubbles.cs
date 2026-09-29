using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    // Flat speech bubbles drawn on screen above whoever is talking, so they always face the player's view.
    // One bubble per speaker, one phrase at a time. Bubbles of people standing close together never overlap:
    // a bubble that would cover another is lifted above it, with a stem down to its own speaker's head.
    // Calm speech is a white bubble, upset speech is warm orange, and furious yelling is red with bold text.
    public static class SpeechBubbles
    {
        const float Range = 18, HeadClearance = .62f;
        static Texture2D rounded;
        static GUIStyle style, frame;
        static readonly List<KeyValuePair<float, OfficeEmployee>> visible = new List<KeyValuePair<float, OfficeEmployee>>();

        public static void Draw(Camera view, List<OfficeEmployee> employees)
        {
            if (!view || Event.current.type != EventType.Repaint) return;
            Prepare();
            visible.Clear();
            Vector3 eye = view.transform.position;
            foreach (OfficeEmployee employee in employees)
            {
                if (!employee || !employee.PhraseShowing) continue;
                Vector3 anchor = Anchor(employee);
                float distance = Vector3.Distance(eye, anchor);
                if (distance > Range || view.WorldToScreenPoint(anchor).z <= 0) continue;
                // Only people you can actually see: walls hide their bubbles.
                if (Physics.Linecast(eye, anchor, ~((1 << 2) | (1 << 8)), QueryTriggerInteraction.Ignore)) continue;
                visible.Add(new KeyValuePair<float, OfficeEmployee>(distance, employee));
            }
            // Lay out nearest first (they keep their natural spot), then lift anything that would overlap.
            visible.Sort((a, b) => a.Key.CompareTo(b.Key));
            placed.Clear();
            foreach (var entry in visible)
            {
                Rect box = Measure(view, entry.Value, entry.Key, out Vector2 anchor, out GUIContent content, out float scale);
                for (int pass = 0; pass < 8; pass++)
                {
                    bool moved = false;
                    foreach (Placed other in placed)
                        if (Grow(other.Box, 4).Overlaps(box)) { box.y = other.Box.y - box.height - 8 * scale; moved = true; }
                    if (!moved) break;
                }
                placed.Add(new Placed { Employee = entry.Value, Box = box, Anchor = anchor, Content = content, Scale = scale });
            }
            // Draw far to near, so nearer bubbles sit on top.
            for (int i = placed.Count - 1; i >= 0; i--) Draw(placed[i]);
        }

        struct Placed { public OfficeEmployee Employee; public Rect Box; public Vector2 Anchor; public GUIContent Content; public float Scale; }
        static readonly List<Placed> placed = new List<Placed>();
        static Rect Grow(Rect r, float by) { return new Rect(r.x - by, r.y - by, r.width + by * 2, r.height + by * 2); }

        static Vector3 Anchor(OfficeEmployee employee)
        {
            Transform head = employee.Robot ? employee.Robot.Head : null;
            return (head ? head.position : employee.transform.position + Vector3.up * 1.35f) + Vector3.up * HeadClearance;
        }

        static void Style(OfficeVoice.Tone tone, float scale)
        {
            style.fontSize = Mathf.RoundToInt((tone == OfficeVoice.Tone.Yelling ? 21 : 18) * scale);
            style.fontStyle = tone == OfficeVoice.Tone.Yelling ? FontStyle.Bold : FontStyle.Normal;
            style.padding = new RectOffset(Mathf.RoundToInt(12 * scale), Mathf.RoundToInt(12 * scale), Mathf.RoundToInt(8 * scale), Mathf.RoundToInt(8 * scale));
        }

        // Where a bubble would naturally go (just above the head) and how big it is.
        static Rect Measure(Camera view, OfficeEmployee employee, float distance, out Vector2 anchor, out GUIContent content, out float scale)
        {
            Vector3 screen = view.WorldToScreenPoint(Anchor(employee));
            float ui = Mathf.Max(.75f, Screen.height / 1080f);
            scale = Mathf.Clamp(6f / Mathf.Max(1, distance), .55f, 1.15f) * ui;
            OfficeVoice.Tone tone = employee.SpeechTone;
            Style(tone, scale);
            content = new GUIContent(tone == OfficeVoice.Tone.Yelling ? employee.Speech.ToUpperInvariant() : employee.Speech);
            float width = Mathf.Min(style.CalcSize(content).x, 300 * scale);
            float height = style.CalcHeight(content, width);
            anchor = new Vector2(screen.x, Screen.height - screen.y);
            return new Rect(anchor.x - width * .5f, anchor.y - height - 12 * scale, width, height);
        }

        static void Draw(Placed bubble)
        {
            OfficeEmployee employee = bubble.Employee;
            float scale = bubble.Scale;
            OfficeVoice.Tone tone = employee.SpeechTone;
            Style(tone, scale);
            GUIContent content = bubble.Content;
            Rect box = bubble.Box;
            float x = bubble.Anchor.x, y = bubble.Anchor.y;
            float tail = 12 * scale;
            Color fill = tone == OfficeVoice.Tone.Yelling ? new Color(1f, .78f, .74f) : tone == OfficeVoice.Tone.Upset ? new Color(1f, .9f, .74f) : new Color(.99f, .99f, .97f);
            Color edge = tone == OfficeVoice.Tone.Yelling ? new Color(.75f, .12f, .1f) : tone == OfficeVoice.Tone.Upset ? new Color(.85f, .5f, .15f) : new Color(.2f, .22f, .26f);
            // Fade out over the last moment of the line.
            float alpha = Mathf.Clamp01((employee.PhraseUntil - Time.time) * 4);
            Color old = GUI.color;
            // Outline, body and a little tail pointing at the speaker's head.
            float border = Mathf.Max(1.5f, 2.5f * scale);
            GUI.color = new Color(edge.r, edge.g, edge.b, alpha);
            // Lifted above a neighbour's bubble: a stem runs down to this speaker's head.
            if (box.yMax < y - tail - 1) GUI.DrawTexture(new Rect(x - border * .6f, box.yMax, border * 1.2f, y - tail - box.yMax), Texture2D.whiteTexture);
            GUI.Box(new Rect(box.x - border, box.y - border, box.width + border * 2, box.height + border * 2), GUIContent.none, frame);
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45, new Vector2(x, y - tail * 1.1f));
            GUI.DrawTexture(new Rect(x - tail * .75f, y - tail * 1.85f, tail * 1.5f, tail * 1.5f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = new Color(fill.r, fill.g, fill.b, alpha);
            GUIUtility.RotateAroundPivot(45, new Vector2(x, y - tail * 1.1f));
            GUI.DrawTexture(new Rect(x - tail * .75f + border * .7f, y - tail * 1.85f + border * .7f, tail * 1.5f - border * 1.4f, tail * 1.5f - border * 1.4f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.Box(box, GUIContent.none, frame);
            style.normal.textColor = new Color(.1f, .1f, .12f, alpha);
            GUI.color = Color.white;
            GUI.Label(box, content, style);
            GUI.color = old;
        }

        static void Prepare()
        {
            if (!rounded)
            {
                // A white rounded rectangle, stretched with fixed corners.
                const int size = 32, radius = 12;
                rounded = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Speech bubble", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Max(radius - x - .5f, x + .5f - (size - radius))), dy = Mathf.Max(0, Mathf.Max(radius - y - .5f, y + .5f - (size - radius)));
                        float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                        rounded.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                rounded.Apply();
            }
            if (frame == null)
            {
                // Nine-slice: the corners keep their radius at any bubble size.
                frame = new GUIStyle { border = new RectOffset(12, 12, 12, 12) };
                frame.normal.background = rounded;
            }
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = false };
                style.border = new RectOffset(12, 12, 12, 12);
            }
        }
    }
}
