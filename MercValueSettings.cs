using System.Collections.Generic;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using SharpDX;

namespace MercValue;

public class MercValueSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("League", "League name used for poe.ninja currency/scarab/unique price lookups. Must match your current league exactly, e.g. Allflame")]
    public TextNode League { get; set; } = new TextNode("Allflame");

    [Menu("Auto Refresh (min)", "Automatically re-fetch currency/scarab/unique prices from poe.ninja at this interval. Set to 0 to only refresh manually.")]
    public RangeNode<int> AutoRefreshMinutes { get; set; } = new RangeNode<int>(30, 0, 180);

    [Menu("Refresh Prices Now", "Immediately re-fetch currency/scarab/unique prices from poe.ninja for the configured league.")]
    public ButtonNode RefreshPricesNow { get; set; } = new ButtonNode();

    [Menu("Price Status", "Shows whether the last currency/scarab/unique price fetch from poe.ninja succeeded or failed, and when prices were last successfully updated.")]
    [JsonIgnore]
    public CustomNode PriceStatusPanel { get; set; } = new CustomNode();

    [Menu("Minimum Value (c)", "If the total chaos value is below this, the total text is drawn in the Below Minimum color; at or above it, the Meets Minimum color.")]
    public RangeNode<int> MinimumValue { get; set; } = new RangeNode<int>(10, 1, 100);

    [Menu("Below Minimum Color", "Text color used when the total is below the minimum value.")]
    public ColorNode BelowMinimumColor { get; set; } = new ColorNode(Color.Red);

    [Menu("Meets Minimum Color", "Text color used when the total is at or above the minimum value.")]
    public ColorNode MeetsMinimumColor { get; set; } = new ColorNode(Color.Green);

    [Menu("Total Box Color", "Background box color behind the total-value text.")]
    public ColorNode TotalBoxColor { get; set; } = new ColorNode(new Color(0, 0, 0, 150));

    [Menu("Show Uniques Valued Above (c)", "Unique items on the mercenary priced at or above this chaos value are flagged in the overlay. Priced using poe.ninja unique weapon/armour/accessory data.")]
    public RangeNode<int> ShowUniquesValueAbove { get; set; } = new RangeNode<int>(10, 1, 100);

    [Menu("Unique Alert Color", "Text color used to flag a unique item found on the mercenary that meets the value threshold above.")]
    public ColorNode UniqueAlertColor { get; set; } = new ColorNode(new Color(175, 96, 37));

    [Menu("Show All Qualifying Uniques", "When on, every unique at or above the value threshold is shown. When off, only the top N highest-priced qualifying uniques are shown (see below) - useful since you can only take one item from the mercenary.")]
    public ToggleNode ShowAllQualifyingUniques { get; set; } = new ToggleNode(true);

    [Menu("Top Uniques To Show", "How many of the uniques matching the Show Uniques Valued Above threshold to display.")]
    [ConditionalDisplay(nameof(ShowingAllQualifyingUniques), false)]
    public RangeNode<int> TopUniquesToShow { get; set; } = new RangeNode<int>(1, 1, 10);

    public bool ShowingAllQualifyingUniques() => ShowAllQualifyingUniques.Value;

    [Menu("Unique Items Selected To Display", "Live preview of every poe.ninja-priced unique that currently meets the value threshold above - these are the uniques that would be flagged if found on a mercenary.")]
    [JsonIgnore]
    public CustomNode QualifyingUniquesPanel { get; set; } = new CustomNode();

    [Menu("Merc Type Alert Color", "Text color used for the merc-type alert message shown when a watched mercenary class is detected.")]
    public ColorNode MercTypeAlertColor { get; set; } = new ColorNode(new Color(255, 180, 60));

    public List<MercTypeAlertEntry> MercTypeAlerts { get; set; } = new List<MercTypeAlertEntry>();

    [Menu("Merc Type Alerts", "Pick a mercenary class from the dropdown, add it, then customize its message. That message is shown in the overlay whenever a mercenary of that class is encountered.")]
    [JsonIgnore]
    public CustomNode MercTypeAlertsPanel { get; set; } = new CustomNode();
}

public class MercTypeAlertEntry
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
