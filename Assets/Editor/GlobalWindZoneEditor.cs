using System;
using UnityEditor;
using UnityEngine;

#nullable disable


[CustomEditor(typeof (GlobalWindZone))]
public class GlobalWindZoneEditor : UnityEditor.Editor
{
  private SerializedProperty _preset;
  private SerializedProperty _sourceWindZone;
  private SerializedProperty _windSettings;
  private SerializedProperty _gustDirection;
  private SerializedProperty _windStrength;
  private SerializedProperty _windSpeed;
  private SerializedProperty _turbulence;
  private SerializedProperty _gustNoise;
  private GlobalWindZoneEditor.WindPreset _selectedPreset = GlobalWindZoneEditor.WindPreset.ClickToLoad;

  private void OnSceneGUI()
  {
    GlobalWindZone target = this.target as GlobalWindZone;
    if ((UnityEngine.Object) target == (UnityEngine.Object) null)
      return;
    Quaternion rotation = Quaternion.Euler(0.0f, target.transform.eulerAngles.y, 0.0f);
    float handleSize = HandleUtility.GetHandleSize(target.transform.position);
    UnityEditor.Handles.color = Color.yellow;
    UnityEditor.Handles.ArrowHandleCap(GUIUtility.GetControlID(FocusType.Passive), target.transform.position, rotation, 2f * handleSize, UnityEngine.EventType.Repaint);
    UnityEditor.Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), target.transform.position, rotation, 0.2f * handleSize, UnityEngine.EventType.Repaint);
  }

  public override void OnInspectorGUI()
  {
    this.serializedObject.Update();
    bool enabled = GUI.enabled;
    EditorGUI.BeginChangeCheck();
    this._selectedPreset = (GlobalWindZoneEditor.WindPreset) EditorGUILayout.EnumPopup("Load Preset", (Enum) (Enum) this._selectedPreset);
    if (EditorGUI.EndChangeCheck() && this._selectedPreset != 0)
      this.ApplyPreset();
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(this._sourceWindZone);
    if (EditorGUI.EndChangeCheck())
    {
      this.serializedObject.ApplyModifiedProperties();
      if (this._sourceWindZone.objectReferenceValue != (UnityEngine.Object) null)
        ((GlobalWindZone) this.target).Zone = (WindZone) this._sourceWindZone.objectReferenceValue;
    }
    if (this._sourceWindZone.objectReferenceValue != (UnityEngine.Object) null)
    {
      EditorGUILayout.HelpBox("Wind settings are loaded from Wind Zone component. Remove the Wind Zone component to manually modify the wind.", MessageType.Info);
      GUI.enabled = false;
    }
    EditorGUI.BeginChangeCheck();
    GUILayout.Label("Wind", EditorStyles.boldLabel);
    EditorGUILayout.PropertyField(this._windStrength);
    EditorGUILayout.PropertyField(this._windSpeed);
    EditorGUILayout.PropertyField(this._turbulence);
    GUI.enabled = enabled;
    GUILayout.Label("Noise", EditorStyles.boldLabel);
    EditorGUILayout.PropertyField(this._gustNoise);
    if (EditorGUI.EndChangeCheck())
    {
      this._selectedPreset = GlobalWindZoneEditor.WindPreset.ClickToLoad;
      this.serializedObject.ApplyModifiedProperties();
      ((GlobalWindZone) this.target).Settings.Apply(this._gustNoise.objectReferenceValue as Texture2D);
    }
    if (this._selectedPreset == (GlobalWindZoneEditor.WindPreset) this._preset.intValue)
      return;
    this._preset.intValue = (int) this._selectedPreset;
    this.serializedObject.ApplyModifiedProperties();
  }

  private void ApplyPreset()
  {
    if (this._sourceWindZone.objectReferenceValue != (UnityEngine.Object) null)
    {
      if (!EditorUtility.DisplayDialog("Apply Preset?", "The wind settings are driven by a wind zone. Do you want to apply the preset to the source Wind Zone?", "Apply", "Cancel"))
      {
        this._selectedPreset = (GlobalWindZoneEditor.WindPreset) this._preset.intValue;
        return;
      }
      Undo.RecordObjects(new UnityEngine.Object[2]
      {
        this.target,
        this._sourceWindZone.objectReferenceValue
      }, "Load Wind Preset");
    }
    else
      Undo.RecordObject(this.target, "Load Wind Preset");
    GlobalWindZone target = (GlobalWindZone) this.target;
    switch (this._selectedPreset)
    {
      case GlobalWindZoneEditor.WindPreset.Calm:
        target.Settings = WindSettings.Calm;
        break;
      case GlobalWindZoneEditor.WindPreset.Breeze:
        target.Settings = WindSettings.Breeze;
        break;
      case GlobalWindZoneEditor.WindPreset.StrongBreeze:
        target.Settings = WindSettings.StrongBreeze;
        break;
      case GlobalWindZoneEditor.WindPreset.Storm:
        target.Settings = WindSettings.Storm;
        break;
    }
    target.Settings = new WindSettings(target.Settings)
    {
      WindDirection = WindSettings.RotationToDirection(target.transform.rotation)
    };
    this.serializedObject.Update();
    if (this._sourceWindZone.objectReferenceValue != (UnityEngine.Object) null)
    {
      ((GlobalWindZone) this.target).Settings.ApplyToWindZone((WindZone) this._sourceWindZone.objectReferenceValue);
      EditorUtility.SetDirty(this._sourceWindZone.objectReferenceValue);
    }
    Undo.FlushUndoRecordObjects();
  }

  private void OnEnable()
  {
    if (target == null)
      return;
    this.FindSerializedProperties();
    this.ValidateNoise();
    this._selectedPreset = (GlobalWindZoneEditor.WindPreset) this._preset.intValue;
    Undo.undoRedoPerformed += new Undo.UndoRedoCallback(this.OnUndoPerformed);
  }

  private void OnDisable()
  {
    Undo.undoRedoPerformed -= new Undo.UndoRedoCallback(this.OnUndoPerformed);
  }

  private void OnUndoPerformed()
  {
    this.serializedObject.Update();
    this._selectedPreset = (GlobalWindZoneEditor.WindPreset) this._preset.intValue;
  }

  private void ValidateNoise()
  {
    if (!(this._gustNoise.objectReferenceValue == (UnityEngine.Object) null))
      return;
    //this._gustNoise.objectReferenceValue = (UnityEngine.Object) GlobalWindInitializer.LoadGustNoise();
    this._gustNoise.serializedObject.ApplyModifiedPropertiesWithoutUndo();
  }
  private void FindSerializedProperties()
  {
      this._preset = this.serializedObject.FindProperty("_selectedPreset");
      this._sourceWindZone = this.serializedObject.FindProperty("_sourceWindZone");
      this._windSettings = this.serializedObject.FindProperty("_windSettings");
      this._gustDirection = this._windSettings.FindPropertyRelative("GustDirection");
      this._windStrength = this._windSettings.FindPropertyRelative("WindStrength");
      this._windSpeed = this._windSettings.FindPropertyRelative("WindSpeed");
      this._turbulence = this._windSettings.FindPropertyRelative("Turbulence");
      this._gustNoise = this.serializedObject.FindProperty("_gustNoise");
  }
  
  private enum WindPreset
  {
    ClickToLoad,
    Calm,
    Breeze,
    StrongBreeze,
    Storm,
  }
}
