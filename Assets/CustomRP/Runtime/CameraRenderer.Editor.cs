using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    partial void DrawGizmos ();
    partial void DrawUnsupportedShaders ();
    partial void PrepareForSceneWindow ();
    partial void PrepareBuffer ();

#if UNITY_EDITOR

    string SampleName { get; set; }

    private static Material errorMaterial;

    private static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM"),
	};

    partial void DrawGizmos () {
		if (Handles.ShouldRenderGizmos()) {
			this.context.DrawGizmos(this.camera, GizmoSubset.PreImageEffects);
			this.context.DrawGizmos(this.camera, GizmoSubset.PostImageEffects);
		}
	}

    partial void DrawUnsupportedShaders () 
    {
        if (errorMaterial == null) 
        {
			errorMaterial = new (Shader.Find("Hidden/InternalErrorShader"));
		}
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(this.camera))
            {
                overrideMaterial = errorMaterial
            };
        for (int i = 1; i < legacyShaderTagIds.Length; i++) 
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
		var filteringSettings = FilteringSettings.defaultValue;
		this.context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings);
	}

    partial void PrepareForSceneWindow ()
    {
        if (this.camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(this.camera);
        }
    }

    partial void PrepareBuffer ()
    {
        Profiler.BeginSample("Editor Only");
        this.buffer.name = this.SampleName = this.camera.name;
        Profiler.EndSample();
    }

#else

    const string SampleName = bufferName;

#endif

}
