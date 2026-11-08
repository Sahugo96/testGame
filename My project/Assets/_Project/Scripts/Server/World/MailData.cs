using System.Collections.Generic;
using Game.Server.Items;
using Game.Shared.Protocol;

namespace Game.Server.World
{
    /// <summary>
    /// A letter (05, 7.14.3 MailMessage): attachments are instances held by the server until claimed (same GUID), gold is
    /// escrowed. ExpiresAt 0 = never (system and returned letters, question 50).
    /// </summary>
    public struct MailData
    {
        public long Id;
        public MailType Type;
        /// <summary>Character name as shown; the mailbox key is its lower case.</summary>
        public string Recipient;
        /// <summary>Character name, null for the system.</summary>
        public string Sender;
        public string Subject;
        public string Body;
        public string SubjectKey;
        public string ItemDefinitionId;
        public int Number;
        public string Detail;
        public List<ItemInstance> Items;
        public long Gold;
        public long SentAt;
        public long ExpiresAt;
        public bool Read;
        public bool Claimed;
    }
}
