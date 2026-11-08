namespace Game.Shared.Protocol
{
    /// <summary>MailMessage.Type (05, 7.14.3).</summary>
    public enum MailType : byte
    {
        Player = 0,
        Sale = 1,
        Return = 2,
        System = 3,
    }

    /// <summary>
    /// A letter as the client sees it. Player letters carry Subject / Body as typed; system letters carry SubjectKey with
    /// {0} = item name (ItemDefinitionId), {1} = Number, {2} = Detail (a shop name, a level).
    /// </summary>
    public struct MailSnapshot
    {
        public long Id;
        public MailType Type;
        public string From;
        public string Subject;
        public string Body;
        public string SubjectKey;
        public string ItemDefinitionId;
        public int Number;
        public string Detail;
        public ItemSlotSnapshot[] Items;
        public long Gold;
        /// <summary>-1 = never expires (system and returned letters, question 50).</summary>
        public long SecondsLeft;
        public bool Read;
        public bool Claimed;
    }

    public sealed class MailboxMessage
    {
        public MailSnapshot[] Mail = new MailSnapshot[0];
    }

    /// <summary>Ask for the mailbox (at a mailbox).</summary>
    public struct ViewMailboxCommand : IGameCommand
    {
    }

    /// <summary>Send a letter with bag items and gold (at a mailbox; fee per attachment; RequestId = idempotency).</summary>
    public struct SendMailCommand : IGameCommand
    {
        public string Recipient;
        public string Subject;
        public string Body;
        public string[] InstanceIds;
        public long Gold;
        public string RequestId;
    }

    /// <summary>Take everything attached (all or nothing: needs room for every item).</summary>
    public struct ClaimMailCommand : IGameCommand
    {
        public long MailId;
    }

    public struct ReadMailCommand : IGameCommand
    {
        public long MailId;
    }

    /// <summary>Delete a letter (only once nothing is left attached).</summary>
    public struct DeleteMailCommand : IGameCommand
    {
        public long MailId;
    }
}
