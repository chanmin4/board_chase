using System;

/// <summary>Station-owned finite physical-money budget. Reserve before payout; rollback on failure.</summary>
[Serializable]
public sealed class MerchantFunds
{
    public int balance;
    public bool TryReserve(int amount)
    {
        if (amount < 0 || amount > balance) return false;
        balance -= amount; return true;
    }
    public void Credit(int amount) => balance = (int)Math.Min(int.MaxValue, (long)Math.Max(0, balance) + Math.Max(0, amount));
}
