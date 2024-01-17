using System;
using System.Collections.Generic;

public class PayInfo
{
    public const int PAY_TILL_END = 0;
    public const int ALLIN = 1;
    public const int FOLDED = 2;

    public int amount;
    public int status;

    public PayInfo(int amount = 0, int status = 0)
    {
        this.amount = amount;
        this.status = status;
    }

    public void UpdateByPay(int amount)
    {
        this.amount += amount;
    }

    public void UpdateToFold()
    {
        status = FOLDED;
    }

    public void UpdateToAllIn()
    {
        status = ALLIN;
    }

    // Serialize format: [amount, status]
    public List<object> Serialize()
    {
        return new List<object> { amount, status };
    }

    public static PayInfo Deserialize(List<object> serial)
    {
        int amount = Convert.ToInt32(serial[0]);
        int status = Convert.ToInt32(serial[1]);
        return new PayInfo(amount, status);
    }
}
