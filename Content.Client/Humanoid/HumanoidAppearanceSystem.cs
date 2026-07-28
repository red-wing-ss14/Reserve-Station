// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.DisplacementMap;
using Content.Shared.CCVar;
using Content.Shared.CCVar;
using Content.Shared._Amour.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    // Amour edit start
    private const string MarkingGradientShaderId = "AmourMarkingGradient";
 
    private static bool MarkingSupportsGradient(MarkingPrototype marking) =>
        marking.SupportsGradient ||
        marking.MarkingCategory is MarkingCategories.Hair or MarkingCategories.FacialHair;

    private static bool UsesBodyLayerGradient(MarkingPrototype marking) =>
        marking.MarkingCategory == MarkingCategories.BodyGradient &&
        marking.BodyParts is { Count: > 0 };

    private static int GetBodyPartSpriteOffset(MarkingPrototype markingPrototype, HumanoidVisualLayers bodyPart, int spriteIndex)
    {
        var offset = 1;
        for (var i = 0; i < spriteIndex; i++)
        {
            if (markingPrototype.GetBodyPart(i) == bodyPart)
                offset++;
        }

        return offset;
    }

    private static void SetGradientShaderParameters(
        ShaderInstance shaderInstance,
        Marking marking,
        int colorIndex,
        Color? mixColor = null,
        float? alpha = null)
    {
        var color1 = colorIndex >= 0 && colorIndex < marking.MarkingColors.Count
            ? marking.MarkingColors[colorIndex]
            : Color.White;
        var color2 = marking.GetGradientColor(colorIndex);

        if (mixColor != null)
        {
            color1 = Color.InterpolateBetween(color1, mixColor.Value, 0.5f);
            color2 = Color.InterpolateBetween(color2, mixColor.Value, 0.5f);
        }

        if (alpha != null)
        {
            color1 = color1.WithAlpha(alpha.Value);
            color2 = color2.WithAlpha(alpha.Value);
        }

        shaderInstance.SetParameter("Color1", color1);
        shaderInstance.SetParameter("Color2", color2);
        shaderInstance.SetParameter("GradientPosition", Marking.ClampGradientPosition(marking.GradientPosition));
        shaderInstance.SetParameter("GradientBlur", Marking.ClampGradientBlur(marking.GradientBlur));
    }
    // Amour edit end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, AfterAutoHandleStateEvent>(OnHandleState);
        Subs.CVar(_configurationManager, CCVars.AccessibilityClientCensorNudity, OnCvarChanged, true);
        Subs.CVar(_configurationManager, CCVars.AccessibilityServerCensorNudity, OnCvarChanged, true);
    }

    private void OnHandleState(EntityUid uid, HumanoidAppearanceComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite((uid, component, Comp<SpriteComponent>(uid)));
    }

    private void OnCvarChanged(bool value)
    {
        var humanoidQuery = EntityManager.AllEntityQueryEnumerator<HumanoidAppearanceComponent, SpriteComponent>();
        while (humanoidQuery.MoveNext(out var _, out var humanoidComp, out var spriteComp))
        {
            UpdateSprite(humanoidComp, spriteComp);
        }
    }

    private void UpdateSprite(HumanoidAppearanceComponent component, SpriteComponent sprite)
    {
        var humanoidQuery = AllEntityQuery<HumanoidAppearanceComponent, SpriteComponent>();
        while (humanoidQuery.MoveNext(out var uid, out var humanoidComp, out var spriteComp))
        {
            UpdateSprite((uid, humanoidComp, spriteComp));
        }
    }

    public void UpdateSprite(Entity<HumanoidAppearanceComponent, SpriteComponent> entity) // Goob edit - made public
    {
        UpdateLayers(entity);
        ApplyMarkingSet(entity);

        var humanoidAppearance = entity.Comp1;
        var sprite = entity.Comp2;

        // begin Goobstation: port EE height/width sliders
        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(humanoidAppearance.Species);

        var height = Math.Clamp(humanoidAppearance.Height, speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        var width = Math.Clamp(humanoidAppearance.Width, speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        humanoidAppearance.Height = height;
        humanoidAppearance.Width = width;

        _sprite.SetScale((entity, sprite), new Vector2(width, height));
        // end Goobstation: port EE height/width sliders

        sprite[_sprite.LayerMapReserve((entity.Owner, sprite), HumanoidVisualLayers.Eyes)].Color = humanoidAppearance.EyeColor;
    }

    private static bool IsHidden(HumanoidAppearanceComponent humanoid, HumanoidVisualLayers layer)
        => humanoid.HiddenLayers.ContainsKey(layer) || humanoid.PermanentlyHidden.Contains(layer);

    private void UpdateLayers(Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        var component = entity.Comp1;
        var sprite = entity.Comp2;

        var oldLayers = new HashSet<HumanoidVisualLayers>(component.BaseLayers.Keys);
        component.BaseLayers.Clear();

        // add default species layers
        var bodyTypeProto = _prototypeManager.Index<BodyTypePrototype>(component.BodyType); // Amour port: WD Slim body types
        foreach (var (key, id) in bodyTypeProto.Sprites) // Amour port: WD Slim body types
        {
            oldLayers.Remove(key);
            if (!component.CustomBaseLayers.ContainsKey(key))
                SetLayerData(entity, key, id, sexMorph: true);
        }

        // add custom layers
        foreach (var (key, info) in component.CustomBaseLayers)
        {
            oldLayers.Remove(key);
            SetLayerData(entity, key, info.Id, sexMorph: false, color: info.Color, overrideSkin: true, shader: info.Shader);
        }

        // hide old layers
        // TODO maybe just remove them altogether?
        foreach (var key in oldLayers)
        {
            if (_sprite.LayerMapTryGet((entity.Owner, sprite), key, out var index, false))
                sprite[index].Visible = false;
        }
    }

    private void SetLayerData(
        Entity<HumanoidAppearanceComponent, SpriteComponent> entity,
        HumanoidVisualLayers key,
        string? protoId,
        bool sexMorph = false,
        Color? color = null,
        bool overrideSkin = false,
        string? shader = null) // Shitmed Change // Goob edit
    {
        var component = entity.Comp1;
        var sprite = entity.Comp2;

        var layerIndex = _sprite.LayerMapReserve((entity.Owner, sprite), key);
        var layer = sprite[layerIndex];
        layer.Visible = !IsHidden(component, key);

        if (color != null)
            layer.Color = color.Value;

        if (shader != null) // Goobstation
            sprite.LayerSetShader(layerIndex, shader);
        else
            sprite.LayerSetShader(layerIndex, null, null);

        if (protoId == null)
        {
            _displacement.EnsureDisplacementIsNotOnSprite((entity.Owner, sprite), key);
            return;
        }

        if (sexMorph)
            protoId = HumanoidVisualLayersExtension.GetSexMorph(key, component.Sex, protoId);

        var proto = _prototypeManager.Index<HumanoidSpeciesSpriteLayer>(protoId);
        component.BaseLayers[key] = proto;

        if (proto.MatchSkin && !overrideSkin) // Shitmed Change
        {
            if (shader == null) // Amour edit
                sprite.LayerSetShader(layerIndex, null, null); // Amour edit
            layer.Color = component.SkinColor.WithAlpha(proto.LayerAlpha);
        }

        if (proto.BaseSprite != null)
            _sprite.LayerSetSprite((entity.Owner, sprite), layerIndex, proto.BaseSprite);

        if (component.MarkingsDisplacement.TryGetValue(key, out var displacementData))
        {
            _displacement.TryAddDisplacement(displacementData, (entity.Owner, sprite), layerIndex, key, out var displacementKey);
            if (displacementKey != null && _sprite.LayerMapTryGet((entity.Owner, sprite), displacementKey, out var dispIndex, false))
            {
                sprite[dispIndex].Visible = layer.Visible;
            }
        }
        else
        {
            _displacement.EnsureDisplacementIsNotOnSprite((entity.Owner, sprite), key);
        }
    }

    /// <summary>
    ///     Loads a profile directly into a humanoid.
    /// </summary>
    /// <param name="uid">The humanoid entity's UID</param>
    /// <param name="profile">The profile to load.</param>
    /// <param name="humanoid">The humanoid entity's humanoid component.</param>
    /// <remarks>
    ///     This should not be used if the entity is owned by the server. The server will otherwise
    ///     override this with the appearance data it sends over.
    /// </remarks>
    public override void LoadProfile(EntityUid uid, HumanoidCharacterProfile? profile, HumanoidAppearanceComponent? humanoid = null)
    {
        if (profile == null)
            return;

        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        var customBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>();

        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var markings = new MarkingSet(speciesPrototype.MarkingPoints, _markingManager, _prototypeManager);

        // Add markings that doesn't need coloring. We store them until we add all other markings that doesn't need it.
        var markingFColored = new Dictionary<Marking, MarkingPrototype>();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                if (!prototype.ForcedColoring)
                {
                    markings.AddBack(prototype.MarkingCategory, marking);
                }
                else
                {
                    markingFColored.Add(marking, prototype);
                }
            }
        }

        // legacy: remove in the future?
        //markings.RemoveCategory(MarkingCategories.Hair);
        //markings.RemoveCategory(MarkingCategories.FacialHair);

        // We need to ensure hair before applying it or coloring can try depend on markings that can be invalid
        var hairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.Hair, out var hairAlpha, _prototypeManager)
            ? profile.Appearance.SkinColor.WithAlpha(hairAlpha)
            : profile.Appearance.HairColor;
        var hair = new Marking(profile.Appearance.HairStyleId,
            new[] { hairColor });
        // Amour edit start
        if (profile.Appearance.HairUseGradient)
        {
            hair.UseGradient = true;
            hair.GradientPosition = profile.Appearance.HairGradientPosition;
            hair.GradientBlur = HumanoidCharacterAppearance.ClampHairGradientBlur(profile.Appearance.HairGradientBlur);
            hair.SetGradientColor(0, profile.Appearance.HairColor2);
        }
        // Amour edit end

        var facialHairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _prototypeManager)
            ? profile.Appearance.SkinColor.WithAlpha(facialHairAlpha)
            : profile.Appearance.FacialHairColor;
        var facialHair = new Marking(profile.Appearance.FacialHairStyleId,
            new[] { facialHairColor });
        // Amour edit start
        if (profile.Appearance.FacialHairUseGradient)
        {
            facialHair.UseGradient = true;
            facialHair.GradientPosition = profile.Appearance.FacialHairGradientPosition;
            facialHair.GradientBlur = profile.Appearance.FacialHairGradientBlur;
            facialHair.SetGradientColor(0, profile.Appearance.FacialHairColor2);
        }
        // Amour edit end
        if (_markingManager.CanBeApplied(profile.Species, profile.Sex, hair, _prototypeManager))
        {
            markings.AddBack(MarkingCategories.Hair, hair);
        }
        if (_markingManager.CanBeApplied(profile.Species, profile.Sex, facialHair, _prototypeManager))
        {
            markings.AddBack(MarkingCategories.FacialHair, facialHair);
        }

        // Finally adding marking with forced colors
        foreach (var (marking, prototype) in markingFColored)
        {
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                profile.Appearance.SkinColor,
                profile.Appearance.EyeColor,
                markings
            );
            markings.AddBack(prototype.MarkingCategory, new Marking(marking.MarkingId, markingColors));
        }

        markings.EnsureSpecies(profile.Species, profile.Appearance.SkinColor, _markingManager, _prototypeManager);
        markings.EnsureSexes(profile.Sex, _markingManager);
        markings.EnsureDefault(
            profile.Appearance.SkinColor,
            profile.Appearance.EyeColor,
            _markingManager);

        DebugTools.Assert(IsClientSide(uid));

        humanoid.MarkingSet = markings;
        humanoid.PermanentlyHidden = new HashSet<HumanoidVisualLayers>();
        humanoid.HiddenLayers = new Dictionary<HumanoidVisualLayers, SlotFlags>();
        humanoid.CustomBaseLayers = customBaseLayers;
        humanoid.Sex = profile.Sex;
        humanoid.Gender = profile.Gender;
        humanoid.Age = profile.Age;
        humanoid.BodyType = profile.BodyType; // Amour port: WD Slim body types
        humanoid.Species = profile.Species;
        humanoid.SkinColor = profile.Appearance.SkinColor;
        humanoid.EyeColor = profile.Appearance.EyeColor;
        humanoid.Height = profile.Height; // Goobstation: port EE height/width sliders
        humanoid.Width = profile.Width; // Goobstation: port EE height/width sliders

        UpdateSprite((uid, humanoid, Comp<SpriteComponent>(uid)));
    }

    private void ApplyMarkingSet(Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        var humanoid = entity.Comp1;
        var sprite = entity.Comp2;

        // I am lazy and I CBF resolving the previous mess, so I'm just going to nuke the markings.
        // Really, markings should probably be a separate component altogether.
        ClearAllMarkings(entity);

        var censorNudity = _configurationManager.GetCVar(CCVars.AccessibilityClientCensorNudity) ||
                           _configurationManager.GetCVar(CCVars.AccessibilityServerCensorNudity);
        // The reason we're splitting this up is in case the character already has undergarment equipped in that slot.
        var applyUndergarmentTop = censorNudity;
        var applyUndergarmentBottom = censorNudity;

        foreach (var markingList in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (_markingManager.TryGetMarking(marking, out var markingPrototype))
                {
                    ApplyMarking(markingPrototype, marking, entity); // Amour edit
                    if (markingPrototype.BodyPart == HumanoidVisualLayers.UndergarmentTop)
                        applyUndergarmentTop = false;
                    else if (markingPrototype.BodyPart == HumanoidVisualLayers.UndergarmentBottom)
                        applyUndergarmentBottom = false;
                }
            }
        }

        humanoid.ClientOldMarkings = new MarkingSet(humanoid.MarkingSet);

        AddUndergarments(entity, applyUndergarmentTop, applyUndergarmentBottom);
    }

    private void ClearAllMarkings(Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        var humanoid = entity.Comp1;
        var sprite = entity.Comp2;

        foreach (var markingList in humanoid.ClientOldMarkings.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                RemoveMarking(marking, (entity, sprite));
            }
        }

        humanoid.ClientOldMarkings.Clear();

        foreach (var markingList in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                RemoveMarking(marking, (entity, sprite));
            }
        }
    }

    private void RemoveMarking(Marking marking, Entity<SpriteComponent> spriteEnt)
    {
        if (!_markingManager.TryGetMarking(marking, out var prototype))
        {
            return;
        }

        foreach (var sprite in prototype.Sprites)
        {
            if (sprite is not SpriteSpecifier.Rsi rsi)
            {
                continue;
            }

            var layerId = $"{marking.MarkingId}-{rsi.RsiState}";
            _displacement.EnsureDisplacementIsNotOnSprite(spriteEnt, layerId);
            _sprite.RemoveLayer(spriteEnt.AsNullable(), layerId, false);
        }
    }
    // Amour edit start
    private void ApplyMarking(MarkingPrototype markingPrototype,
        Marking marking,
        Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        if (UsesBodyLayerGradient(markingPrototype))
        {
            ApplyBodyLayerGradient(markingPrototype, marking, entity);
            return;
        }

        ApplyMarking(markingPrototype, marking.MarkingColors, marking.Visible, entity, marking);
    }

    private void ApplyBodyLayerGradient(
        MarkingPrototype markingPrototype,
        Marking marking,
        Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        if (!marking.Visible)
            return;

        var humanoid = entity.Comp1;
        var sprite = entity.Comp2;
        var color1 = marking.MarkingColors.Count > 0 ? marking.MarkingColors[0] : humanoid.SkinColor;
        ShaderPrototype? gradientProto = null;

        if (MarkingSupportsGradient(markingPrototype) && marking.UseGradient)
        {
            if (!_prototypeManager.TryIndex<ShaderPrototype>(MarkingGradientShaderId, out gradientProto))
                return;
        }

        var appliedLayers = new HashSet<HumanoidVisualLayers>();
        for (var i = 0; i < markingPrototype.Sprites.Count; i++)
        {
            var bodyPart = markingPrototype.GetBodyPart(i);
            if (!appliedLayers.Add(bodyPart) ||
                !_sprite.LayerMapTryGet((entity.Owner, sprite), bodyPart, out var layerIndex, false) ||
                !humanoid.BaseLayers.TryGetValue(bodyPart, out var baseLayer) ||
                !baseLayer.MatchSkin)
            {
                continue;
            }

            if (gradientProto != null)
            {
                var shaderInstance = gradientProto.InstanceUnique();
                SetGradientShaderParameters(shaderInstance, marking, 0, alpha: baseLayer.LayerAlpha);
                sprite.LayerSetShader(layerIndex, shaderInstance);
                sprite[layerIndex].Color = Color.White;
            }
            else
            {
                sprite.LayerSetShader(layerIndex, null, null);
                sprite[layerIndex].Color = color1.WithAlpha(baseLayer.LayerAlpha);
            }
        }
    }
    // Amour edit end

    private void AddUndergarments(Entity<HumanoidAppearanceComponent, SpriteComponent> entity, bool undergarmentTop, bool undergarmentBottom)
    {
        var humanoid = entity.Comp1;

        if (undergarmentTop && humanoid.UndergarmentTop != null)
        {
            var marking = new Marking(humanoid.UndergarmentTop, new List<Color> { new Color() });
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                // Markings are added to ClientOldMarkings because otherwise it causes issues when toggling the feature on/off.
                humanoid.ClientOldMarkings.Markings.Add(MarkingCategories.UndergarmentTop, new List<Marking> { marking });
                ApplyMarking(prototype, null, true, entity);
            }
        }

        if (undergarmentBottom && humanoid.UndergarmentBottom != null)
        {
            var marking = new Marking(humanoid.UndergarmentBottom, new List<Color> { new Color() });
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                humanoid.ClientOldMarkings.Markings.Add(MarkingCategories.UndergarmentBottom, new List<Marking> { marking });
                ApplyMarking(prototype, null, true, entity);
            }
        }
    }

    private void ApplyMarking(MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        bool visible,
        Entity<HumanoidAppearanceComponent, SpriteComponent> entity,
        Marking? marking = null) // Amour edit
    {
        var humanoid = entity.Comp1;
        var sprite = entity.Comp2;

        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            var markingSprite = markingPrototype.Sprites[j];

            if (markingSprite is not SpriteSpecifier.Rsi rsi)
            {
                continue;
            }

            // Amour edit start
            var bodyPart = markingPrototype.GetBodyPart(j);
            if (!_sprite.LayerMapTryGet((entity.Owner, sprite), bodyPart, out var targetLayer, false))
            {
                continue;
            }

            var layerVisible = visible && !IsHidden(humanoid, bodyPart);
            var hasSetting = humanoid.BaseLayers.TryGetValue(bodyPart, out var setting);
            layerVisible &= hasSetting && setting is { AllowsMarkings: true };
            var layerOffset = GetBodyPartSpriteOffset(markingPrototype, bodyPart, j);
            // Amour edit end
            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (!_sprite.LayerMapTryGet((entity.Owner, sprite), layerId, out var layer, false)) // Goob edit
            {
                layer = _sprite.AddLayer((entity.Owner, sprite), markingSprite, targetLayer + layerOffset); // Goob edit
                _sprite.LayerMapSet((entity.Owner, sprite), layerId, layer);
                _sprite.LayerSetSprite((entity.Owner, sprite), layerId, rsi);
            }

            var hasInfo = humanoid.CustomBaseLayers.TryGetValue(bodyPart, out var info); // Goobstation

            // Amour edit start
            var gradientApplied = false;
            if (MarkingSupportsGradient(markingPrototype) && marking is { UseGradient: true } && colors != null && j < colors.Count)
            {
                if (_prototypeManager.TryIndex<ShaderPrototype>(MarkingGradientShaderId, out var gradientProto))
                {
                    var shaderInstance = gradientProto.InstanceUnique();
                    // Mix the per-layer humanoid tint into each stop so existing
                    // skin-tint logic still affects the gradient.
                    SetGradientShaderParameters(shaderInstance, marking, j, hasInfo ? info.Color : null);
                    sprite.LayerSetShader(layer, shaderInstance);
                    // The shader already tints the texture, so reset the layer
                    // modulate to white to avoid double-tinting.
                    _sprite.LayerSetColor((entity.Owner, sprite), layerId, Color.White);
                    gradientApplied = true;
                }
            }

            if (!gradientApplied)
            {
                // impstation edit begin - check if there's a shader defined in the markingPrototype's shader datafield, and if there is...
                if (markingPrototype.Shader != null)
                {
                    // use spriteComponent's layersetshader function to set the layer's shader to that which is specified.
                    sprite.LayerSetShader(layer, markingPrototype.Shader); // Goob edit
                }
                else // Goobstation
                {
                    if (hasInfo && info.Shader != null)
                        sprite.LayerSetShader(layer, info.Shader);
                    else
                        sprite.LayerSetShader(layer, null, null);
                }
                // impstation edit end
            }

            _sprite.LayerSetVisible((entity.Owner, sprite), layerId, layerVisible);

            if (!layerVisible || !hasSetting) // this is kinda implied
            {
                continue;
            }

            if (gradientApplied)
            {
                if (humanoid.MarkingsDisplacement.TryGetValue(bodyPart, out var dispData) && markingPrototype.CanBeDisplaced)
                {
                    _displacement.TryAddDisplacement(dispData, (entity.Owner, sprite), targetLayer + layerOffset, layerId, out _);
                }
                continue;
            }
            // Amour edit end

            // Okay so if the marking prototype is modified but we load old marking data this may no longer be valid
            // and we need to check the index is correct.
            // So if that happens just default to white?
            if (colors != null && j < colors.Count)
            {
                // Goob edit start
                var color = colors[j];
                if (hasInfo && info.Color != null)
                    color = Color.InterpolateBetween(color, info.Color.Value, 0.5f);
                _sprite.LayerSetColor((entity.Owner, sprite), layerId, color);
                // Goob edit end
            }
            else
            {
                // Goob edit start
                var color = Color.White;
                if (hasInfo && info.Color != null)
                    color = info.Color.Value;
                _sprite.LayerSetColor((entity.Owner, sprite), layerId, color);
                // Goob edit end
            }

            if (humanoid.MarkingsDisplacement.TryGetValue(bodyPart, out var displacementData) && markingPrototype.CanBeDisplaced) // Amour edit
            {
                _displacement.TryAddDisplacement(displacementData, (entity.Owner, sprite), targetLayer + layerOffset, layerId, out _); // Amour edit
            }
        }
    }

    public override void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, bool verify = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || humanoid.SkinColor == skinColor)
            return;

        base.SetSkinColor(uid, skinColor, false, verify, humanoid);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        foreach (var (layer, spriteInfo) in humanoid.BaseLayers)
        {
            if (!spriteInfo.MatchSkin)
                continue;

            var index = _sprite.LayerMapReserve((uid, sprite), layer);
            sprite.LayerSetShader(index, null, null); // Amour edit
            sprite[index].Color = skinColor.WithAlpha(spriteInfo.LayerAlpha);
        }
    }

    public override void SetLayerVisibility(
        Entity<HumanoidAppearanceComponent> ent,
        HumanoidVisualLayers layer,
        bool visible,
        SlotFlags? slot,
        ref bool dirty)
    {
        base.SetLayerVisibility(ent, layer, visible, slot, ref dirty);

        var sprite = Comp<SpriteComponent>(ent);
        if (!_sprite.LayerMapTryGet((ent.Owner, sprite), layer, out var index, false))
        {
            if (!visible)
                return;
            index = _sprite.LayerMapReserve((ent.Owner, sprite), layer);
        }

        var spriteLayer = sprite[index];
        if (spriteLayer.Visible == visible)
            return;

        spriteLayer.Visible = visible;

        if (_sprite.LayerMapTryGet((ent.Owner, sprite), $"{layer}-displacement", out var dispIndex, false))
        {
            sprite[dispIndex].Visible = visible;
        }

        // I fucking hate this. I'll get around to refactoring sprite layers eventually I swear
        // Just a week away...

        foreach (var markingList in ent.Comp.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (_markingManager.TryGetMarking(marking, out var markingPrototype) && markingPrototype.AppliesToBodyPart(layer)) // Amour edit
                    ApplyMarking(markingPrototype, marking, (ent, ent.Comp, sprite)); // Amour edit
            }
        }
    }
}
