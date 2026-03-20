module FarmOS.Hearth.Rules.ExcursionRules

open System

// ─── Types ───────────────────────────────────────────────────────────────────

type ZoneType =
    | Freezer
    | Refrigerator
    | Storage
    | FermentationRoom
    | FruitingRoom
    | Greenhouse
    | Kitchen
    | Other

type SensorType =
    | Temperature
    | Humidity
    | PH
    | CO2

type ThresholdDirection =
    | Above
    | Below

type ExcursionSeverity =
    | Warning
    | Critical

type ThresholdSpec = {
    ZoneType: ZoneType
    SensorType: SensorType
    Limit: decimal
    Direction: ThresholdDirection
    GraceMinutes: int
}

type ExcursionEvaluation = {
    IsViolation: bool
    Severity: ExcursionSeverity
    Message: string
    CorrectiveAction: string option
    GraceMinutes: int
}

// ─── FDA / GDA / USP Threshold Definitions ───────────────────────────────────

// FDA Food Code 2022: Freezer ≤ 0°F
// FDA Food Code 2022: Refrigerator ≤ 41°F
// USP <795>: Controlled room temp ≤ 77°F (25°C) for botanicals
// Standard fermentation: 65–85°F
// Fruiting room (mushrooms): ≤ 75°F, humidity ≥ 80%

let private thresholds : ThresholdSpec list = [
    { ZoneType = Freezer;          SensorType = Temperature; Limit = 0m;   Direction = Above; GraceMinutes = 15 }
    { ZoneType = Refrigerator;     SensorType = Temperature; Limit = 41m;  Direction = Above; GraceMinutes = 30 }
    { ZoneType = Storage;          SensorType = Temperature; Limit = 77m;  Direction = Above; GraceMinutes = 60 }
    { ZoneType = Storage;          SensorType = Humidity;    Limit = 65m;  Direction = Above; GraceMinutes = 120 }
    { ZoneType = FermentationRoom; SensorType = Temperature; Limit = 85m;  Direction = Above; GraceMinutes = 30 }
    { ZoneType = FermentationRoom; SensorType = Temperature; Limit = 65m;  Direction = Below; GraceMinutes = 30 }
    { ZoneType = FruitingRoom;     SensorType = Temperature; Limit = 75m;  Direction = Above; GraceMinutes = 30 }
    { ZoneType = FruitingRoom;     SensorType = Humidity;    Limit = 80m;  Direction = Below; GraceMinutes = 60 }
    { ZoneType = Kitchen;          SensorType = Temperature; Limit = 41m;  Direction = Above; GraceMinutes = 30 }
]

// ─── Evaluation ──────────────────────────────────────────────────────────────

let private isViolation (spec: ThresholdSpec) (value: decimal) : bool =
    match spec.Direction with
    | Above -> value > spec.Limit
    | Below -> value < spec.Limit

let private determineSeverity (spec: ThresholdSpec) (value: decimal) : ExcursionSeverity =
    let overshoot = abs (value - spec.Limit)
    match spec.SensorType with
    | Temperature when overshoot > 10m -> Critical
    | Humidity when overshoot > 20m    -> Critical
    | PH when overshoot > 1.0m        -> Critical
    | _                                -> Warning

let private buildMessage (spec: ThresholdSpec) (value: decimal) : string =
    let dir = match spec.Direction with Above -> "above" | Below -> "below"
    let unit = match spec.SensorType with Temperature -> "°F" | Humidity -> "%" | PH -> "" | CO2 -> "ppm"
    sprintf "%A %s reading %.1f%s is %s the %.1f%s limit for %A zone"
        spec.SensorType (dir) value unit dir spec.Limit unit spec.ZoneType

let private buildCorrectiveAction (spec: ThresholdSpec) : string option =
    match spec.ZoneType, spec.SensorType with
    | Freezer, Temperature ->
        Some "Check freezer door seal, compressor, and power supply. Move Dexter beef to backup unit if temp cannot be restored within 30 min."
    | Refrigerator, Temperature ->
        Some "Check refrigerator door seal and compressor. Verify food temps with probe thermometer. Discard if held >2h above 41°F per FDA Food Code."
    | Storage, Temperature ->
        Some "Increase ventilation or relocate herbs to climate-controlled area. Dried herbs (Passionflower, Moringa) degrade above 77°F per USP <795>."
    | Storage, Humidity ->
        Some "Check dehumidifier and ventilation. Inspect dried herbs for moisture absorption. Mycotoxin risk increases above 65% RH."
    | FermentationRoom, Temperature ->
        Some "Adjust fermentation chamber climate control. Check heater/cooler thermostat. pH progression may be affected."
    | FruitingRoom, Temperature ->
        Some "Reduce fruiting room temperature. Check exhaust fan and cooling system."
    | FruitingRoom, Humidity ->
        Some "Increase fruiting room humidity. Check humidifier water level and misting schedule."
    | Kitchen, Temperature ->
        Some "Check walk-in cooler compressor and door seal. FDA Danger Zone 41-135°F applies."
    | _ -> None

/// <summary>
/// Evaluate a sensor reading against zone-specific thresholds.
/// Returns None if no threshold applies to this zone/sensor combination.
/// </summary>
let evaluate (zoneType: ZoneType) (sensorType: SensorType) (value: decimal) : ExcursionEvaluation option =
    thresholds
    |> List.tryFind (fun t -> t.ZoneType = zoneType && t.SensorType = sensorType)
    |> Option.map (fun spec ->
        let violated = isViolation spec value
        if violated then
            {
                IsViolation = true
                Severity = determineSeverity spec value
                Message = buildMessage spec value
                CorrectiveAction = buildCorrectiveAction spec
                GraceMinutes = spec.GraceMinutes
            }
        else
            {
                IsViolation = false
                Severity = Warning
                Message = sprintf "%A reading %.1f — within safe range for %A zone" sensorType value zoneType
                CorrectiveAction = None
                GraceMinutes = spec.GraceMinutes
            }
    )

/// <summary>
/// Get the threshold spec for a given zone type and sensor type.
/// </summary>
let getThreshold (zoneType: ZoneType) (sensorType: SensorType) : ThresholdSpec option =
    thresholds |> List.tryFind (fun t -> t.ZoneType = zoneType && t.SensorType = sensorType)
