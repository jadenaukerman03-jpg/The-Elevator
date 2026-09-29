using UnityEngine;

namespace TheElevator
{
    // A ring around the cursor that fills clockwise from the top. It shows throw strength while Q is held
    // (a full ring is the hardest throw) and how much of an item has been used up (a full ring is empty).
    public static class CursorGauge
    {
        const int Ticks = 60;

        public static void Draw(float fill, Color color)
        {
            fill = Mathf.Clamp01(fill);
            float scale = Mathf.Max(.75f, Screen.height / 1080f);
            float radius = 30 * scale, width = 4.5f * scale, length = 10 * scale;
            Vector2 centre = new Vector2(Screen.width * .5f, Screen.height * .5f);
            Matrix4x4 old = GUI.matrix;
            Color previous = GUI.color;
            int lit = Mathf.RoundToInt(fill * Ticks);
            for (int i = 0; i < Ticks; i++)
            {
                GUI.matrix = old;
                GUIUtility.RotateAroundPivot(i * 360f / Ticks, centre);
                GUI.color = i < lit ? color : new Color(0, 0, 0, .35f);
                GUI.DrawTexture(new Rect(centre.x - width * .5f, centre.y - radius - length * .5f, width, length), Texture2D.whiteTexture);
            }
            GUI.matrix = old;
            GUI.color = previous;
        }
    }
}
