using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds an AnimatorController that blends a hand between the open and gripped poses recorded by
/// <see cref="HandPoseClipRecorder"/>. Doing this from code keeps the setup reproducible - the
/// controller can always be regenerated from the clips instead of being wired up by hand.
/// </summary>
static class HandAnimatorBuilder
{
    const string k_ClipFolder = "Assets/HandPoses";
    const string k_OutputFolder = "Assets/HandPoses";
    const string k_Parameter = "Grip";

    /// <summary>Parameter name Unity injects when it creates a blend tree for us.</summary>
    const string k_DefaultParameter = "Blend";

    [MenuItem("Tools/Hand Pose/Build Hand Animator")]
    static void Build()
    {
        var built = 0;
        foreach (var side in new[] { "L", "R" })
        {
            var openClip = LoadClip($"HandPose_Open_{side}");
            var gripClip = LoadClip($"HandPose_Grip_{side}");

            if (openClip == null || gripClip == null)
                continue;

            BuildController(side, openClip, gripClip);
            built++;
        }

        if (built == 0)
        {
            Debug.LogError(
                $"[HandPose] No clip pairs found in {k_ClipFolder}. Record HandPose_Open_L and " +
                "HandPose_Grip_L with Tools > Hand Pose > Clip Recorder first.");
            return;
        }

        AssetDatabase.SaveAssets();
    }

    static AnimationClip LoadClip(string name)
        => AssetDatabase.LoadAssetAtPath<AnimationClip>($"{k_ClipFolder}/{name}.anim");

    static void BuildController(string side, AnimationClip openClip, AnimationClip gripClip)
    {
        var path = $"{k_OutputFolder}/HandAnimator_{side}.controller";

        // Deleting and recreating would hand back a new GUID, which empties the Controller slot of every
        // Animator already pointing here. Re-running this must never break a scene that was working.
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null)
        {
            Retarget(existing, openClip, gripClip);
            return;
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter(k_Parameter, AnimatorControllerParameterType.Float);

        controller.CreateBlendTreeInController("Grip Blend", out var blendTree, 0);
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = k_Parameter;

        // Thresholds are assigned explicitly below, so keep Unity from redistributing them.
        blendTree.useAutomaticThresholds = false;
        blendTree.AddChild(openClip, 0f);
        blendTree.AddChild(gripClip, 1f);

        RemoveUnusedDefaultParameter(controller);

        EditorUtility.SetDirty(controller);
        Debug.Log($"[HandPose] Built {path}  ({openClip.name} 0 -> {gripClip.name} 1, parameter '{k_Parameter}').");
    }

    /// <summary>
    /// Points an existing controller's blend tree at the given clips, leaving the asset itself - and so
    /// every reference to it - untouched. Only needed when the clips were saved under new names; a clip
    /// re-recorded over itself already reaches the tree through its unchanged GUID.
    /// </summary>
    static void Retarget(AnimatorController controller, AnimationClip openClip, AnimationClip gripClip)
    {
        var blendTree = FindBlendTree(controller);
        if (blendTree == null)
        {
            Debug.LogError(
                $"[HandPose] {controller.name} has no blend tree to update. Delete it and run this again " +
                "to rebuild from scratch - but reassign it on any Animator that used it.");
            return;
        }

        blendTree.children = new[]
        {
            new ChildMotion { motion = openClip, threshold = 0f, timeScale = 1f },
            new ChildMotion { motion = gripClip, threshold = 1f, timeScale = 1f },
        };

        EditorUtility.SetDirty(controller);
        Debug.Log($"[HandPose] Updated {controller.name} in place ({openClip.name} 0 -> {gripClip.name} 1).");
    }

    static BlendTree FindBlendTree(AnimatorController controller)
    {
        foreach (var layer in controller.layers)
        {
            foreach (var child in layer.stateMachine.states)
            {
                if (child.state.motion is BlendTree tree)
                    return tree;
            }
        }

        return null;
    }

    /// <summary>
    /// CreateBlendTreeInController adds a float parameter named "Blend" when the controller has none.
    /// The tree drives off <see cref="k_Parameter"/> instead, so drop the leftover.
    /// </summary>
    static void RemoveUnusedDefaultParameter(AnimatorController controller)
    {
        var parameters = new List<AnimatorControllerParameter>(controller.parameters);
        for (var i = parameters.Count - 1; i >= 0; i--)
        {
            if (parameters[i].name == k_DefaultParameter)
                controller.RemoveParameter(i);
        }
    }
}
