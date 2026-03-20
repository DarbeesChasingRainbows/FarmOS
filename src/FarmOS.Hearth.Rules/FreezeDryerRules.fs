module FarmOS.Hearth.Rules.FreezeDryerRules

open System

// Reuse AlertLevel/AlertResult from IoTRules
open FarmOS.Hearth.Rules.IoTRules

// ─── Freeze-Dryer Phase Definitions ─────────────────────────────────────────

type FreezeDryerPhase =
    | Loading
    | Freezing
    | PrimaryDrying
    | SecondaryDrying

// ─── Vacuum Pressure Rules ──────────────────────────────────────────────────
// Harvest Right units target <500 mTorr during drying phases.
// 500-1000 mTorr = marginal (door seal or pump issue)
// >1000 mTorr = critical (vacuum leak — batch at risk)

let evaluateVacuum (mTorr: decimal) : AlertResult =
    if mTorr < 500M then
        { Level = Safe; Message = $"Vacuum {mTorr} mTorr — normal operating range"; CorrectiveAction = None }
    elif mTorr <= 1000M then
        { Level = Warning; Message = $"Vacuum {mTorr} mTorr — marginal. Check door seal and pump oil."; CorrectiveAction = Some "Inspect door gasket, check pump oil level, verify drain valve closed" }
    else
        { Level = Critical; Message = $"Vacuum {mTorr} mTorr — leak detected. Batch integrity at risk."; CorrectiveAction = Some "Stop cycle. Check door seal, hose connections, and pump. Do not resume until <500 mTorr achieved." }

// ─── Shelf Temperature Rules (phase-specific) ──────────────────────────────
// Freezing: shelves should reach ≤-30°F (-34°C) for pre-freeze
// Primary Drying: shelves gradually warm from -30°F to ~100°F
// Secondary Drying: shelves at 100-125°F to drive off bound moisture

let evaluateShelfTemp (phase: FreezeDryerPhase) (tempF: decimal) : AlertResult =
    match phase with
    | Loading ->
        { Level = Safe; Message = $"Shelf temp {tempF}°F — loading phase, no temperature requirement"; CorrectiveAction = None }
    | Freezing ->
        if tempF <= -20M then
            { Level = Safe; Message = $"Shelf temp {tempF}°F — adequate pre-freeze temperature"; CorrectiveAction = None }
        elif tempF <= 0M then
            { Level = Warning; Message = $"Shelf temp {tempF}°F — still cooling. Target ≤-20°F for pre-freeze."; CorrectiveAction = Some "Allow more time for shelves to reach target temperature" }
        else
            { Level = Critical; Message = $"Shelf temp {tempF}°F — too warm for freezing phase"; CorrectiveAction = Some "Verify compressor operation. Do not advance to drying until product is fully frozen." }
    | PrimaryDrying ->
        if tempF <= 125M then
            { Level = Safe; Message = $"Shelf temp {tempF}°F — within primary drying range"; CorrectiveAction = None }
        else
            { Level = Warning; Message = $"Shelf temp {tempF}°F — exceeds primary drying target (≤125°F)"; CorrectiveAction = Some "Reduce heat input to prevent case hardening" }
    | SecondaryDrying ->
        if tempF >= 80M && tempF <= 135M then
            { Level = Safe; Message = $"Shelf temp {tempF}°F — within secondary drying range"; CorrectiveAction = None }
        elif tempF < 80M then
            { Level = Warning; Message = $"Shelf temp {tempF}°F — below target for secondary drying (80-135°F)"; CorrectiveAction = Some "Increase heat to drive off residual bound moisture" }
        else
            { Level = Critical; Message = $"Shelf temp {tempF}°F — too hot, risk of product degradation"; CorrectiveAction = Some "Reduce heat immediately. Check heater control." }

// ─── Cycle Time Rules ───────────────────────────────────────────────────────
// Typical Harvest Right cycles: Freezing 6-9h, Primary 12-24h, Secondary 4-8h
// Stalled cycle detection prevents wasted product.

let evaluateCycleTime (elapsedHours: float) (phase: FreezeDryerPhase) : AlertResult =
    let (warnThreshold, critThreshold) =
        match phase with
        | Loading        -> (2.0, 4.0)
        | Freezing       -> (9.0, 14.0)
        | PrimaryDrying  -> (24.0, 36.0)
        | SecondaryDrying -> (8.0, 14.0)

    if elapsedHours <= warnThreshold then
        { Level = Safe; Message = $"Cycle time {elapsedHours:F1}h — within expected range for {phase}"; CorrectiveAction = None }
    elif elapsedHours <= critThreshold then
        { Level = Warning; Message = $"Cycle time {elapsedHours:F1}h — longer than typical for {phase}"; CorrectiveAction = Some $"Check product load size and ambient conditions" }
    else
        { Level = Critical; Message = $"Cycle time {elapsedHours:F1}h — possible stalled cycle in {phase}"; CorrectiveAction = Some $"Inspect unit immediately. Check vacuum pump, compressor, and heater operation." }

// ─── Harvest Right Screen-to-Phase Mapping ──────────────────────────────────
// Maps the 26 Harvest Right screen numbers to our domain phase model.
// Screen numbers are from the ha-harvest-right reverse engineering:
//   0 = Ready to Start, 1-3 = setup, 4 = Freezing, 5 = Primary Drying,
//   6 = Secondary Drying, 7/18 = Final Dry/Complete, 23-26 = Error states

let mapScreenToPhase (screen: int) : FreezeDryerPhase option =
    match screen with
    | 0 | 1 | 2 | 3 -> Some Loading
    | 4              -> Some Freezing
    | 5              -> Some PrimaryDrying
    | 6              -> Some SecondaryDrying
    | 7 | 18         -> None  // Complete — handled at command level, not a "drying" phase
    | _ when screen >= 23 && screen <= 26 -> None  // Error — handled at command level
    | _              -> None  // Transitional/unknown screens

/// Returns true if the screen indicates the dryer is actively running a batch
let isRunningScreen (screen: int) : bool =
    match screen with
    | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 18 -> true
    | _ -> false

/// Returns true if the screen indicates an error condition
let isErrorScreen (screen: int) : bool =
    screen >= 23 && screen <= 26

/// Returns true if the screen indicates a completed cycle
let isCompleteScreen (screen: int) : bool =
    screen = 7 || screen = 18

// ─── Combined Telemetry Evaluation ──────────────────────────────────────────
// Evaluates a full telemetry snapshot and returns the highest-severity alert.

let evaluateTelemetry (phase: FreezeDryerPhase) (tempF: decimal) (mTorr: decimal) (elapsedHrs: float) : AlertResult =
    let vacuumAlert = evaluateVacuum mTorr
    let tempAlert = evaluateShelfTemp phase tempF
    let timeAlert = evaluateCycleTime elapsedHrs phase

    let alertSeverity (a: AlertResult) =
        match a.Level with
        | Safe -> 0
        | Warning -> 1
        | Critical -> 2

    [vacuumAlert; tempAlert; timeAlert]
    |> List.maxBy alertSeverity
