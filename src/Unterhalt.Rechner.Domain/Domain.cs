using System;
using System.Collections.Generic;

namespace Unterhalt.Rechner.Domain;

public enum ResidenceStatus
{
    WithFather,
    WithMother,
    OwnHousehold
}

public sealed class Child
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public bool IsInGeneralSchool { get; init; } // e.g. Abiturjahr (privileged, <21)
    public bool IsStudent { get; init; }         // University studies
    public ResidenceStatus Residence { get; init; }
    public bool KindergeldActive { get; init; }  // Currently paid
    public decimal MiniJobNetMonthly { get; init; } // Net mini-job income (monthly)
}

public sealed class PropertyIncome
{
    // Rented property:
    public bool IsOwnerOccupied { get; init; }
    public decimal RentIncome { get; init; }          // Monthly rent income (0 when self-occupied)
    public decimal OperatingCosts { get; init; }      // Deductible costs (e.g. "kalte" Kosten) reducing rent surplus
    public decimal Interest { get; init; }            // Monthly loan interest
    public decimal Principal { get; init; }           // Monthly principal
    // For owner-occupied: imputed rental value
    public decimal ImputedRent { get; init; }         // Only relevant when IsOwnerOccupied = true
}

public sealed class ParentIncome
{
    // Base
    public decimal GrossMonthlyEUR { get; init; }             // Gross income (EUR)
    public decimal Taxes { get; init; }                       // Monthly taxes
    public decimal MandatorySocialSec { get; init; }          // Mandatory social security
    public decimal HealthInsurance { get; init; }             // Health insurance (full amount)
    public decimal JobExpenseRate { get; init; } = 0.05m;     // Pauschal; set to 0 when using absolute
    public decimal AdditionalPensionMonthly { get; init; }    // e.g. voluntary pillar 3a
    public decimal AdditionalPensionCapRate { get; init; } = 0.04m; // 4% of gross
    public decimal TaxRefundMonthly { get; init; }            // Regular tax refunds / 12
    public decimal OtherNetIncomes { get; init; }             // Other regular net incomes
    public List<PropertyIncome> Properties { get; init; } = new();

    // Optional absolute job expenses (used instead of the rate)
    public decimal? JobExpensesAbsolute { get; init; }
}

public interface IDusseldorfTable
{
    /// Need for adult children (age tier 4) in EUR
    int GetNeed18Plus(decimal combinedParentsRelevantIncome);
    /// Need for a student in own household (flat incl. insurance)
    int GetNeedStudentOwnHousehold();
    /// Kindergeld per child (fully creditable for adults)
    int GetChildBenefit();
    /// Self-support threshold toward adults (working)
    int GetSelfSupportRegular();
    /// Optional lower self-support (privileged under 21, shortage cases)
    int GetSelfSupportNecessary();
    /// Need for adult child when looking at single parent income (liability cap)
    int GetNeed18PlusBySingleIncome(decimal singleRelevantIncome);
}

public sealed class DusseldorfTable2025 : IDusseldorfTable
{
    // Income bands and needs (age 18+) – simplified
    private static readonly int[] Grenzen = { 2100, 2500, 2900, 3300, 3700, 4100, 4500, 4900, 5300, 5700, 6400, 7200, 8200, 9700, 11200 };
    private static readonly int[] BedarfAb18 = { 693, 728, 763, 797, 832, 888, 943, 998, 1054, 1109, 1165, 1220, 1276, 1331, 1386 };

    public int GetNeed18Plus(decimal combinedParentsRelevantIncome)
    {
        var inc = (int)Math.Round(combinedParentsRelevantIncome, 0, MidpointRounding.AwayFromZero);
        for (int i = 0; i < Grenzen.Length; i++)
            if (inc <= Grenzen[i]) return BedarfAb18[i];
        // Above highest tier: conservatively return last table value
        return BedarfAb18[^1];
    }

    public int GetNeedStudentOwnHousehold() => 990; // 2025 flat rate
    public int GetChildBenefit() => 255;            // 2025 value
    public int GetSelfSupportRegular() => 1750;     // Self-support for adults
    public int GetSelfSupportNecessary() => 1450;   // Reduced self-support (privileged, shortage)
    public int GetNeed18PlusBySingleIncome(decimal singleRelevantIncome)
    {
        var inc = (int)Math.Round(singleRelevantIncome, 0, MidpointRounding.AwayFromZero);
        for (int i = 0; i < Grenzen.Length; i++)
            if (inc <= Grenzen[i]) return BedarfAb18[i];
        return BedarfAb18[^1];
    }
}

