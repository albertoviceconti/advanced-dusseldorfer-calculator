using System.Collections.Generic;
using System.Linq;
using Unterhalt.Rechner.Domain;

namespace Unterhalt.Rechner.Web.Services;

public sealed class CalculationState
{
    public ParentIncomeInput Father { get; } = ParentIncomeInput.CreateDefault();
    public ParentIncomeInput Mother { get; } = ParentIncomeInput.CreateDefault();
    public List<ChildInput> Children { get; } = new() { ChildInput.CreateDefault("Kind 1") };

    public void EnsureAtLeastOneChild()
    {
        if (Children.Count == 0)
        {
            Children.Add(ChildInput.CreateDefault($"Kind {Children.Count + 1}"));
        }
    }

    public static ParentIncomeInput CloneParent(ParentIncomeInput source)
    {
        return new ParentIncomeInput
        {
            Mode = source.Mode,
            NetIncome = CloneMoney(source.NetIncome),
            Gross = CloneMoney(source.Gross),
            Taxes = CloneMoney(source.Taxes),
            MandatorySocialSec = CloneMoney(source.MandatorySocialSec),
            HealthInsurance = CloneMoney(source.HealthInsurance),
            JobExpenseRate = source.JobExpenseRate,
            AdditionalPension = CloneMoney(source.AdditionalPension),
            AdditionalPensionCapRate = source.AdditionalPensionCapRate,
            TaxRefund = CloneMoney(source.TaxRefund),
            OtherNetIncomes = CloneMoney(source.OtherNetIncomes),
            JobExpensesAbsolute = source.JobExpensesAbsolute != null ? CloneMoney(source.JobExpensesAbsolute) : null,
            Properties = source.Properties.Select(CloneProperty).ToList()
        };
    }

    public static ChildInput CloneChild(ChildInput source)
    {
        return new ChildInput
        {
            Name = source.Name,
            Age = source.Age,
            IsInGeneralSchool = source.IsInGeneralSchool,
            IsStudent = source.IsStudent,
            Residence = source.Residence,
            KindergeldActive = source.KindergeldActive,
            HasMiniJob = source.HasMiniJob,
            MiniJobIncome = CloneMoney(source.MiniJobIncome)
        };
    }

    public void LoadFromSnapshot(CalculationSnapshot snapshot)
    {
        CopyParent(Father, snapshot.Father);
        CopyParent(Mother, snapshot.Mother);

        Children.Clear();
        if (snapshot.Children != null && snapshot.Children.Any())
        {
            Children.AddRange(snapshot.Children.Select(CloneChild));
        }
        EnsureAtLeastOneChild();
    }

    private static void CopyParent(ParentIncomeInput target, ParentIncomeInput source)
    {
        target.Mode = source.Mode;
        CopyMoney(target.NetIncome, source.NetIncome);
        CopyMoney(target.Gross, source.Gross);
        CopyMoney(target.Taxes, source.Taxes);
        CopyMoney(target.MandatorySocialSec, source.MandatorySocialSec);
        CopyMoney(target.HealthInsurance, source.HealthInsurance);
        target.JobExpenseRate = source.JobExpenseRate;
        CopyMoney(target.AdditionalPension, source.AdditionalPension);
        target.AdditionalPensionCapRate = source.AdditionalPensionCapRate;
        CopyMoney(target.TaxRefund, source.TaxRefund);
        CopyMoney(target.OtherNetIncomes, source.OtherNetIncomes);
        target.JobExpensesAbsolute = source.JobExpensesAbsolute != null ? CloneMoney(source.JobExpensesAbsolute) : null;

        target.Properties = source.Properties.Select(CloneProperty).ToList();
    }

    private static PropertyIncomeInput CloneProperty(PropertyIncomeInput source)
    {
        return new PropertyIncomeInput
        {
            Label = source.Label,
            IsOwnerOccupied = source.IsOwnerOccupied,
            RentIncome = CloneMoney(source.RentIncome),
            OperatingCosts = CloneMoney(source.OperatingCosts),
            Interest = CloneMoney(source.Interest),
            Principal = CloneMoney(source.Principal),
            ImputedRent = CloneMoney(source.ImputedRent)
        };
    }

    private static MoneyInput CloneMoney(MoneyInput source) => new() { Amount = source.Amount, Period = source.Period };
    private static void CopyMoney(MoneyInput target, MoneyInput source)
    {
        target.Amount = source.Amount;
        target.Period = source.Period;
    }
}

public sealed class ParentIncomeInput
{
    public IncomeEntryMode Mode { get; set; } = IncomeEntryMode.Detailed;
    public MoneyInput NetIncome { get; set; } = MoneyInput.Monthly();
    public MoneyInput Gross { get; set; } = MoneyInput.Monthly();
    public MoneyInput Taxes { get; set; } = MoneyInput.Monthly();
    public MoneyInput MandatorySocialSec { get; set; } = MoneyInput.Monthly();
    public MoneyInput HealthInsurance { get; set; } = MoneyInput.Monthly();
    public decimal JobExpenseRate { get; set; } = 0.05m;
    public MoneyInput AdditionalPension { get; set; } = MoneyInput.Monthly();
    public decimal AdditionalPensionCapRate { get; set; } = 0.04m;
    public MoneyInput TaxRefund { get; set; } = MoneyInput.Monthly();
    public MoneyInput OtherNetIncomes { get; set; } = MoneyInput.Monthly();
    public MoneyInput? JobExpensesAbsolute { get; set; }
    public List<PropertyIncomeInput> Properties { get; set; } = new();

