using System.Linq;
using Content.Shared._RW.TTS;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RW.Surgery;

public sealed class ChangeSexSurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubSurgery<SurgeryChangeSexStepComponent>(OnChangeSexStep, OnChangeSexCheck);
        SubSurgery<SurgeryUndoChangeSexStepComponent>(OnRevereSexStep, OnRevertSexCheck);
    }

    private void OnRevertSexCheck(Entity<SurgeryUndoChangeSexStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        args.Cancelled |= HasComp<HumanoidAppearanceComponent>(args.Body) && HasComp<ChangedSexComponent>(args.Body);
    }

    private void OnRevereSexStep(Entity<SurgeryUndoChangeSexStepComponent> ent, ref SurgeryStepEvent args)
    {
        if(!TryComp<HumanoidAppearanceComponent>(args.Body, out var humanoidAppearanceComponent) ||
           !TryComp<ChangedSexComponent>(args.Body, out var changedSexComponent))
            return;

        RevertChangeSex(args.Body, humanoidAppearanceComponent, changedSexComponent);
    }

    private void OnChangeSexCheck(Entity<SurgeryChangeSexStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        args.Cancelled |= HasComp<HumanoidAppearanceComponent>(args.Body) && !HasComp<ChangedSexComponent>(args.Body);
    }

    private void OnChangeSexStep(Entity<SurgeryChangeSexStepComponent> ent, ref SurgeryStepEvent args)
    {
        if(!TryComp<HumanoidAppearanceComponent>(args.Body, out var humanoidAppearanceComponent))
            return;

        DoChangeSex(args.Body, humanoidAppearanceComponent);
    }

    private void RevertChangeSex(EntityUid surgeryUid,
        HumanoidAppearanceComponent humanoidAppearanceComponent,
        ChangedSexComponent changedSexComponent)
    {
        humanoidAppearanceComponent.Gender = changedSexComponent.OldGender;
        humanoidAppearanceComponent.Sex = changedSexComponent.OldSex;

        if (TryComp<TTSComponent>(surgeryUid, out var ttsComponent))
        {
            ttsComponent.VoicePrototypeId = changedSexComponent.OldVoice;
            Dirty(surgeryUid, ttsComponent);
        }

        RemComp<ChangedSexComponent>(surgeryUid);
        Dirty(surgeryUid, humanoidAppearanceComponent);
    }

    private void DoChangeSex(EntityUid surgeryUid, HumanoidAppearanceComponent humanoidAppearanceComponent)
    {
        var changedComp = EnsureComp<ChangedSexComponent>(surgeryUid);

        changedComp.OldSex = humanoidAppearanceComponent.Sex;
        changedComp.OldGender = humanoidAppearanceComponent.Gender;

        var isMale = humanoidAppearanceComponent.Sex is Sex.Male or Sex.Unsexed;

        if (isMale)
        {
            if(humanoidAppearanceComponent.Gender is Gender.Male)
                humanoidAppearanceComponent.Gender = Gender.Female;

            humanoidAppearanceComponent.Sex = Sex.Female;
        }
        else
        {

            if(humanoidAppearanceComponent.Gender is Gender.Female)
                humanoidAppearanceComponent.Gender = Gender.Male;

            humanoidAppearanceComponent.Sex = Sex.Male;
        }

        Dirty(surgeryUid, humanoidAppearanceComponent);

        if (!TryComp<TTSComponent>(surgeryUid, out var ttsComponent))
            return;

        changedComp.OldVoice = ttsComponent.VoicePrototypeId;

        var voices = _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(p => p.Sex == humanoidAppearanceComponent.Sex)
            .ToList();

        if (voices.Count <= 0)
            return;

        var randomVoice = voices[_random.Next(0, voices.Count)];

        ttsComponent.VoicePrototypeId = randomVoice.ID;
        Dirty(surgeryUid, ttsComponent);
    }

    private void SubSurgery<TComp>(EntityEventRefHandler<TComp, SurgeryStepEvent> onStep,
        EntityEventRefHandler<TComp, SurgeryStepCompleteCheckEvent> onComplete) where TComp : IComponent
    {
        SubscribeLocalEvent(onStep);
        SubscribeLocalEvent(onComplete);
    }
}
