using Content.Goobstation.Common.Barks;
using Content.Client._RW.UserInterface.Controls;
using System.Linq;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<BarkPrototype> _barkPrototypes = new();

    private void InitializeBarkVoice()
    {
        BarkPitchSlider.OnReleased += _ => OnBarkPitchChanged();
        BarkPitchVarianceSlider.OnReleased += _ => OnBarkPitchVarianceChanged();
        BarkPauseSlider.OnReleased += _ => OnBarkPauseChanged();

        BarkVoiceButton.OnItemSelected += args =>
        {
            BarkVoiceButton.SelectId(args.Id);
            SetBark(_barkPrototypes[args.Id], Profile?.BarkSettings ?? BarkPercentageApplyData.Default);
        };

        BarkVoicePlayButton.OnPressed += _ => PlayPreviewBark();
    }

    private void UpdateBarkVoice()
    {
        if (Profile is null)
            return;

        _barkPrototypes = _prototypeManager
            .EnumeratePrototypes<BarkPrototype>()
            .Where(o => o.RoundStart &&
                        (o.SpeciesWhitelist is null ||
                         o.SpeciesWhitelist.Contains(Profile.Species)))
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();

        BarkVoiceButton.Clear();

        var selectedBarkId = -1;
        for (var i = 0; i < _barkPrototypes.Count; i++)
        {
            var bark = _barkPrototypes[i];
            if (bark == Profile.BarkVoice)
                selectedBarkId = i;

            BarkVoiceButton.AddItem(Loc.GetString(bark.Name), i);
        }

        if (selectedBarkId == -1)
            selectedBarkId = 0;

        if (_barkPrototypes.Count > 0)
        {
            BarkVoiceButton.SelectId(selectedBarkId);
            SetBark(_barkPrototypes[selectedBarkId], Profile.BarkSettings, preview: false);
        }

        UpdateBarkSliderValues();
    }

    private void UpdateBarkSliderValues()
    {
        if (Profile is null)
            return;

        BarkPauseSlider.Value = Profile.BarkSettings.Pause;
        BarkPitchSlider.Value = Profile.BarkSettings.Pitch;
        BarkPitchVarianceSlider.Value = Profile.BarkSettings.PitchVariance;
    }

        private void SetBark(BarkPrototype barkVoice, BarkPercentageApplyData settings, bool preview = true)
    {
        Profile = Profile?
            .WithBarkVoice(barkVoice)
            .WithBarkSettings(settings);
        IsDirty = true;

        if (preview)
            PlayPreviewBark();
    }

    private void OnBarkPauseChanged()
    {
        if (Profile is null || _barkPrototypes.Count == 0)
            return;

        var bark = _barkPrototypes[BarkVoiceButton.SelectedId];
        SetBark(bark, new BarkPercentageApplyData
        {
            Pause = (byte) BarkPauseSlider.Value,
            Pitch = Profile.BarkSettings.Pitch,
            Volume = Profile.BarkSettings.Volume,
            PitchVariance = Profile.BarkSettings.PitchVariance,
        });
    }

    private void OnBarkPitchChanged()
    {
        if (Profile is null || _barkPrototypes.Count == 0)
            return;

        var bark = _barkPrototypes[BarkVoiceButton.SelectedId];
        SetBark(bark, new BarkPercentageApplyData
        {
            Pause = Profile.BarkSettings.Pause,
            Pitch = (byte) BarkPitchSlider.Value,
            Volume = Profile.BarkSettings.Volume,
            PitchVariance = Profile.BarkSettings.PitchVariance,
        });
    }

    private void OnBarkPitchVarianceChanged()
    {
        if (Profile is null || _barkPrototypes.Count == 0)
            return;

        var bark = _barkPrototypes[BarkVoiceButton.SelectedId];
        SetBark(bark, new BarkPercentageApplyData
        {
            Pause = Profile.BarkSettings.Pause,
            Pitch = Profile.BarkSettings.Pitch,
            Volume = Profile.BarkSettings.Volume,
            PitchVariance = (byte) BarkPitchVarianceSlider.Value,
        });
    }

    private void PlayPreviewBark()
    {
        if (Profile is null)
            return;

        var ev = new PreviewBarkEvent(Profile.BarkVoice, Profile.BarkSettings);
        _entManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}
