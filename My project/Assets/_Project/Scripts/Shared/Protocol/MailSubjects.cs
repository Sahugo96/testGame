namespace Game.Shared.Protocol
{
    /// <summary>Localization keys of system letters: {0} = item name, {1} = number, {2} = detail (shop name, level, character).</summary>
    public static class MailSubjects
    {
        public const string Sold = "mail.subject.sold";
        public const string Purchase = "mail.subject.purchase";
        public const string ShopClosed = "mail.subject.shop_closed";
        public const string Returned = "mail.subject.returned";
        public const string OrderSuccess = "mail.subject.order_success";
        public const string OrderFailed = "mail.subject.order_failed";
        public const string OrderBroken = "mail.subject.order_broken";
        public const string OrderReturned = "mail.subject.order_returned";
        public const string OrderPayment = "mail.subject.order_payment";
    }
}
