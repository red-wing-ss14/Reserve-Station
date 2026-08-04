namespace Content.Goobstation.Common.Barks;

public static class BarkSettingsUtility
{
    private const float PauseMin = 0.05f;
    private const float PauseMax = 0.12f;
    private const float PitchMin = 0.7f;
    private const float PitchMax = 1.3f;
    private const float PitchVarianceMin = 0f;
    private const float PitchVarianceMax = 0.35f;

    public static float GetPause(BarkPercentageApplyData data)
    {
        return PauseMin + (PauseMax - PauseMin) * (data.Pause / (float) byte.MaxValue);
    }

    public static float GetPitch(BarkPercentageApplyData data)
    {
        return PitchMin + (PitchMax - PitchMin) * (data.Pitch / (float) byte.MaxValue);
    }

    public static float GetPitchVariance(BarkPercentageApplyData data)
    {
        return PitchVarianceMin + (PitchVarianceMax - PitchVarianceMin) * (data.PitchVariance / (float) byte.MaxValue);
    }
}
