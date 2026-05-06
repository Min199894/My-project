using UnityEditor;
using UnityEngine;

public class RemapSlider : MaterialPropertyDrawer
{
   private const float FIELD_WIDTH = 40;

   private readonly float minLimit;
   private readonly float maxLimit;

   public RemapSlider(float min, float max, float offset)
   {
      //Properties 不能传入负数
      this.minLimit = min - offset;
      this.maxLimit = max;
   }
   
   public RemapSlider(float min, float max)
   {
      //Properties 不能传入负数
      this.minLimit = min;
      this.maxLimit = max;
   }
        
   public override void OnGUI (Rect position, MaterialProperty prop, string label, MaterialEditor editor)
   {
      float minVal = prop.vectorValue.x;
      float maxVal = prop.vectorValue.y;
            
      EditorGUI.BeginChangeCheck();
      EditorGUI.showMixedValue = prop.hasMixedValue;
      float sliderWidth = EditorGUIUtility.currentViewWidth -40;

      Rect labelRect = position;
      EditorGUIUtility.labelWidth = 0;
      labelRect.width = EditorGUIUtility.labelWidth;
      EditorGUI.LabelField(labelRect, label);
      sliderWidth-= labelRect.width;
            
      Rect minPos = position;
      minPos.x = EditorGUIUtility.labelWidth + 40;
      minPos.width = FIELD_WIDTH;
      minVal = EditorGUI.FloatField(minPos, minVal);
      minPos.x += FIELD_WIDTH;
      maxVal = EditorGUI.FloatField(minPos, maxVal);
      sliderWidth -= 2 * FIELD_WIDTH;
      
      Rect sliderPos = position;
      sliderPos.x = 40f + labelRect.width + 2 * FIELD_WIDTH + 5;//minPos.x + FIELD_WIDTH + 25;
      sliderPos.width = sliderWidth - 10;//(EditorGUIUtility.labelWidth + 40f) / 0.44999998807907104f * 0.48f;//+ (FIELD_WIDTH * 2f);
      EditorGUI.MinMaxSlider(sliderPos, ref minVal, ref maxVal, minLimit, maxLimit);
      
            
      Rect maxPos = position;
      maxPos.x = sliderPos.x + sliderPos.width;
      maxPos.width = FIELD_WIDTH;
     

      EditorGUI.showMixedValue = false;
            
      if (EditorGUI.EndChangeCheck())
      {
         prop.vectorValue = new Vector4(minVal, maxVal, 0f, 0f);
      }
   }
}
