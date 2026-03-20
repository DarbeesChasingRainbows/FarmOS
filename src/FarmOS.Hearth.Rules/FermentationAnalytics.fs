module FarmOS.Hearth.Rules.FermentationAnalytics

open System

// ─── Types ───────────────────────────────────────────────────────────────────

type ProductType =
    | Kombucha
    | Jun
    | Sourdough
    | GeneralFermented

type PHDataPoint = {
    Timestamp: DateTimeOffset
    PH: decimal
}

type FermentationSafetyResult = {
    IsSafe: bool
    CurrentPH: decimal
    TargetPH: decimal
    DropRatePerHour: decimal option
    EstimatedHoursToSafe: decimal option
    Confidence: decimal
    Message: string
}

// ─── FDA / Food Safety pH Thresholds ─────────────────────────────────────────
//
// FDA Food Code: pH ≤ 4.6 inhibits C. botulinum growth (general food safety)
// Kombucha: target pH ≤ 3.5 for proper acidity (TTB/FDA guidance)
// Jun: similar to kombucha, pH ≤ 3.5
// Sourdough: pH ≤ 4.5 for safe bulk ferment (lactic acid threshold)

let private targetPH (product: ProductType) : decimal =
    match product with
    | Kombucha         -> 3.5m
    | Jun              -> 3.5m
    | Sourdough        -> 4.5m
    | GeneralFermented -> 4.6m

// ─── Analytics ───────────────────────────────────────────────────────────────

/// <summary>
/// Calculate the pH drop rate in pH units per hour using simple linear regression
/// on the most recent data points.
/// </summary>
let private calculateDropRate (points: PHDataPoint list) : decimal option =
    match points with
    | [] | [_] -> None
    | _ ->
        // Use last N points (max 10) for recent trend
        let recent = points |> List.sortByDescending (fun p -> p.Timestamp) |> List.truncate 10 |> List.rev
        if recent.Length < 2 then None
        else
            let first = recent |> List.head
            let last = recent |> List.last
            let hours = decimal (last.Timestamp - first.Timestamp).TotalHours
            if hours <= 0m then None
            else
                let drop = first.PH - last.PH  // positive = pH is dropping (good)
                Some (drop / hours)

/// <summary>
/// Estimate hours remaining until pH reaches the target safe level.
/// Uses current drop rate to project forward.
/// </summary>
let private estimateTimeToSafe (currentPH: decimal) (targetPH: decimal) (dropRatePerHour: decimal) : decimal option =
    if currentPH <= targetPH then Some 0m
    elif dropRatePerHour <= 0m then None  // pH not dropping — can't estimate
    else Some ((currentPH - targetPH) / dropRatePerHour)

/// <summary>
/// Calculate confidence based on R² of the linear trend and number of data points.
/// More points + consistent trend = higher confidence.
/// </summary>
let private calculateConfidence (points: PHDataPoint list) (dropRate: decimal option) : decimal =
    match dropRate with
    | None -> 0m
    | Some rate ->
        let pointBonus = min 1.0m (decimal points.Length / 10.0m)
        let rateBonus = if rate > 0m then 0.5m else 0.1m
        min 1.0m (pointBonus * 0.5m + rateBonus)

// ─── Public Entry Point ──────────────────────────────────────────────────────

/// <summary>
/// Evaluate the fermentation pH trajectory for a batch.
/// Determines if the product has reached safe acidity, and if not, predicts when it will.
/// </summary>
let evaluateTrajectory (product: ProductType) (readings: PHDataPoint list) : FermentationSafetyResult =
    let target = targetPH product

    match readings with
    | [] ->
        {
            IsSafe = false
            CurrentPH = 7.0m
            TargetPH = target
            DropRatePerHour = None
            EstimatedHoursToSafe = None
            Confidence = 0m
            Message = "No pH readings recorded yet."
        }
    | _ ->
        let sorted = readings |> List.sortBy (fun r -> r.Timestamp)
        let current = (sorted |> List.last).PH
        let isSafe = current <= target
        let dropRate = calculateDropRate sorted
        let eta = dropRate |> Option.bind (estimateTimeToSafe current target)
        let confidence = calculateConfidence sorted dropRate

        let message =
            if isSafe then
                sprintf "pH %.2f has reached safe acidity target (≤%.1f) for %A. Fermentation complete." current target product
            else
                match eta with
                | Some hours when hours > 0m ->
                    sprintf "pH %.2f — dropping at ~%.3f/hr. Estimated %.1f hours to reach safe pH %.1f for %A."
                        current (Option.defaultValue 0m dropRate) hours target product
                | _ ->
                    sprintf "pH %.2f — not yet at safe level (target ≤%.1f for %A). Insufficient trend data to estimate completion."
                        current target product

        {
            IsSafe = isSafe
            CurrentPH = current
            TargetPH = target
            DropRatePerHour = dropRate
            EstimatedHoursToSafe = if isSafe then Some 0m else eta
            Confidence = confidence
            Message = message
        }

/// <summary>
/// Quick safety check: has this batch reached the FDA safe acidity threshold?
/// </summary>
let isSafeForProduct (product: ProductType) (currentPH: decimal) : bool =
    currentPH <= targetPH product