public sealed class IncomeCalculator
{
    public decimal ComputeRelevantIncome(ParentIncome p)
    {
        // 1) Net before further deductions
        decimal netBefore = p.GrossMonthlyEUR
                            - p.Taxes
                            - p.MandatorySocialSec
                            - p.HealthInsurance;

        // 2) Job-related expenses (rate or absolute)
        decimal jobCosts = p.JobExpensesAbsolute ?? (netBefore * p.JobExpenseRate);
        netBefore -= jobCosts;

        // 3) Properties:
        //    Rented: (rent - (interest + principal)) affects income
        //    Owner-occupied: imputed rent minus (interest + principal) capped at imputed rent
        decimal propertyDelta = 0m;
        foreach (var prop in p.Properties)
        {
            if (!prop.IsOwnerOccupied)
            {
                var rentNet = prop.RentIncome - prop.OperatingCosts;
                var diff = rentNet - (prop.Interest + prop.Principal);
                propertyDelta += diff; // Surplus increases, deficit decreases
            }
            else
            {
                var costs = prop.Interest + prop.Principal;
                var advantage = prop.ImputedRent - Math.Min(costs, prop.ImputedRent);
                propertyDelta += Math.Max(0, advantage); // No negative advantage
            }
        }
        netBefore += propertyDelta;

        // 4) Regular tax refunds & other incomes
        netBefore += p.TaxRefundMonthly + p.OtherNetIncomes;

        // 5) Additional pension (e.g. voluntary) up to 4% of gross
        var cap = p.GrossMonthlyEUR * p.AdditionalPensionCapRate;
        var pensionDeduct = Math.Min(p.AdditionalPensionMonthly, cap);
        netBefore -= pensionDeduct;

        return netBefore;
    }
}

public sealed record ParentRelevantIncome(decimal Father, decimal Mother);

public sealed record Quote(decimal FatherShare, decimal MotherShare)
{
    public static Quote FromAvailable(decimal fatherAvail, decimal motherAvail)
    {
        var sum = fatherAvail + motherAvail;
        if (sum <= 0) return new Quote(0m, 0m);
        return new Quote(fatherAvail / sum, motherAvail / sum);
    }
}

public sealed record ChildNeedResult(
    Child Child,
    int TableNeed,
    int ChildBenefitApplied,
    decimal NetNeedAfterBenefit,
    decimal ChildOwnContribution,
    decimal NetNeedAfterOwnIncome);

public sealed record PaymentSplit(decimal FatherPays, decimal MotherPays);

public sealed class SupportCalculator
{
    private readonly IDusseldorfTable _table;
    private readonly IncomeCalculator _income;

    public SupportCalculator(IDusseldorfTable table, IncomeCalculator income)
    {
        _table = table;
        _income = income;
    }

    public ParentRelevantIncome ComputeParentsRelevantIncome(ParentIncome father, ParentIncome mother)
    {
        var f = _income.ComputeRelevantIncome(father);
        var m = _income.ComputeRelevantIncome(mother);
        return new ParentRelevantIncome(f, m);
    }

    public ChildNeedResult ComputeChildNeed(Child c, ParentRelevantIncome incomes)
    {
        int tableNeed;
        if (c.IsStudent && c.Residence == ResidenceStatus.OwnHousehold)
        {
            tableNeed = _table.GetNeedStudentOwnHousehold();
        }
        else
        {
            // Adult child in a parent's household -> table (age tier 18+), based on combined income
            var combined = incomes.Father + incomes.Mother;
            tableNeed = _table.GetNeed18Plus(combined);
        }

        var benefit = c.KindergeldActive ? _table.GetChildBenefit() : 0;
        var netAfterBenefit = Math.Max(0, tableNeed - benefit);

        var childContribution = ComputeChildOwnContribution(c);
        var netAfterOwnIncome = Math.Max(0, netAfterBenefit - childContribution);

        return new ChildNeedResult(c, tableNeed, benefit, netAfterBenefit, childContribution, netAfterOwnIncome);
    }

    public PaymentSplit SplitByQuote(ChildNeedResult need, ParentRelevantIncome incomes, bool privilegedUnder21AllowsLowerSB = false)
    {
        // Self-support thresholds
        int selfSupport = privilegedUnder21AllowsLowerSB ? _table.GetSelfSupportNecessary() : _table.GetSelfSupportRegular();

        var fatherAvail = Math.Max(0, incomes.Father - selfSupport);
        var motherAvail = Math.Max(0, incomes.Mother - selfSupport);

        var quote = Quote.FromAvailable(fatherAvail, motherAvail);
        var fatherShare = decimal.Round(need.NetNeedAfterOwnIncome * quote.FatherShare, 2, MidpointRounding.AwayFromZero);
        var motherShare = decimal.Round(need.NetNeedAfterOwnIncome * quote.MotherShare, 2, MidpointRounding.AwayFromZero);

        // Liability cap: each at most what they would owe alone
        var fatherSoloNeed = _table.GetNeed18PlusBySingleIncome(incomes.Father);
        var motherSoloNeed = _table.GetNeed18PlusBySingleIncome(incomes.Mother);
        var fatherSoloPay = Math.Max(0, fatherSoloNeed - need.ChildBenefitApplied - need.ChildOwnContribution);
        var motherSoloPay = Math.Max(0, motherSoloNeed - need.ChildBenefitApplied - need.ChildOwnContribution);

        fatherShare = Math.Min(fatherShare, fatherSoloPay);
        motherShare = Math.Min(motherShare, motherSoloPay);

        return new PaymentSplit(fatherShare, motherShare);
    }

    private static decimal ComputeChildOwnContribution(Child child)
    {
        if (child.MiniJobNetMonthly <= 0) return 0m;

        // For pupils/students a small allowance (100 EUR) is typically left for own needs; the rest reduces the need.
        var allowance = (child.IsInGeneralSchool || child.IsStudent) ? 100m : 0m;
        var contribution = Math.Max(0, child.MiniJobNetMonthly - allowance);

        // Do not contribute more than the calculated need after child benefit.
        return contribution;
    }
}
