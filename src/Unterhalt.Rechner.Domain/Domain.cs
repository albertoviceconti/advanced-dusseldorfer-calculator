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
    /// Need by age tier (based on relevant income)
    int GetNeedByAge(decimal relevantIncome, int age);
    /// Income group details for display/explanations
    IncomeGroupInfo GetIncomeGroupInfo(decimal relevantIncome, int age);
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

public sealed class DusseldorfTable2026 : IDusseldorfTable
{
    // Income bands and needs by age tier (Dusseldorf table 2026)
    private static readonly int[] Grenzen = { 2100, 2500, 2900, 3300, 3700, 4100, 4500, 4900, 5300, 5700, 6400, 7200, 8200, 9700, 11200 };
    private static readonly int[] Prozentsatz = { 100, 105, 110, 115, 120, 128, 136, 144, 152, 160, 168, 176, 184, 192, 200 };
    private static readonly int[] Bedarf0Bis5 = { 486, 511, 535, 559, 584, 623, 661, 700, 739, 778, 817, 856, 895, 934, 972 };
    private static readonly int[] Bedarf6Bis11 = { 558, 586, 614, 642, 670, 715, 759, 804, 849, 893, 938, 983, 1027, 1072, 1116 };
    private static readonly int[] Bedarf12Bis17 = { 653, 686, 719, 751, 784, 836, 889, 941, 993, 1045, 1098, 1150, 1202, 1254, 1306 };
    private static readonly int[] BedarfAb18 = { 698, 733, 768, 803, 838, 894, 950, 1006, 1061, 1117, 1173, 1229, 1285, 1341, 1396 };

    public int GetNeed18Plus(decimal combinedParentsRelevantIncome)
    {
        return GetNeedByIncome(combinedParentsRelevantIncome, BedarfAb18);
    }

    public int GetNeedStudentOwnHousehold() => 990; // 2026 flat rate
    public int GetChildBenefit() => 259;            // 2026 value
    public int GetSelfSupportRegular() => 1750;     // Self-support for adults
    public int GetSelfSupportNecessary() => 1450;   // Reduced self-support (privileged, shortage)
    public int GetNeed18PlusBySingleIncome(decimal singleRelevantIncome)
    {
        return GetNeed18Plus(singleRelevantIncome);
    }

    public int GetNeedByAge(decimal relevantIncome, int age)
    {
        if (age <= 5) return GetNeedByIncome(relevantIncome, Bedarf0Bis5);
        if (age <= 11) return GetNeedByIncome(relevantIncome, Bedarf6Bis11);
        if (age <= 17) return GetNeedByIncome(relevantIncome, Bedarf12Bis17);
        return GetNeedByIncome(relevantIncome, BedarfAb18);
    }

    public IncomeGroupInfo GetIncomeGroupInfo(decimal relevantIncome, int age)
    {
        var needs = GetNeedsByAge(age);
        var index = GetGroupIndex(relevantIncome);
        var lower = index == 0 ? 0 : Grenzen[index - 1] + 1;
        var upper = Grenzen[index];
        return new IncomeGroupInfo(
            index + 1,
            lower,
            upper,
            Prozentsatz[index],
            needs[0],
            needs[index]);
    }

    private static int GetNeedByIncome(decimal relevantIncome, int[] needs)
    {
        var index = GetGroupIndex(relevantIncome);
        return needs[index];
    }

    private static int[] GetNeedsByAge(int age)
    {
        if (age <= 5) return Bedarf0Bis5;
        if (age <= 11) return Bedarf6Bis11;
        if (age <= 17) return Bedarf12Bis17;
        return BedarfAb18;
    }

