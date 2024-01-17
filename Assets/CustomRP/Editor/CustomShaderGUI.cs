using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private Object[] materials;
    private MaterialProperty[] properties;

    private bool showPresets;

    public override void OnGUI(
        MaterialEditor materialEditor, MaterialProperty[] properties
    )
    {
        base.OnGUI(materialEditor, properties);
        this.editor = materialEditor;
        this.materials = materialEditor.targets;
        this.properties = properties;

        EditorGUILayout.Space();
        this.TestShaderEffectPreset();

        EditorGUILayout.Space();
        this.showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets)
        {
            this.OpaquePreset();
            this.ClipPreset();
            this.FadePreset();
            if (this.HasPremultiplyAlpha)
            {
                this.TransparentPreset();
            }
        }
    }

    private void TestShaderEffectPreset()
    {
        if (GetProperty("_TestShaderEffect"))
        {
            if (PresetButton("Test Shader Effect 끄기"))
            {
                TestShaderEffect = false;
            }
        }
        else
        {
            if (PresetButton("Test Shader Effect 켜기"))
            {
                TestShaderEffect = true;
            }
        }

    }

    private void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }

    private void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

    private void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    private void TransparentPreset()
    {
        if (PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    private bool TestShaderEffect
    {
        set => SetProperty("_TestShaderEffect", "_TEST_SHADER_EFFECT", value);
    }

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    private bool PremultiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }

    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }

    private bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

    private RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in materials.Cast<Material>())
            {
                m.renderQueue = (int)value;
            }
        }
    }

    private bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            this.editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }

    private bool SetProperty(string name, float value)
    {
        // propertyIsMandatory 가 true 면 익셉션을 발생시킨다.
        var property = FindProperty(name, properties, propertyIsMandatory: false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    private bool HasProperty(string name) =>
        FindProperty(name, properties, propertyIsMandatory: false) != null;

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material m in materials.Cast<Material>())
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material m in materials.Cast<Material>())
            {
                m.DisableKeyword(keyword);
            }
        }
    }

    private void SetProperty(string name, string keyword, bool value)
    {
        if (this.SetProperty(name, value ? 1f : 0f))
        {
            this.SetKeyword(keyword, value);
        }
    }

    private bool GetProperty(string name)
    {
        var property = FindProperty(name, properties, propertyIsMandatory: false);
        return property != null && property.floatValue == 1f;
    }
}