    public ParentIncome ToDomain()
    {
        if (Mode == IncomeEntryMode.Net)
        {
            var net = NetIncome.ToMonthly();
            return new ParentIncome
            {
                GrossMonthlyEUR = net, // treated as already net
                Taxes = 0,
                MandatorySocialSec = 0,
                HealthInsurance = 0,
                JobExpenseRate = 0,
                AdditionalPensionMonthly = 0,
                AdditionalPensionCapRate = 0,
                TaxRefundMonthly = 0,
                OtherNetIncomes = 0,
                JobExpensesAbsolute = 0,
                Properties = new()
            };
        }

        return new ParentIncome
        {
            GrossMonthlyEUR = Gross.ToMonthly(),
            Taxes = Taxes.ToMonthly(),
            MandatorySocialSec = MandatorySocialSec.ToMonthly(),
            HealthInsurance = HealthInsurance.ToMonthly(),
            JobExpenseRate = JobExpenseRate,
            AdditionalPensionMonthly = AdditionalPension.ToMonthly(),
            AdditionalPensionCapRate = AdditionalPensionCapRate,
            TaxRefundMonthly = TaxRefund.ToMonthly(),
            OtherNetIncomes = OtherNetIncomes.ToMonthly(),
            JobExpensesAbsolute = JobExpensesAbsolute?.ToMonthly(),
            Properties = Properties.Select(p => p.ToDomain()).ToList()
        };
    }

    public static ParentIncomeInput CreateDefault() => new()
    {
        Mode = IncomeEntryMode.Detailed,
        NetIncome = MoneyInput.Monthly(),
        JobExpenseRate = 0.05m,
        AdditionalPensionCapRate = 0.04m,
        Properties = new()
    };
}

public enum Period
{
    Monthly,
    Yearly
}

public enum IncomeEntryMode
{
    Detailed,
    Net
}

public sealed class MoneyInput
{
    public decimal Amount { get; set; }
    public Period Period { get; set; } = Period.Monthly;

    public decimal ToMonthly() => Period == Period.Monthly ? Amount : Amount / 12m;

    public static MoneyInput Monthly(decimal amount = 0) => new() { Amount = amount, Period = Period.Monthly };
    public static MoneyInput Yearly(decimal amount = 0) => new() { Amount = amount, Period = Period.Yearly };
}

public sealed class PropertyIncomeInput
{
    public string Label { get; set; } = "Objekt";
    public bool IsOwnerOccupied { get; set; }
    public MoneyInput RentIncome { get; set; } = MoneyInput.Monthly();
    public MoneyInput OperatingCosts { get; set; } = MoneyInput.Monthly();
    public MoneyInput Interest { get; set; } = MoneyInput.Monthly();
    public MoneyInput Principal { get; set; } = MoneyInput.Monthly();
    public MoneyInput ImputedRent { get; set; } = MoneyInput.Monthly();

    public PropertyIncome ToDomain() => new()
    {
        IsOwnerOccupied = IsOwnerOccupied,
        RentIncome = RentIncome.ToMonthly(),
        OperatingCosts = OperatingCosts.ToMonthly(),
        Interest = Interest.ToMonthly(),
        Principal = Principal.ToMonthly(),
        ImputedRent = ImputedRent.ToMonthly()
    };

    public static PropertyIncomeInput CreateDefault(int index) => new()
    {
        Label = $"Objekt {index}"
    };
}

public sealed class ChildInput
{
    public string Name { get; set; } = "Kind";
    public int Age { get; set; } = 18;
    public bool IsInGeneralSchool { get; set; }
    public bool IsStudent { get; set; }
    public ResidenceStatus Residence { get; set; } = ResidenceStatus.WithMother;
    public bool KindergeldActive { get; set; } = true;
    public bool HasMiniJob { get; set; }
    public MoneyInput MiniJobIncome { get; set; } = MoneyInput.Monthly();

    public bool PrivilegedUnder21 => Age < 21 && IsInGeneralSchool && Residence != ResidenceStatus.OwnHousehold;

    public Child ToDomain() => new()
    {
        Name = Name,
        Age = Age,
        IsInGeneralSchool = IsInGeneralSchool,
        IsStudent = IsStudent,
        Residence = Residence,
        KindergeldActive = KindergeldActive,
        MiniJobNetMonthly = HasMiniJob ? MiniJobIncome.ToMonthly() : 0m
    };

    public static ChildInput CreateDefault(string name) => new()
    {
        Name = name,
        Age = 18,
        KindergeldActive = true,
        Residence = ResidenceStatus.WithMother
    };
}

public sealed class SnapshotChildResult
{
    public ChildInput Child { get; set; } = ChildInput.CreateDefault("Kind");
    public ChildNeedResult? Need { get; set; }
    public PaymentSplit? Split { get; set; }
}

public sealed class CalculationSnapshot
{
    public ParentIncomeInput Father { get; set; } = ParentIncomeInput.CreateDefault();
    public ParentIncomeInput Mother { get; set; } = ParentIncomeInput.CreateDefault();
    public List<ChildInput> Children { get; set; } = new();
    public ParentRelevantIncome? Incomes { get; set; }
    public List<SnapshotChildResult> Results { get; set; } = new();
}
