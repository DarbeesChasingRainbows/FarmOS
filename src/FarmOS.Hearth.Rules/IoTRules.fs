module FarmOS.Hearth.Rules.IoTRules

open System

// ─── Types ───────────────────────────────────────────────────────────────────

type SensorType =
    | Temperature
    | PH
    | Humidity
    | CO2

type SensorReading = {
    DeviceId: string
    SensorType: SensorType
    Value: decimal
    Unit: string
    Timestamp: DateTimeOffset
}

type AlertLevel =
    | Safe
    | Warning
    | Critical

type AlertResult = {
    Level: AlertLevel
    Message: string
    CorrectiveAction: string option
}

// ─── Safe Zone Rules (FDA Food Code / HACCP) ─────────────────────────────────

// FDA Food Code 2022:
//   Refrigeration: ≤41°F (≤5°C)
//   Freezer:       ≤0°F  (≤-18°C)
//   Hot-hold:      ≥140°F (≥60°C)
//   Danger zone:   41°F–135°F
let private evaluateTemperature (value: decimal) : AlertResult =
    if value <= 41M then
        { Level = Safe; Message = $"Temperature {value}°F — refrigeration OK (≤41°F)"; CorrectiveAction = None }
    elif value >= 140M then
        { Level = Safe; Message = $"Temperature {value}°F — hot-hold OK (≥140°F)"; CorrectiveAction = None }
    elif value >= 135M then
        // Between 135–140: borderline but alert
        { Level = Warning; Message = $"Temperature {value}°F approaching danger zone minimum for hot-hold"; CorrectiveAction = Some "Increase heat source to maintain ≥140°F" }
    else
        // 41–135°F: FDA Danger Zone
        { Level = Critical; Message = $"Temperature {value}°F is in the FDA Danger Zone (41–135°F)"; CorrectiveAction = Some "Move food immediately. Discard if held >2h in danger zone. Service equipment." }

// Target pH ranges:
//   Sourdough bulk ferment: 3.5–4.5
//   Kombucha primary:       2.5–3.5
//   General food safety:    pH <4.6 inhibits C. botulinum growth
let private evaluatePH (value: decimal) : AlertResult =
    if value < 2.5M then
        { Level = Critical; Message = $"pH {value} is dangerously acidic — possible culture die-off or calibration error"; CorrectiveAction = Some "Discard batch, deep-clean vessel, recalibrate probe" }
    elif value > 4.6M then
        { Level = Warning; Message = $"pH {value} above fermentation target (>4.6) — C. botulinum risk zone"; CorrectiveAction = Some "Monitor closely. If sourdough: check starter health. If kombucha: check SCOBY." }
    else
        { Level = Safe; Message = $"pH {value} within safe fermentation range"; CorrectiveAction = None }

// OSHA / building codes: CO2 >1000ppm = poor ventilation; >5000ppm = IDLH
let private evaluateCO2 (value: decimal) : AlertResult =
    if value > 5000M then
        { Level = Critical; Message = $"CO2 {value}ppm exceeds OSHA IDLH threshold (5000ppm)"; CorrectiveAction = Some "Evacuate and ventilate immediately" }
    elif value > 1000M then
        { Level = Warning; Message = $"CO2 {value}ppm — poor ventilation (ASHRAE recommends <1000ppm)"; CorrectiveAction = Some "Increase ventilation. Check HVAC." }
    else
        { Level = Safe; Message = $"CO2 {value}ppm — ventilation OK"; CorrectiveAction = None }

// Relative humidity: typical fermentation room 65–80%
let private evaluateHumidity (value: decimal) : AlertResult =
    if value > 90M then
        { Level = Warning; Message = $"Humidity {value} %% — risk of mold and condensation"; CorrectiveAction = Some "Reduce humidity. Check dehumidifier." }
    elif value < 40M then
        { Level = Warning; Message = $"Humidity {value} %% — too dry for active fermentation cultures"; CorrectiveAction = Some "Increase humidity or cover cultures" }
    else
        { Level = Safe; Message = $"Humidity {value} %% — within acceptable range"; CorrectiveAction = None }

// ─── Public Entry Point ───────────────────────────────────────────────────────

let evaluate (reading: SensorReading) : AlertResult =
    match reading.SensorType with
    | Temperature -> evaluateTemperature reading.Value
    | PH          -> evaluatePH reading.Value
    | CO2         -> evaluateCO2 reading.Value
    | Humidity    -> evaluateHumidity reading.Value
