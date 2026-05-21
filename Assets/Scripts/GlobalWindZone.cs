using UnityEngine;

[ExecuteAlways]
public class GlobalWindZone : MonoBehaviour
{
  [SerializeField]
  private WindSettings _windSettings = WindSettings.Calm;
  [SerializeField]
  private WindZone _sourceWindZone;
  [SerializeField]
  private Texture2D _gustNoise;
  [HideInInspector]
  [SerializeField]
  private int _selectedPreset;
  [SerializeField]
  private SimpleSpring _globalSimpleSpring;
  private Quaternion _cachedRotation;
  private float _cachedWindMain;
  private float _cachedWindPulseFrequency;
  private float _cachedWindTurbulence;
  private double _smoothWindOffset;
  private double _cachedTime;
  private Vector2 _windOffset;
  private Vector2 _prevWindOffset;
  private Vector2 _direction = new Vector2(0.0f, 1f);
  private Vector2 _directionVelocity;
  private float _strength;
  private float _strengthVelocity;
  private float _speed;
  private float _speedVelocity;
  private float _turbulence;
  private float _turbulenceVelocity;
  

  public static GlobalWindZone Instance { get; private set; }

  public WindSettings Settings
  {
    get => this._windSettings;
    set
    {
      this._windSettings = value;
      this._windSettings.Apply();
      this.UpdateDirection(false);
    }
  }

  public WindZone Zone
  {
    get => this._sourceWindZone;
    set
    {
      this._sourceWindZone = value;
      if (!((UnityEngine.Object) value != (UnityEngine.Object) null))
        return;
      this.ValidateWindZone();
      this.CopyAndApply();
    }
  }

  public Texture2D GustNoise
  {
    get => this._gustNoise;
    set
    {
      this._gustNoise = value;
      this._windSettings.Apply(this._gustNoise);
    }
  }

  public void SetFloatingOrigin(double x, double z)
  {
    double num1 = 0.02;
    Shader.SetGlobalVector("g_FloatingOriginOffset_Gust", new Vector4(this.Wrap(x, 1.0 / num1), this.Wrap(z, 1.0 / num1), 0.0f, 0.0f));
    double num2 = 1.0 / 16.0;
    Shader.SetGlobalVector("g_FloatingOriginOffset_Ambient", new Vector4(this.Wrap(x, 1.0 / num2), this.Wrap(z, 1.0 / num2), 0.0f, 0.0f));
    double range = 2285.0;
    Shader.SetGlobalVector("g_FloatingOriginOffset_Turbulence", new Vector4(this.Wrap(x, range), this.Wrap(z, range), 0.0f, 0.0f));
  }

  private float Wrap(double value, double range)
  {
    while (value > range)
      value -= range;
    while (value < range)
      value += range;
    return (float) value;
  }

  public void UpdateTime(double time)
  {
    double deltaTime = time - this._cachedTime;
    this._cachedTime = time;
    Shader.SetGlobalVector("g_PrevSmoothTime", new Vector4((float) this._smoothWindOffset * 6f, (float) this._smoothWindOffset * 0.15f, (float) this._smoothWindOffset * 3.5f, (float) this._smoothWindOffset * 3.5f));
    this._smoothWindOffset += deltaTime * (double) this.Settings.WindSpeed;
    Shader.SetGlobalVector("g_SmoothTime", new Vector4((float) this._smoothWindOffset * 6f, (float) this._smoothWindOffset * 0.15f, (float) this._smoothWindOffset * 3.5f, (float) this._smoothWindOffset * 3.5f));
    this._direction = Vector2.SmoothDamp(this._direction, this.Settings.WindDirection, ref this._directionVelocity, 1f, 1f, (float) deltaTime);
    this._turbulence = Mathf.SmoothDamp(this._turbulence, this.Settings.Turbulence, ref this._turbulenceVelocity, 1f, 1f, (float) deltaTime);
    
    this._speed = Mathf.SmoothDamp(this._speed, this.Settings.WindSpeed, ref this._speedVelocity, 1f, 1f, (float) deltaTime);
    this._strength = _globalSimpleSpring.SmoothDamp(this._strength, this.Settings.WindStrength, ref this._strengthVelocity,(float) deltaTime);
    this._prevWindOffset = this._windOffset;
    this._windOffset += (float) deltaTime * this._speed * this._direction * 0.15f;
    Shader.SetGlobalVector("g_WindOffset", new Vector4(this._windOffset.x, this._windOffset.y, this._prevWindOffset.x, this._prevWindOffset.y));
    Shader.SetGlobalVector("g_WindDirection", new Vector4(this._direction.x, 0.0f, this._direction.y));
    Shader.SetGlobalVector("g_Wind", new Vector4(this._speed, this._strength));
    Shader.SetGlobalVector("g_Turbulence", new Vector4(this._speed, this._turbulence));
  }

  private void OnEnable()
  {
    GlobalWindZone.Instance = this;
    this.ValidateWindZone();
    if ((UnityEngine.Object) this._sourceWindZone != (UnityEngine.Object) null)
      this.CopyFromWindZone();
    else
      this.UpdateDirection(false);
    this._windSettings.Apply(this._gustNoise);
    this._globalSimpleSpring = new SimpleSpring();
  }

  private void Update()
  {
    if ((UnityEngine.Object) this._sourceWindZone != (UnityEngine.Object) null && this.WindZoneHasChanged())
      this.CopyAndApply();
    //if (Application.isPlaying)
      this.UpdateTime((double) Time.time);
    this.UpdateDirection(true);
  }

  private void CopyAndApply()
  {
    this.CacheWindZoneProperties();
    this.CopyFromWindZone();
  }

  private void CopyFromWindZone()
  {
    this.Settings = WindSettings.FromWindZone(this._sourceWindZone);
  }

  private bool WindZoneHasChanged()
  {
    return this._cachedRotation != this._sourceWindZone.transform.rotation || (double) this._cachedWindMain != (double) this._sourceWindZone.windMain || (double) this._cachedWindPulseFrequency != (double) this._sourceWindZone.windPulseFrequency || (double) this._cachedWindTurbulence != (double) this._sourceWindZone.windTurbulence;
  }

  private void CacheWindZoneProperties()
  {
    this._cachedRotation = this._sourceWindZone.transform.rotation;
    this._cachedWindMain = this._sourceWindZone.windMain;
    this._cachedWindPulseFrequency = this._sourceWindZone.windPulseFrequency;
    this._cachedWindTurbulence = this._sourceWindZone.windTurbulence;
  }

  private void ValidateWindZone()
  {
    if (!((UnityEngine.Object) this._sourceWindZone != (UnityEngine.Object) null) || this._sourceWindZone.mode == 0)
      return;
    Debug.LogWarning((object) (((object) this).GetType().Name + " requires a directional wind zone."), (UnityEngine.Object) this);
  }

  private void UpdateDirection(bool useCache)
  {
    if ((UnityEngine.Object) this._sourceWindZone != (UnityEngine.Object) null || useCache && this.transform.rotation == this._cachedRotation)
      return;
    this._cachedRotation = this.transform.rotation;
    this._windSettings.WindDirection = WindSettings.RotationToDirection(this.transform.rotation);
    this._windSettings.Apply();
  }
}
