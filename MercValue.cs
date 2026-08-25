using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using ImGuiNET;
using ItemFilterLibrary;
using Newtonsoft.Json;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace MercValue;

public class MercValue : BaseSettingsPlugin<MercValueSettings>
{
    private static readonly HttpClient HttpClient = new();
    private const string PoeNinjaExchangeOverviewEndpoint = "economy/exchange/current/overview";
    private const string PoeNinjaItemOverviewEndpoint = "economy/stash/current/item/overview";
    private const int HighlightFrameThickness = 3;
    private static readonly string[] CurrencyTypes = ["Currency", "Scarab"];
    private static readonly string[] UniqueTypes = ["UniqueWeapon", "UniqueArmour", "UniqueAccessory"];

    // All known mercenary classes, alphabetical, for the Merc Type Alerts dropdown.
    private static readonly string[] MercTypes =
    [
        "Bastion", "Blade Ambusher", "Bladebitter", "Bladecaster", "Bloodletter", "Cardinal",
        "Combatant", "Cruel Mistress", "Earthshaker", "Eruptor", "Fallen Reverend", "Flamehand",
        "Flamequiver", "Flaming Charlatan", "Frost Ambusher", "Frosthand", "Kineticist", "Manyshot",
        "Mysterious Diver", "Reanimator", "Ripper", "Sanguimancer", "Shattersword", "Shock Ambusher",
        "Smoulderstrike", "Sniper", "Storming Zealot", "Stormhand", "Striker", "Swiftblade",
        "Thunderquiver", "Toxicologist", "Warpriest", "Winter Deacon", "Withertouch"
    ];

