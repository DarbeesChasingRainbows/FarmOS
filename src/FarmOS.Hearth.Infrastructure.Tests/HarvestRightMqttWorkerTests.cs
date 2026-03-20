using FluentAssertions;
using FsRules = FarmOS.Hearth.Rules.FreezeDryerRules;
using IoTRules = FarmOS.Hearth.Rules.IoTRules;

namespace FarmOS.Hearth.Infrastructure.Tests;

/// <summary>
/// Tests for the FreezeDryerRules F# module, which provides the screen-to-phase
/// mapping and telemetry evaluation logic used by <see cref="HarvestRight.HarvestRightMqttWorker"/>.
/// </summary>
public class HarvestRightMqttWorkerTests
{
    // ─── Screen-to-Phase Mapping ─────────────────────────────────────────────

    [Fact]
    public void MapScreenToPhase_Screen0_ReturnsLoading()
    {
        var result = FsRules.mapScreenToPhase(0);
        result.Should().NotBeNull();
        Microsoft.FSharp.Core.FSharpOption<FsRules.FreezeDryerPhase>.get_IsSome(result).Should().BeTrue();
        result.Value.Should().Be(FsRules.FreezeDryerPhase.Loading);
    }

    [Fact]
    public void MapScreenToPhase_Screen4_ReturnsFreezing()
    {
        var result = FsRules.mapScreenToPhase(4);
        Microsoft.FSharp.Core.FSharpOption<FsRules.FreezeDryerPhase>.get_IsSome(result).Should().BeTrue();
        result.Value.Should().Be(FsRules.FreezeDryerPhase.Freezing);
    }

    [Fact]
    public void MapScreenToPhase_Screen5_ReturnsPrimaryDrying()
    {
        var result = FsRules.mapScreenToPhase(5);
        Microsoft.FSharp.Core.FSharpOption<FsRules.FreezeDryerPhase>.get_IsSome(result).Should().BeTrue();
        result.Value.Should().Be(FsRules.FreezeDryerPhase.PrimaryDrying);
    }

    [Fact]
    public void MapScreenToPhase_Screen6_ReturnsSecondaryDrying()
    {
        var result = FsRules.mapScreenToPhase(6);
        Microsoft.FSharp.Core.FSharpOption<FsRules.FreezeDryerPhase>.get_IsSome(result).Should().BeTrue();
        result.Value.Should().Be(FsRules.FreezeDryerPhase.SecondaryDrying);
    }

    // ─── isRunningScreen ─────────────────────────────────────────────────────

    [Fact]
    public void IsRunningScreen_Screen4_ReturnsTrue()
    {
        FsRules.isRunningScreen(4).Should().BeTrue();
    }

    [Fact]
    public void IsRunningScreen_Screen0_ReturnsFalse()
    {
        FsRules.isRunningScreen(0).Should().BeFalse();
    }

    // ─── isErrorScreen ───────────────────────────────────────────────────────

    [Fact]
    public void IsErrorScreen_Screen23_ReturnsTrue()
    {
        FsRules.isErrorScreen(23).Should().BeTrue();
    }

    [Fact]
    public void IsErrorScreen_Screen5_ReturnsFalse()
    {
        FsRules.isErrorScreen(5).Should().BeFalse();
    }

    // ─── evaluateTelemetry ───────────────────────────────────────────────────

    [Fact]
    public void EvaluateTelemetry_HighVacuum_ReturnsCritical()
    {
        // 1500 mTorr is well above the 1000 mTorr critical threshold
        var result = FsRules.evaluateTelemetry(
            FsRules.FreezeDryerPhase.PrimaryDrying,
            100m,    // tempF — normal for primary drying
            1500m,   // mTorr — critical vacuum failure
            10.0);   // elapsedHrs — within normal range

        result.Level.Should().Be(IoTRules.AlertLevel.Critical);
        result.Message.Should().Contain("1500");
    }
}
