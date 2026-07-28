using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /*
     * Flavor content
     */

    public static readonly CVarDef<bool> FlavorTraitsEnabled =
        CVarDef.Create("ic.flavor_traits_enabled", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> FlavorGyrEnabled =
        CVarDef.Create("ic.flavor_gyr_enabled", true, CVar.SERVER | CVar.REPLICATED);



    /*
     * Flavor settings
     */


    /// <summary>
    ///     Sets the maximum length for character description text.
    /// </summary>
    public static readonly CVarDef<int> CharacterDescriptionLength =
        CVarDef.Create("ic.character_description_length", 4500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for green preferences text.
    /// </summary>
    public static readonly CVarDef<int> GreenPreferencesLength =
        CVarDef.Create("ic.green_preferences_length", 4500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for yellow preferences text.
    /// </summary>
    public static readonly CVarDef<int> YellowPreferencesLength =
        CVarDef.Create("ic.yellow_preferences_length", 4500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for red preferences text.
    /// </summary>
    public static readonly CVarDef<int> RedPreferencesLength =
        CVarDef.Create("ic.red_preferences_length", 4500, CVar.SERVER | CVar.REPLICATED);

}
