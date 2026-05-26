namespace Cma.Services.Payment.Bambora;

public static class BamboraConstants
{
    public static class Approval
    {
        public const string Approved = "1";
        public const string Declined = "0";
    }

    public static class CardType
    {
        public const string AmericanExpress = "AM";
        public const string MasterCard = "MC";
        public const string Visa = "VI";
        public const string VisaDebit = "PV";
        public const string MasterCardDebit = "MD";
    }
}