using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "FrictionPreset", menuName = "Vehicle/Friction Preset", order = 1)]
public class FrictionPreset : ScriptableObject
{
    public Vector4 BCDE = new Vector4(10f, 1.9f, 1f, 0.97f);

    [SerializeField]
    private AnimationCurve _curve;

    public AnimationCurve Curve => _curve;

    public float peakSlip = 0.12f;

    private const int KeyCount = 20;

    private void OnValidate()
    {
        UpdateFrictionCurve();
    }

    public void UpdateFrictionCurve()
    {
        _curve = new AnimationCurve();

        float t = 0f;

        for (int i = 0; i < KeyCount; i++)
        {
            float val = GetFrictionValue(t, BCDE);
            _curve.AddKey(t, val);

            if (i <= 10) t += 0.02f;
            else t += 0.1f;
        }

        for (int i = 0; i < KeyCount; i++)
            _curve.SmoothTangents(i, 0f);

        peakSlip = CalculatePeakSlip();
    }

    private float GetFrictionValue(float slip, Vector4 p)
    {
        float B = p.x;
        float C = p.y;
        float D = p.z;
        float E = p.w;

        float t = Mathf.Abs(slip);
        return D * Mathf.Sin(C * Mathf.Atan(B * t - E * (B * t - Mathf.Atan(B * t))));
    }

    private float CalculatePeakSlip()
    {
        float peak = 0;
        float valMax = 0;

        for (float s = 0; s <= 1f; s += 0.01f)
        {
            float v = _curve.Evaluate(s);
            if (v > valMax)
            {
                valMax = v;
                peak = s;
            }
        }

        return peak;
    }
}