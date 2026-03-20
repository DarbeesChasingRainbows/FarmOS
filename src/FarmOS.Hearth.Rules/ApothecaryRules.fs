module FarmOS.Hearth.Rules.ApothecaryRules

open System

// ─── Types ───────────────────────────────────────────────────────────────────

type HerbCategory =
    | DriedLeaf
    | DriedRoot
    | DriedFlower
    | Powder
    | Tincture

type ApothecaryReading = {
    HerbCategory: HerbCategory
    Temperature: decimal  // °F
    Humidity: decimal     // %RH
    Timestamp: DateTimeOffset
}

type RiskLevel =
    | Safe
    | Caution
    | MoldRisk
    | MycotoxinRisk
    | PotencyDegradation

type ApothecaryEvaluation = {
    TempRisk: RiskLevel
    HumidityRisk: RiskLevel
    OverallRisk: RiskLevel
    Message: string
    CorrectiveAction: string option
}

// ─── USP <795> / FDA OTC Botanical Storage Guidelines ────────────────────────
//
// Dried herbs (Passionflower, Moringa, Elderberry, etc.):
//   Temperature: ≤ 77°F (25°C) — "controlled room temperature" per USP
//   Humidity: 30–60% RH optimal for dried botanicals
//   > 60% RH: mold colonization risk begins
//   > 75% RH: mycotoxin-producing fungi (Aspergillus) become active
//   > 86°F (30°C): accelerated potency degradation of volatile compounds

let private evaluateTemperature (tempF: decimal) : RiskLevel * string =
    if tempF <= 77m then
        Safe, sprintf "Temperature %.1f°F — within USP <795> controlled room temp (≤77°F)" tempF
    elif tempF <= 86m then
        Caution, sprintf "Temperature %.1f°F — above USP limit (77°F). Monitor volatile compound stability." tempF
    else
        PotencyDegradation, sprintf "Temperature %.1f°F — exceeds 86°F. Active potency degradation of essential oils and alkaloids." tempF

let private evaluateHumidity (rh: decimal) : RiskLevel * string =
    if rh < 30m then
        Caution, sprintf "Humidity %.0f%% — below optimal range. Over-drying may cause brittleness and loss of volatile oils." rh
    elif rh <= 60m then
        Safe, sprintf "Humidity %.0f%% — within optimal range (30-60%%) for dried herb storage." rh
    elif rh <= 75m then
        MoldRisk, sprintf "Humidity %.0f%% — mold colonization risk. Aspergillus, Penicillium species activate above 60%% RH." rh
    else
        MycotoxinRisk, sprintf "Humidity %.0f%% — CRITICAL mycotoxin risk. Aflatoxin-producing Aspergillus flavus is active above 75%% RH." rh

let private worstRisk (a: RiskLevel) (b: RiskLevel) : RiskLevel =
    let order = function
        | Safe -> 0
        | Caution -> 1
        | MoldRisk -> 2
        | PotencyDegradation -> 3
        | MycotoxinRisk -> 4
    if order a >= order b then a else b

let private buildCorrectiveAction (overall: RiskLevel) : string option =
    match overall with
    | Safe -> None
    | Caution -> Some "Monitor conditions. Consider adjusting HVAC or dehumidifier settings."
    | MoldRisk -> Some "Activate dehumidifier immediately. Inspect all herb lots for visible mold. Isolate suspect containers."
    | PotencyDegradation -> Some "Move herbs to cooler storage. Potency testing recommended for affected lots (especially Passionflower, Moringa leaf)."
    | MycotoxinRisk -> Some "URGENT: Reduce humidity immediately. Quarantine all herb inventory. Test affected lots for aflatoxin B1 before release. Do NOT sell or consume until lab results confirm safety."

// ─── Public Entry Point ──────────────────────────────────────────────────────

let evaluate (reading: ApothecaryReading) : ApothecaryEvaluation =
    let tempRisk, tempMsg = evaluateTemperature reading.Temperature
    let humRisk, humMsg = evaluateHumidity reading.Humidity
    let overall = worstRisk tempRisk humRisk
    let action = buildCorrectiveAction overall
    {
        TempRisk = tempRisk
        HumidityRisk = humRisk
        OverallRisk = overall
        Message = sprintf "%s | %s" tempMsg humMsg
        CorrectiveAction = action
    }

/// <summary>
/// Quick check: is this reading compliant for storage of dried herbs?
/// Returns true if both temp and humidity are in safe ranges.
/// </summary>
let isCompliant (tempF: decimal) (humidityPct: decimal) : bool =
    tempF <= 77m && humidityPct >= 30m && humidityPct <= 60m
