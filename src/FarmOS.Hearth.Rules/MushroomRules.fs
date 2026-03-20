namespace FarmOS.Hearth.Rules

open System

module MushroomRules =

    let maxColonizationDays = 30
    let optimalFruitingTempF = (55m, 75m)  // min, max for oyster mushrooms
    let optimalHumidityPct = (80m, 95m)

    type ColonizationResult = 
        | Ready 
        | NeedsMoreTime 
        | SuspectContamination of string

    let checkColonization (startDate: DateOnly) (today: DateOnly) (showsSigns: bool) =
        let days = today.DayNumber - startDate.DayNumber
        match showsSigns, days with
        | true, _ -> Ready
        | false, d when d > maxColonizationDays -> SuspectContamination $"No colonization after {d} days"
        | _ -> NeedsMoreTime

    let validateFruitingTemp (tempF: decimal) =
        let (lo, hi) = optimalFruitingTempF
        if tempF < lo then Error $"Temperature {tempF}°F below minimum {lo}°F"
        elif tempF > hi then Error $"Temperature {tempF}°F above maximum {hi}°F"
        else Ok ()

    let validateHumidity (humidityPct: decimal) =
        let (lo, hi) = optimalHumidityPct
        if humidityPct < lo then Error $"Humidity {humidityPct}%% below minimum {lo}%%"
        elif humidityPct > hi then Error $"Humidity {humidityPct}%% above maximum {hi}%%"
        else Ok ()
