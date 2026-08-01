//SPDX-FileCopyrightText: 2025 MirageEexe <Mirageeexe@gmail.com>
//SPDX-License-Identifier: AGPL-3.0-or-later
//RW
using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Audio;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;


namespace Content.Goobstation.Shared.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeArmyHandCombat()
    {
        SubscribeLocalEvent<CanPerformComboComponent, ArbElbowStrikePerformedEvent>(OnArbElbowStrike);
        SubscribeLocalEvent<CanPerformComboComponent, ArbKneeStrikePerformedEvent>(OnArbKneeStrike);
        SubscribeLocalEvent<CanPerformComboComponent, ArbArmLockPerformedEvent>(OnArbArmLock);
        SubscribeLocalEvent<CanPerformComboComponent, ArbSweepPerformedEvent>(OnArbSweep);
        SubscribeLocalEvent<CanPerformComboComponent, ArbHipThrowPerformedEvent>(OnArbHipThrow);
        SubscribeLocalEvent<CanPerformComboComponent, ArbChokePerformedEvent>(OnArbChoke);

        SubscribeLocalEvent<GrantArbComponent, ClothingGotEquippedEvent>(OnGrantArb);
        SubscribeLocalEvent<GrantArbComponent, ClothingGotUnequippedEvent>(OnRemoveArb);

        SubscribeLocalEvent<MartialArtsKnowledgeComponent, MeleeHitEvent>(OnArbPassivePenalty);
    }


    private void OnGrantArb(Entity<GrantArbComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_netManager.IsServer)
            return;

        var user = args.Wearer;
        TryGrantMartialArt(user, ent.Comp);
    }

    private void OnRemoveArb(Entity<GrantArbComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var user = args.Wearer;
        if (!TryComp<MartialArtsKnowledgeComponent>(user, out var martialArtsKnowledge))
            return;

        if (martialArtsKnowledge.MartialArtsForm != MartialArtsForms.ArmyHandCombat)
            return;

        if (!TryComp<MeleeWeaponComponent>(args.Wearer, out var meleeWeaponComponent))
            return;

        var originalDamage = new DamageSpecifier();
        originalDamage.DamageDict[martialArtsKnowledge.OriginalFistDamageType]
            = FixedPoint2.New(martialArtsKnowledge.OriginalFistDamage);
        meleeWeaponComponent.Damage = originalDamage;

        RemComp<MartialArtsKnowledgeComponent>(user);
        RemComp<CanPerformComboComponent>(user);
    }

    private void OnArbPassivePenalty(Entity<MartialArtsKnowledgeComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.MartialArtsForm != MartialArtsForms.ArmyHandCombat)
            return;

        if (args.Weapon != ent.Owner || args.HitEntities.Count == 0)
            return;

        ApplyMultiplier(ent, 0.5f, 0f, TimeSpan.FromSeconds(2.5f), MartialArtModifierType.AttackRate);
    }


    private void ArbClearCombo(Entity<CanPerformComboComponent> ent)
    {
        ent.Comp.LastAttacks.Clear();
        Dirty(ent, ent.Comp);
    }


    private void OnArbElbowStrike(Entity<CanPerformComboComponent> ent, ref ArbElbowStrikePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ArbClearCombo(ent);
    }


    private void OnArbKneeStrike(Entity<CanPerformComboComponent> ent, ref ArbKneeStrikePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);

        _movementMod.TryUpdateMovementSpeedModDuration(target, MartsGenericSlow, TimeSpan.FromSeconds(4), 0.55f, 0.55f);
        _modifier.RefreshMovementSpeedModifiers(target);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ArbClearCombo(ent);
    }


    private void OnArbArmLock(Entity<CanPerformComboComponent> ent, ref ArbArmLockPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);

        if (_hands.TryGetActiveItem(target, out var activeItem))
        {
            if (_hands.TryDrop(target, activeItem.Value))
            {
                if (_hands.TryGetEmptyHand(ent.Owner, out var emptyHand))
                {
                    if (_hands.TryPickup(ent, activeItem.Value, emptyHand))
                        _hands.SetActiveHand(ent.Owner, emptyHand);
                }
            }
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/thudswoosh.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ArbClearCombo(ent);
    }


    private void OnArbSweep(Entity<CanPerformComboComponent> ent, ref ArbSweepPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed)
            return;

        _stun.TryKnockdown(target, proto.ParalyzeTime, true, true, proto.DropItems);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ArbClearCombo(ent);
    }

    private void OnArbHipThrow(Entity<CanPerformComboComponent> ent, ref ArbHipThrowPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed)
            return;

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        dir *= 1f / dir.Length();

        _stun.TryKnockdown(target, proto.ParalyzeTime, true, true, proto.DropItems);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _grabThrowing.Throw(target, ent, dir, proto.ThrownSpeed, behavior: proto.DropItems);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/bodyfall.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ArbClearCombo(ent);
    }


    private void OnArbChoke(Entity<CanPerformComboComponent> ent, ref ArbChokePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        _stun.TryKnockdown(target, proto.ParalyzeTime, true, true, proto.DropItems);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _, TargetBodyPart.Head);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/bodyfall.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ArbClearCombo(ent);
    }
}
