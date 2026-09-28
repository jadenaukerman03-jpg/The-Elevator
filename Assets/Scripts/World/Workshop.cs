using System.Collections.Generic;
using UnityEngine;

namespace TheElevator
{
    // Shared primitive construction keeps the prototype editable without imported assets.
    public sealed class Workshop
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        Material labelMaterial;
        Font labelFont;
        readonly List<Material> ownedMaterials = new List<Material>();
        public Material Own(Material material) { ownedMaterials.Add(material); return material; }
        public static readonly Color Ink = new Color(0.055f, 0.09f, 0.105f);
        public static readonly Color Steel = new Color(0.22f, 0.30f, 0.32f);
        public static readonly Color Cream = new Color(0.83f, 0.82f, 0.67f);
        public static readonly Color Yellow = new Color(0.98f, 0.66f, 0.16f);
        public static readonly Color Mint = new Color(0.26f, 0.87f, 0.69f);
        public static readonly Color Red = new Color(0.94f, 0.30f, 0.24f);

        public Material Mat(Color color)
        {
            if (materials.TryGetValue(color, out Material mat)) return mat;
            Shader shader = Shader.Find("Elevator/PrototypeSurface");
            if (!shader) shader = Shader.Find("Standard");
            mat = new Material(shader) { color = color, name = "Workshop " + color };
            materials.Add(color, mat);
            return mat;
        }

        public GameObject Shape(string name, Transform parent, Vector3 position, Vector3 scale,
            Color color, PrimitiveType type = PrimitiveType.Cube, bool solid = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if (!solid)
            {
                Collider col = go.GetComponent<Collider>();
                col.enabled = false;
                if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            }
            return go;
        }

        public Transform Group(string name, Transform parent, Vector3 position)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        public TextMesh Label(string text, Transform parent, Vector3 position, float size, Color color,
            float yaw = 0f, bool backing = true)
        {
            Transform t = Group(text, parent, position);
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            TextMesh mesh = t.gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            if (!labelMaterial)
            {
                labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelMaterial = new Material(Shader.Find("Elevator/WorldLabel")) { name = "Depth-tested world lettering" };
                labelMaterial.mainTexture = labelFont.material.mainTexture;
                Font.textureRebuilt += RefreshFontAtlas;
            }
            mesh.font = labelFont;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = labelMaterial;
            mesh.fontSize = 64;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            if(backing)
            {
                Bounds bounds=mesh.GetComponent<Renderer>().localBounds;
                Shape("Printed sign backing",t,bounds.center+Vector3.forward*.005f,new Vector3(Mathf.Max(.03f,bounds.size.x+.008f),Mathf.Max(.02f,bounds.size.y+.008f),.006f),color.grayscale<.35f?Cream:Ink,PrimitiveType.Cube,false);
            }
            return mesh;
        }

        public Light Lamp(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            Transform t = Group("Work light", parent, position);
            Light light = t.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        void RefreshFontAtlas(Font font)
        {
            if (font == labelFont && labelMaterial) labelMaterial.mainTexture = font.material.mainTexture;
        }

        public void Dispose()
        {
            Font.textureRebuilt -= RefreshFontAtlas;
            if (labelMaterial) { if (Application.isPlaying) Object.Destroy(labelMaterial); else Object.DestroyImmediate(labelMaterial); }
            foreach (Material mat in materials.Values) { if (Application.isPlaying) Object.Destroy(mat); else Object.DestroyImmediate(mat); }
            materials.Clear();
            foreach (Material mat in ownedMaterials) { if (Application.isPlaying) Object.Destroy(mat); else Object.DestroyImmediate(mat); }
            ownedMaterials.Clear();
        }
    }
}