    private static int GetGroupIndex(decimal relevantIncome)
    {
        var inc = (int)Math.Round(relevantIncome, 0, MidpointRounding.AwayFromZero);
        for (int i = 0; i < Grenzen.Length; i++)
            if (inc <= Grenzen[i]) return i;
        // Above highest tier: conservatively return last table value
        return Grenzen.Length - 1;
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
    decimal RelevantIncomeUsed,
    IncomeGroupInfo? GroupInfo,
    int TableNeed,
    decimal ChildBenefitApplied,
    decimal NetNeedAfterBenefit,
    decimal ChildOwnContribution,
    decimal NetNeedAfterOwnIncome);

public sealed record IncomeGroupInfo(
    int GroupIndex,
    int LowerBound,
    int UpperBound,
    int Percent,
    int MinimumNeed,
    int Need);

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
        IncomeGroupInfo? groupInfo = null;
        decimal relevantIncome = 0m;
        var isMinor = c.Age < 18;
        var isPrivilegedUnder21 = IsPrivilegedUnder21(c);
        if (c.IsStudent && c.Residence == ResidenceStatus.OwnHousehold)
        {
            tableNeed = _table.GetNeedStudentOwnHousehold();
        }
        else
        {
            if (isMinor && c.Residence != ResidenceStatus.OwnHousehold)
            {
                // Minor -> based on bar parent's income only
                relevantIncome = GetBarParentIncome(c.Residence, incomes);
            }
            else
            {
                // Adult child in a parent's household -> table (age tier 18+), based on combined income
                relevantIncome = incomes.Father + incomes.Mother;
            }

            tableNeed = _table.GetNeedByAge(relevantIncome, c.Age);
            groupInfo = _table.GetIncomeGroupInfo(relevantIncome, c.Age);
        }

        var benefit = c.KindergeldActive
            ? (isMinor ? _table.GetChildBenefit() / 2m : _table.GetChildBenefit())
            : 0m;
        var netAfterBenefit = Math.Max(0m, tableNeed - benefit);

        var childContribution = ComputeChildOwnContribution(c);
        var netAfterOwnIncome = Math.Max(0m, netAfterBenefit - childContribution);

        return new ChildNeedResult(c, relevantIncome, groupInfo, tableNeed, benefit, netAfterBenefit, childContribution, netAfterOwnIncome);
    }

    public PaymentSplit SplitByQuote(ChildNeedResult need, ParentRelevantIncome incomes, bool privilegedUnder21AllowsLowerSB = false)
    {
        var isMinor = need.Child.Age < 18;
        var isPrivilegedUnder21 = privilegedUnder21AllowsLowerSB;

        if (isMinor && need.Child.Residence != ResidenceStatus.OwnHousehold)
        {
            int barSelfSupport = _table.GetSelfSupportNecessary();
            bool fatherIsBar = need.Child.Residence == ResidenceStatus.WithMother;
            bool motherIsBar = need.Child.Residence == ResidenceStatus.WithFather;

            var barIncome = fatherIsBar ? incomes.Father : incomes.Mother;
            var available = Math.Max(0m, barIncome - barSelfSupport);
            var pay = Math.Min(need.NetNeedAfterOwnIncome, available);

            return new PaymentSplit(fatherIsBar ? pay : 0m, motherIsBar ? pay : 0m);
        }

        // Self-support thresholds (regular for adult children)
        int selfSupport = isPrivilegedUnder21 ? _table.GetSelfSupportNecessary() : _table.GetSelfSupportRegular();

        var fatherAvail = Math.Max(0m, incomes.Father - selfSupport);
        var motherAvail = Math.Max(0m, incomes.Mother - selfSupport);

        var quote = Quote.FromAvailable(fatherAvail, motherAvail);
        var fatherShare = decimal.Round(need.NetNeedAfterOwnIncome * quote.FatherShare, 2, MidpointRounding.AwayFromZero);
        var motherShare = decimal.Round(need.NetNeedAfterOwnIncome * quote.MotherShare, 2, MidpointRounding.AwayFromZero);

        // Liability cap: each at most what they would owe alone
        var usesFlatNeed = need.Child.IsStudent && need.Child.Residence == ResidenceStatus.OwnHousehold;
        var fatherSoloNeed = usesFlatNeed ? need.TableNeed : _table.GetNeedByAge(incomes.Father, need.Child.Age);
        var motherSoloNeed = usesFlatNeed ? need.TableNeed : _table.GetNeedByAge(incomes.Mother, need.Child.Age);
        var fatherSoloPay = Math.Max(0m, fatherSoloNeed - need.ChildBenefitApplied - need.ChildOwnContribution);
        var motherSoloPay = Math.Max(0m, motherSoloNeed - need.ChildBenefitApplied - need.ChildOwnContribution);

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

    private static decimal GetBarParentIncome(ResidenceStatus residence, ParentRelevantIncome incomes)
    {
        return residence switch
        {
            ResidenceStatus.WithMother => incomes.Father,
            ResidenceStatus.WithFather => incomes.Mother,
            _ => incomes.Father + incomes.Mother
        };
    }

    private static bool IsPrivilegedUnder21(Child child)
    {
        return child.Age < 21 && child.IsInGeneralSchool && child.Residence != ResidenceStatus.OwnHousehold;
    }
}