    // Every possible skill each mercenary class can roll, from https://poedb.tw/us/Mercenaries.
    // A merc's actual encounter only shows a subset of these (confirmed: a live Striker
    // encounter showed 6 of this list's 12 skills). "Infamous X" entries on poedb are
    // identical to their base class - not duplicated here, since MercType is already
    // stripped of the "Infamous " prefix before it's used as a lookup key.
    private static readonly Dictionary<string, string[]> MercSkillPool = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bastion"] = ["Shield Crush", "Shield Charge", "Spectral Shield Throw of Trarthus", "Tempest Shield", "Prismplate", "Determination", "Dash", "Intimidating Cry", "Physical Aegis", "Impenetrable Bastion"],
        ["Blade Ambusher"] = ["Blade Trap", "Trarthan Agility", "Spectral Throw of Trarthus", "Spectral Helix of Trarthus", "Smoke Mine", "Flame Dash", "Grace", "Summon Skitterbots", "Bear Trap"],
        ["Bladebitter"] = ["Viper Strike", "Abyssal Cry", "Venom Gyre", "Profane Strike", "Cobra Lash", "Pestilent Strike", "Whirling Blades", "Dash", "Malevolence", "Withering Step", "Grace"],
        ["Bladecaster"] = ["Clutches of the Damned", "Bloody Warp", "Seismic Crush", "Bladefall", "Blade Vortex", "Ethereal Knives", "Bladefall of Trarthus", "Dash", "Grace", "Flame Dash", "Aspect of the Spider", "Frost Shield"],
        ["Bloodletter"] = ["Bloodthirst", "Blood Mortar", "Lacerate", "Bladestorm", "Perforate", "Leap Slam", "Dash", "Determination", "Oil Slick"],
        ["Cardinal"] = ["Holy Hammers", "Holy Strike", "Holy Sweep", "Sanctified Strike", "Consecrated Path", "Purity of Lightning", "Flame Dash", "Herald of Purity", "Wrath"],
        ["Combatant"] = ["Inspiring Cry", "Herald of Ice", "Frost Blades", "Wild Strike", "Spectral Helix", "Static Strike", "Wrath", "Dash", "Frostblink", "Purity of Ice"],
        ["Cruel Mistress"] = ["Despair", "Void Sphere", "Dark Bargain", "Voltaxic Burst", "Soulrend of Reaping", "Forbidden Rite Totem", "Summon Seeking Void", "Profane Cascade", "Zealotry", "Lightning Warp", "Frostblink", "Flame Dash", "Envy"],
        ["Earthshaker"] = ["Intimidating Cry", "Leap Slam", "Vaal Ground Slam", "Ground Slam", "Earthquake of Amplification", "Tectonic Cascade", "Sunder of Trarthus", "Determination", "Molten Shell", "Burrow", "Endure", "Vaal Vitality"],
        ["Eruptor"] = ["Infernal Cry", "Leap Slam", "Triggerslam", "Volcanic Fissure of Snaking", "Consecrated Path", "Tectonic Slam", "Ashen Fissure", "Molten Shell", "Determination", "Purity of Fire", "Herald of Ash", "Flame Link"],
        ["Fallen Reverend"] = ["Absolution", "Raise Spectre of Transience", "Desecrate", "Reinforce: Fallen Bishop", "Reinforce: Fallen Emperor", "Reinforce: Fallen Osseotitan", "Wrath", "Zealotry", "Battlemage's Cry", "Flame Dash", "Frostblink"],
        ["Flamehand"] = ["Flame Wall", "Flammability", "Greater Flame Dash", "Flameblast", "Vaal Flameblast", "Fireball", "Fireball of Impact", "Rolling Magma", "Flame Surge", "Lightning Warp", "Frostblink", "Purity of Fire", "Anger", "Flame Aegis"],
        ["Flamequiver"] = ["Burning Arrow", "Alchemist's Mark", "Vaal Burning Arrow", "Molten Well", "Artillery Ballista", "Explosive Arrow", "Blast Rain of Trarthus", "Conflagration", "Grace", "Blink Arrow", "Dash", "Blasphemy Flammability"],
        ["Flaming Charlatan"] = ["Holy Flame Totem", "Purifying Flame", "Holy Mortar", "Wave of Conviction of Trarthus", "Scorching Ray Totem", "Meteor", "Lightning Warp", "Flame Dash", "Herald of Ash", "Anger"],
        ["Frost Ambusher"] = ["Frost Shield", "Greater Vortex Trap", "Ice Trap", "Creeping Frost Trap", "Summon Skitterbots", "Flame Dash", "Frostblink", "Grace", "Arctic Armour", "Vaal Arctic Armour"],
        ["Frosthand"] = ["Frost Bomb", "Frostbite", "Frost Wall", "Eye of Winter", "Vortex", "Frostbolt", "Ice Nova", "Ice Nova of Projection", "Icestorm", "Lightning Warp", "Frostblink", "Flame Dash", "Purity of Ice", "Discipline"],
        ["Kineticist"] = ["Elemental Weakness", "Flame Wall", "Kinetic Bolt", "Power Siphon", "Barrage", "Greater Kinetic Blast", "Kinetic Blast of Clustering", "Kinetic Rain of Impact", "Flame Dash", "Frostblink", "Haste", "Inspiring Cry"],
        ["Manyshot"] = ["Ice Shot", "Vaal Grace", "Vaal Ice Shot", "Frigid Forkshot", "Icicle Rain", "Mirror Arrow", "Hatred", "Frost Bomb", "Grace", "Dash", "Blink Arrow"],
        ["Mysterious Diver"] = ["Elemental Hit of Ice", "Frost Bomb", "Frost Blades", "Wild Strike", "Herald of Ice", "Hatred", "Purity of Ice", "Frostblink", "Dash"],
        ["Reanimator"] = ["Raise Zombie of Gigantism", "Desecrate", "Raise Zombie of Falling", "Summon Skeletons", "Flesh Offering", "Bone Offering", "Summon Raging Spirit", "Lightning Warp", "Frostblink", "Flame Dash", "Blasphemy Enfeeble", "Relic of Binding"],
        ["Ripper"] = ["Tunnelslam", "Enrage", "Cleave", "Vaal Cleave", "Slicing Wind", "Chain Hook of Trarthus", "Leap Slam of Groundbreaking", "Molten Shell", "Determination", "Leap Slam", "Vitality"],
        ["Sanguimancer"] = ["Boiling Blood", "Storm Call of Trarthus", "Bodyswap", "Exsanguinate", "Vaal Reap", "Reap", "Corrupted Blade Vortex of the Scythe", "Flame Dash", "Lightning Warp", "Pride", "Proximity Shield", "Vaal Vitality"],
        ["Shattersword"] = ["Call of Steel", "Rallying Cry", "Triggerblades", "Shattering Steel", "Splitting Steel", "Lancing Steel", "Dash", "Whirling Blades", "Grace", "Brutal Teachings"],
        ["Shock Ambusher"] = ["Conductivity", "Vaal Lightning Trap", "Lightning Trap", "Lightning Spire Trap", "Ball Lightning of Orbiting Trap", "Lightning Warp Trap", "Flame Dash", "Frostblink", "Smoke Mine", "Summon Skitterbots", "Zealotry"],
        ["Smoulderstrike"] = ["Punishment", "Inflame", "Vaal Molten Strike", "Infernal Blow of Immolation", "Molten Strike", "Flamebolt Strike", "Anger", "Leap Slam", "Molten Shell", "Flame Aegis", "Infernal Cry"],
        ["Sniper"] = ["Ensnaring Arrow", "Poacher's Mark", "Greater Split Arrow", "Rain of Arrows of Saturation", "Tornado Shot", "Shrapnel Ballista", "Barrage of Volley Fire", "Siege Ballista of Trarthus", "Split Arrow", "Puncture", "Grace", "Blink Arrow", "Dash", "Frenzy", "Haste"],
        ["Storming Zealot"] = ["Unnerving Blast", "Wave of Conviction", "Divine Ire", "Tornado of Elemental Turbulence", "Divine Retribution", "Shockwave Totem of Shocking", "Charged Dash of the Arcane", "Wrath", "Lightning Warp", "Flame Dash"],
        ["Stormhand"] = ["Conductivity", "Sigil of Power", "Spark", "Arc", "Ball Lightning of Static", "Greater Stormcall", "Greater Shock Nova", "Orb of Storms", "Stormcall", "Lightning Warp", "Frostblink", "Flame Dash", "Wrath", "Purity of Lightning", "Proximity Shield"],
        ["Striker"] = ["Vigilant Strike", "Enduring Cry", "Physical Aegis", "Heavy Strike of Vulnerability", "Boneshatter", "Dual Strike", "Determination", "Leap Slam", "Molten Shell", "Vitality", "Vaal Ancestral Warchief", "Decoy Totem"],
        ["Swiftblade"] = ["Cyclone of the Empire", "Rallying Cry", "Triggerblades", "Flicker Strike", "Vaal Double Strike", "Pride", "Leap Slam", "Incite", "Dash", "Steelskin Ally"],
        ["Thunderquiver"] = ["Lightning Arrow", "Assassin's Mark", "Galvanic Arrow", "Greater Lightning Arrow", "Vaal Lightning Arrow", "Storm Rain", "Wrath", "Blink Arrow", "Trarthan Agility", "Precision", "Dash"],
        ["Toxicologist"] = ["Withering Step", "Chaotic Burst", "Chaotic Shot", "Vaal Caustic Arrow", "Toxic Rain", "Scourge Arrow of Menace", "Caustic Arrow", "Grace", "Blink Arrow", "Dash", "Malevolence", "Trarthan Agility"],
        ["Warpriest"] = ["Holy Relic", "Smite", "Beacons of Faith", "Dominating Blow", "Raise Spectre: Holy Flame Elementals", "Touch of God", "Purity of Lightning", "Flame Dash", "Herald of Purity"],
        ["Winter Deacon"] = ["Frost Shield", "Vaal Glacial Hammer", "Ice Crash", "Glacial Hammer", "Earthquake of Winter", "Heavy Strike of Trarthus", "Hatred", "Frostblink", "Frost Bomb", "Arctic Armour", "Purity of Ice"],
        ["Withertouch"] = ["Bane", "Temporal Chains", "Greater Soulrend", "Scourstorm", "Blight", "Essence Drain of Wickedness", "Altar of Chaos", "Malevolence", "Lightning Warp", "Frostblink", "Flame Dash", "Clarity", "Withering Step", "Wither Totem"],
    };

    private Dictionary<string, float> _priceByName = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, float> _uniquePriceByName = new(StringComparer.OrdinalIgnoreCase);
    private bool _isFetchingPrices;
    private DateTime _lastPriceFetchAttempt = DateTime.MinValue;
    private string _lastUpdatedText = "never";
    private string _lastFetchStatus = "Not yet fetched";
    private bool? _lastFetchWasError;
    private readonly HashSet<string> _loggedUnmatchedUniques = new(StringComparer.OrdinalIgnoreCase);
    private int _selectedMercTypeIndex;

    public MercValue()
    {
        Name = "MercValue";
    }

    public override bool Initialise()
    {
        Settings.RefreshPricesNow.OnPressed = QueuePriceFetch;
        Settings.PriceStatusPanel.DrawDelegate = DrawPriceStatusPanel;
        Settings.QualifyingUniquesPanel.DrawDelegate = DrawQualifyingUniquesPanel;
        Settings.MercTypeAlertsPanel.DrawDelegate = DrawMercTypeAlertsPanel;
        QueuePriceFetch();
        return true;
    }

    private void DrawPriceStatusPanel()
    {
        ImGui.Text($"Prices as of: {_lastUpdatedText}");

        var statusColor = _lastFetchWasError switch
        {
            true => new Vector4(1f, 0.35f, 0.35f, 1f),
            false => new Vector4(0.4f, 1f, 0.4f, 1f),
            null => new Vector4(0.7f, 0.7f, 0.7f, 1f)
        };
        ImGui.TextColored(statusColor, _lastFetchStatus);
    }

    private void DrawQualifyingUniquesPanel()
    {
        var threshold = Settings.ShowUniquesValueAbove.Value;
        var qualifying = _uniquePriceByName
            .Where(kv => kv.Value >= threshold)
            .OrderByDescending(kv => kv.Value)
            .ToList();

        if (!ImGui.CollapsingHeader($"{qualifying.Count} unique(s) at or above {threshold}c##QualifyingUniquesHeader"))
            return;

        if (!ImGui.BeginTable("##MercValueQualifyingUniques", 2,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.ScrollY,
                new Vector2(0, 300)))
            return;

        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (name, value) in qualifying)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{RoundChaos(value)}c");
            ImGui.TableNextColumn();
            ImGui.Text(name);
        }

        ImGui.EndTable();
    }

    private void DrawMercTypeAlertsPanel()
    {
        ImGui.SetNextItemWidth(220);
        ImGui.Combo("##MercTypePicker", ref _selectedMercTypeIndex, MercTypes, MercTypes.Length);
        ImGui.SameLine();

        if (ImGui.Button("Add"))
        {
            var selected = MercTypes[_selectedMercTypeIndex];
            var alreadyAdded = Settings.MercTypeAlerts.Any(a => string.Equals(a.Type, selected, StringComparison.OrdinalIgnoreCase));
            if (!alreadyAdded)
            {
                Settings.MercTypeAlerts.Add(new MercTypeAlertEntry
                {
                    Type = selected,
                    Message = $"{selected} detected",
                    Enabled = true
                });
            }
        }

        ImGui.Separator();

        if (!ImGui.BeginTable("##MercValueMercTypeAlerts", 4,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV))
            return;

        ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableHeadersRow();

        MercTypeAlertEntry toRemove = null;
        foreach (var alert in Settings.MercTypeAlerts)
        {
            ImGui.PushID(alert.Type);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var enabled = alert.Enabled;
            if (ImGui.Checkbox("##enabled", ref enabled))
                alert.Enabled = enabled;

            ImGui.TableNextColumn();
            ImGui.Text(alert.Type);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var message = alert.Message;
            if (ImGui.InputText("##message", ref message, 128))
                alert.Message = message;

            ImGui.TableNextColumn();
            if (ImGui.Button("Remove"))
                toRemove = alert;

            ImGui.PopID();
        }

        if (toRemove != null)
            Settings.MercTypeAlerts.Remove(toRemove);

        ImGui.EndTable();

        // One expandable skill picker per added merc type - outside the table above since
        // ImGui tables don't handle variable-height per-row content well. Skill list comes
        // from MercSkillPool (poedb.tw/us/Mercenaries); Unknown for a type not in that map
        // shouldn't happen since MercTypes and MercSkillPool are meant to stay in sync.
        foreach (var alert in Settings.MercTypeAlerts)
        {
            if (!MercSkillPool.TryGetValue(alert.Type, out var skills)) continue;

            ImGui.PushID(alert.Type);
            if (ImGui.CollapsingHeader($"{alert.Type} skills##skills"))
            {
                foreach (var skill in skills)
                {
                    ImGui.PushID(skill);
                    var state = GetSkillPreference(alert.Type, skill);
                    ImGui.TextUnformatted(skill);
                    ImGui.SameLine(260);

                    var desiredActive = state == SkillPreferenceState.Desired;
                    if (desiredActive) ImGui.PushStyleColor(ImGuiCol.Button, ToImGuiColor(Settings.DesiredSkillColor));
                    if (ImGui.Button("Desired"))
                        SetSkillPreference(alert.Type, skill, desiredActive ? SkillPreferenceState.Unset : SkillPreferenceState.Desired);
                    if (desiredActive) ImGui.PopStyleColor();

                    ImGui.SameLine();

                    var brickActive = state == SkillPreferenceState.Brick;
                    if (brickActive) ImGui.PushStyleColor(ImGuiCol.Button, ToImGuiColor(Settings.BrickSkillColor));
                    if (ImGui.Button("Brick"))
                        SetSkillPreference(alert.Type, skill, brickActive ? SkillPreferenceState.Unset : SkillPreferenceState.Brick);
                    if (brickActive) ImGui.PopStyleColor();

                    ImGui.PopID();
                }
            }
            ImGui.PopID();
        }
    }

    private SkillPreferenceState GetSkillPreference(string mercType, string skill)
    {
        var entry = Settings.SkillPreferences.FirstOrDefault(p =>
            string.Equals(p.MercType, mercType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.SkillName, skill, StringComparison.OrdinalIgnoreCase));
        return entry?.State ?? SkillPreferenceState.Unset;
    }

    private void SetSkillPreference(string mercType, string skill, SkillPreferenceState state)
    {
        var entry = Settings.SkillPreferences.FirstOrDefault(p =>
            string.Equals(p.MercType, mercType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.SkillName, skill, StringComparison.OrdinalIgnoreCase));

        if (state == SkillPreferenceState.Unset)
        {
            if (entry != null) Settings.SkillPreferences.Remove(entry);
            return;
        }

        if (entry != null) entry.State = state;
        else Settings.SkillPreferences.Add(new MercSkillPreference { MercType = mercType, SkillName = skill, State = state });
    }

    public override Job Tick()
    {
        TryScheduleAutoPriceRefresh();
        return null;
    }

    // The class name ("Sniper", "Kineticist", etc.) lives at this fixed spot in the
    // encounter window's UI tree - there's no semantic name for it, only position.
    // Infamous mercs show it with an "Infamous " prefix baked into the same text
    // (e.g. "Infamous Warpriest"), so that's parsed out separately here.
    private static string GetMercenaryClassName(Element window)
    {
        var classElement = window?.Children.ElementAtOrDefault(2)?.Children.ElementAtOrDefault(4)?.Children.ElementAtOrDefault(1);
        var text = classElement?.TextNoTags;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    // The merc's rolled skill list (a subset of MercSkillPool for its class) - found by
    // dumping the encounter window's UI element tree: window.2.10.0.1.0.{row}.1 holds each
    // skill's name text, same fixed-position approach as the class name lookup above.
    // Returns both the whole row (icon + name + description, used for the border) and just
    // the icon's rect (used for the Brick X) - there's no separate icon element in the
    // window tree, so the icon rect is derived as the gap between the row's own left edge
    // and where the name text starts, which lined up consistently (~83px) across every
    // skill row in the sample dump.
    private static List<(string Name, RectangleF RowRect, RectangleF IconRect)> GetMercenarySkills(Element window)
    {
        var result = new List<(string, RectangleF, RectangleF)>();
        var container = window?.Children.ElementAtOrDefault(2)?.Children.ElementAtOrDefault(10)
            ?.Children.ElementAtOrDefault(0)?.Children.ElementAtOrDefault(1)?.Children.ElementAtOrDefault(0);
        if (container == null) return result;

        foreach (var row in container.Children)
        {
            var textElement = row?.Children.ElementAtOrDefault(1);
            var text = textElement?.TextNoTags;
            if (string.IsNullOrWhiteSpace(text)) continue;

            var rowRect = row.GetClientRectCache;
            var textRect = textElement.GetClientRectCache;
            var iconWidth = Math.Max(0, textRect.X - rowRect.X);
            var iconRect = new RectangleF(rowRect.X, rowRect.Y, iconWidth, rowRect.Height);
            result.Add((text.Trim(), rowRect, iconRect));
        }
        return result;
    }

    private const string InfamousPrefix = "Infamous ";

    private static (string BaseType, bool IsInfamous) ParseMercType(string rawClassName)
    {
        if (string.IsNullOrEmpty(rawClassName)) return (rawClassName, false);

        if (rawClassName.StartsWith(InfamousPrefix, StringComparison.OrdinalIgnoreCase))
            return (rawClassName[InfamousPrefix.Length..].Trim(), true);

        return (rawClassName, false);
    }

    public override void Render()
    {
        var window = GameController.IngameState.IngameUi.MercenaryEncounterWindow;
        if (window is not { IsVisible: true }) return;

        var (mercType, _) = ParseMercType(GetMercenaryClassName(window));
        var mercTypeAlertMessage = mercType != null
            ? Settings.MercTypeAlerts.FirstOrDefault(a => a.Enabled && string.Equals(a.Type, mercType, StringComparison.OrdinalIgnoreCase))?.Message
            : null;

        var total = 0f;
        RectangleF? currencyInventoryRect = null;
        var uniqueCandidates = new List<(string Name, float Value, RectangleF Rect)>();

        foreach (var inv in window.Inventories)
        {
            var invRect = inv.GetClientRectCache;
            foreach (var uiItem in inv.VisibleInventoryItems)
            {
                if (uiItem.Item is not { Address: not 0, IsValid: true }) continue;

                var data = new ItemData(uiItem.Item, GameController);

                var currencyValue = GetItemChaosValue(data);
                if (currencyValue > 0)
                {
                    total += currencyValue;
                    // Multiple currency/scarab items typically share one "rucksack" inventory -
                    // union the rect in case they're ever split across more than one.
                    currencyInventoryRect = currencyInventoryRect.HasValue
                        ? RectangleF.Union(currencyInventoryRect.Value, invRect)
                        : invRect;
                }

                var uniqueValue = GetUniqueChaosValue(data);
                if (uniqueValue >= Settings.ShowUniquesValueAbove.Value)
                {
                    uniqueCandidates.Add((data.Name, uniqueValue, uiItem.GetClientRectCache));
                }
            }
        }

        var qualifyingUniques = uniqueCandidates.OrderByDescending(x => x.Value).ToList();
        var topUnique = qualifyingUniques.Count > 0 ? qualifyingUniques[0] : ((string Name, float Value, RectangleF Rect)?)null;

        var displayedUniques = Settings.ShowAllQualifyingUniques.Value
            ? qualifyingUniques
            : qualifyingUniques.Take(Settings.TopUniquesToShow.Value).ToList();

        DrawTotalWindow(total, displayedUniques.Select(x => (x.Name, x.Value)).ToList(), mercTypeAlertMessage);

        // Same "you can only take one" comparison used for the value box's text colors,
        // reused here so the on-screen frames agree with it.
        var (currencyMeetsBar, uniqueIsBetterPick) = ComparePick(total, topUnique?.Value);

        if (topUnique.HasValue && uniqueIsBetterPick)
        {
            Graphics.DrawFrame(topUnique.Value.Rect, Settings.MeetsMinimumColor, HighlightFrameThickness);
        }

        if (currencyInventoryRect.HasValue && currencyMeetsBar)
        {
            Graphics.DrawFrame(currencyInventoryRect.Value, Settings.MeetsMinimumColor, HighlightFrameThickness);
        }

        if (mercType != null)
        {
            foreach (var (skillName, rowRect, iconRect) in GetMercenarySkills(window))
            {
                var state = GetSkillPreference(mercType, skillName);
                if (state == SkillPreferenceState.Unset) continue;

                if (state == SkillPreferenceState.Desired)
                {
                    Graphics.DrawFrame(rowRect, Settings.DesiredSkillColor, HighlightFrameThickness);
                }
                else
                {
                    Graphics.DrawFrame(rowRect, Settings.BrickSkillColor, HighlightFrameThickness);
                    var topLeft = new Vector2(iconRect.X, iconRect.Y);
                    var topRight = new Vector2(iconRect.X + iconRect.Width, iconRect.Y);
                    var bottomLeft = new Vector2(iconRect.X, iconRect.Y + iconRect.Height);
                    var bottomRight = new Vector2(iconRect.X + iconRect.Width, iconRect.Y + iconRect.Height);
                    Graphics.DrawLine(topLeft, bottomRight, HighlightFrameThickness, Settings.BrickSkillColor);
                    Graphics.DrawLine(topRight, bottomLeft, HighlightFrameThickness, Settings.BrickSkillColor);
                }
            }
        }
    }

    private float GetItemChaosValue(ItemData item)
    {
        if (string.IsNullOrEmpty(item.BaseName) || !_priceByName.TryGetValue(item.BaseName, out var chaosEach))
            return 0f;

        var count = Math.Max(1, item.StackInfo?.Count ?? 1);
        return chaosEach * count;
    }

    // .NET's default "{value:0}" formatting rounds half-to-even (banker's rounding), so 7.5
    // can display as "8" but 6.5 displays as "6". Round explicitly (half away from zero, i.e.
    // .5 and above rounds up) so displayed chaos values behave the way a person expects.
    private static int RoundChaos(float value) => (int)MathF.Round(value, MidpointRounding.AwayFromZero);

    // Compares using the same rounded whole-chaos values shown on screen, so a tie in the
    // displayed numbers (e.g. both showing "8c") always favors currency rather than being
    // decided by fractional values the player can't see.
    //
    // Currency and the top unique each have their own bar to clear first (Minimum Value,
    // and Show Uniques Valued Above respectively - a unique only reaches here at all if it
    // already cleared its own bar). Only when BOTH clear their own bar does the head-to-head
    // value comparison decide the winner; if only one clears its bar, that one wins outright
    // regardless of raw value, and if neither does, neither is highlighted.
    private (bool CurrencyMeetsBar, bool UniqueIsBetterPick) ComparePick(float total, float? topUniqueValue)
    {
        var roundedTotal = RoundChaos(total);
        var roundedTopUnique = topUniqueValue.HasValue ? RoundChaos(topUniqueValue.Value) : (int?)null;

        var currencyClearsMinimum = roundedTotal >= Settings.MinimumValue.Value;
        var hasQualifyingUnique = roundedTopUnique.HasValue;

        bool currencyMeetsBar;
        bool uniqueIsBetterPick;

        if (currencyClearsMinimum && hasQualifyingUnique)
        {
            currencyMeetsBar = roundedTotal >= roundedTopUnique.Value;
            uniqueIsBetterPick = !currencyMeetsBar;
        }
        else
        {
            currencyMeetsBar = currencyClearsMinimum;
            uniqueIsBetterPick = hasQualifyingUnique && !currencyClearsMinimum;
        }

        return (currencyMeetsBar, uniqueIsBetterPick);
    }

    private float GetUniqueChaosValue(ItemData item)
    {
        if (item.Rarity != ItemRarity.Unique || string.IsNullOrWhiteSpace(item.Name))
            return 0f;

        var name = item.Name.Trim();
        if (_uniquePriceByName.TryGetValue(name, out var value))
            return value;

        // Unique found in-game but no exact name match in the poe.ninja price table -
        // log it once so a naming mismatch (rather than a missing/filtered listing) is
        // visible instead of the item just silently not showing a price.
        if (_loggedUnmatchedUniques.Add(name))
            DebugWindow.LogMsg($"[MercValue] No price match for unique '{name}' (BaseName='{item.BaseName}').");
        return 0f;
    }

    private void DrawTotalWindow(float total, List<(string Name, float Value)> qualifyingUniques, string mercTypeAlertMessage)
    {
        ImGui.SetNextWindowBgAlpha(BoxAlpha(Settings.TotalBoxColor));
        ImGui.Begin("##MercValueWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);

        if (!string.IsNullOrEmpty(mercTypeAlertMessage))
        {
            DrawBoldEnlargedText(mercTypeAlertMessage, ToImGuiColor(Settings.MercTypeAlertColor), 1.15f);
            ImGui.Spacing();
        }

        // You can only take one reward from the mercenary, so the currency stack and the
        // single best unique are competing options - whichever is worth more is the pick,
        // shown in green, with the other shown in red. If there's no qualifying unique to
        // compare against, fall back to the flat Minimum Value threshold for currency.
        var topUniqueValue = qualifyingUniques.Count > 0 ? qualifyingUniques[0].Value : (float?)null;
        var (currencyMeetsBar, uniqueIsBetterPick) = ComparePick(total, topUniqueValue);

        var currencyColor = ToImGuiColor(currencyMeetsBar ? Settings.MeetsMinimumColor : Settings.BelowMinimumColor);
        ImGui.TextColored(currencyColor, $"Rucksack: {RoundChaos(total)}c");

        var topUniqueColor = ToImGuiColor(uniqueIsBetterPick ? Settings.MeetsMinimumColor : Settings.BelowMinimumColor);
        var alertColor = ToImGuiColor(Settings.UniqueAlertColor);
        for (var i = 0; i < qualifyingUniques.Count; i++)
        {
            var (name, value) = qualifyingUniques[i];
            var color = i == 0 ? topUniqueColor : alertColor;
            ImGui.TextColored(color, $"[UNIQUE] {name} ({RoundChaos(value)}c)");
        }

        ImGui.End();
    }

    private static float BoxAlpha(Color color) => color.A / 255f;

    private static Vector4 ToImGuiColor(Color color) => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    // ImGui has no bold font loaded, so bold is faked by drawing the text at a couple of
    // 1px-offset positions before the final draw - a standard ImGui trick for the look
    // without needing a separate bold font asset.
    private static readonly Vector2[] BoldOffsets = [new(-1, 0), new(0, -1)];

    private static void DrawBoldEnlargedText(string text, Vector4 color, float scale)
    {
        var startPos = ImGui.GetCursorPos();

        ImGui.SetWindowFontScale(scale);
        foreach (var offset in BoldOffsets)
        {
            ImGui.SetCursorPos(startPos + offset);
            ImGui.TextColored(color, text);
        }

        ImGui.SetCursorPos(startPos);
        ImGui.TextColored(color, text);
        ImGui.SetWindowFontScale(1f);
    }

    private void QueuePriceFetch()
    {
        _ = Task.Run(FetchPricesAsync);
    }

    private void TryScheduleAutoPriceRefresh()
    {
        var minutes = Settings.AutoRefreshMinutes.Value;
        if (minutes <= 0 || _isFetchingPrices) return;
        if ((DateTime.UtcNow - _lastPriceFetchAttempt).TotalMinutes < minutes) return;

        QueuePriceFetch();
    }

    private async Task FetchPricesAsync()
    {
        if (_isFetchingPrices) return;
        _isFetchingPrices = true;
        _lastPriceFetchAttempt = DateTime.UtcNow;
        try
        {
            var league = Uri.EscapeDataString(Settings.League.Value?.Trim() ?? "");
            if (string.IsNullOrWhiteSpace(league))
            {
                SetFetchStatus(true, "Price fetch failed: league name is empty.");
                return;
            }

            var currencyPrices = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chaos Orb"] = 1f
            };
            foreach (var type in CurrencyTypes)
            {
                await MergeOverviewPricesAsync(currencyPrices, league, PoeNinjaExchangeOverviewEndpoint, type);
            }

            var uniquePrices = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in UniqueTypes)
            {
                await MergeOverviewPricesAsync(uniquePrices, league, PoeNinjaItemOverviewEndpoint, type);
            }

            // An unrecognized league name doesn't throw - poe.ninja just returns HTTP 200 with
            // empty "lines" arrays. Treat that as a failure and keep whatever prices we already
            // had, rather than silently wiping them out with empty data.
            var currencyLineCount = currencyPrices.Count - 1; // exclude the seeded Chaos Orb entry
            if (currencyLineCount <= 0 && uniquePrices.Count == 0)
            {
                SetFetchStatus(true,
                    $"Price fetch failed: poe.ninja returned no data for league '{Settings.League.Value}'. Check the league name is correct.");
                return;
            }

            _priceByName = currencyPrices;
            _uniquePriceByName = uniquePrices;
            _lastUpdatedText = DateTime.Now.ToString("HH:mm:ss");
            SetFetchStatus(false, $"Successfully fetched {currencyLineCount} currency/scarab and {uniquePrices.Count} unique prices.");
        }
        catch (Exception ex)
        {
            SetFetchStatus(true, $"Price fetch failed: {ex.Message}");
        }
        finally
        {
            _isFetchingPrices = false;
        }
    }

    private void SetFetchStatus(bool isError, string message)
    {
        _lastFetchWasError = isError;
        _lastFetchStatus = message;
        if (isError)
            DebugWindow.LogError($"[MercValue] {message}", 10);
        else
            DebugWindow.LogMsg($"[MercValue] {message} (league '{Settings.League.Value}').");
    }

    private async Task MergeOverviewPricesAsync(Dictionary<string, float> target, string league, string endpoint, string type)
    {
        var url = $"https://poe.ninja/poe1/api/{endpoint}?league={league}&type={type}";
        var json = await HttpClient.GetStringAsync(url);
        var response = JsonConvert.DeserializeObject<PoeNinjaOverviewResponse>(json);
        if (response?.Lines == null) return;

        var namesById = response.Items?
            .Where(i => !string.IsNullOrWhiteSpace(i.Id) && !string.IsNullOrWhiteSpace(i.Name))
            .ToDictionary(i => i.Id, i => i.Name, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in response.Lines)
        {
            var name = !string.IsNullOrWhiteSpace(line.Id) && namesById.TryGetValue(line.Id, out var n)
                ? n
                : line.Name ?? line.CurrencyTypeName;
            if (string.IsNullOrWhiteSpace(name)) continue;

            // Skip mutated ("Foulborn") crafting-outcome, mirrored, and Replica listings -
            // none of these can drop from a mercenary. Note: a non-empty "variant" tag alone
            // isn't a reliable signal - some uniques (Mageblood, Voidforge, etc.) always carry
            // one to distinguish their normal random implicit rolls, not corruption/mutation.
            if (name.Contains("Foulborn", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Mirrored", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Replica", StringComparison.OrdinalIgnoreCase)) continue;

            var value = line.PrimaryValue ?? line.ChaosValue ?? line.ChaosEquivalent;
            if (value is not > 0) continue;

            // When multiple variant listings share a name, keep the lowest value so
            // pricing (and the value threshold gate) is a conservative lower bound.
            if (!target.TryGetValue(name, out var existing) || value.Value < existing)
            {
                target[name] = value.Value;
            }
        }
    }

    private class PoeNinjaOverviewResponse
    {
        [JsonProperty("items")] public List<PoeNinjaOverviewItem> Items { get; set; }
        [JsonProperty("lines")] public List<PoeNinjaOverviewLine> Lines { get; set; }
    }

    private class PoeNinjaOverviewItem
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }

    private class PoeNinjaOverviewLine
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("currencyTypeName")] public string CurrencyTypeName { get; set; }
        [JsonProperty("primaryValue")] public float? PrimaryValue { get; set; }
        [JsonProperty("chaosValue")] public float? ChaosValue { get; set; }
        [JsonProperty("chaosEquivalent")] public float? ChaosEquivalent { get; set; }
        [JsonProperty("variant")] public string Variant { get; set; }
    }
}
