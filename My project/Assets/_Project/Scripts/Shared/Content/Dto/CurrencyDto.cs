using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/currencies.json.</summary>
    [Serializable]
    public class CurrenciesFileDto
    {
        public CurrencyDto[] Currencies;
    }

    /// <summary>CurrencyDefinition { Id, NameKey, Icon, Kind, Scope, Tradable, Cap } (05, 7.14.2).</summary>
    [Serializable]
    public class CurrencyDto
    {
        public string Id;
        public string NameKey;
        public string Icon;
        /// <summary>Soft, Premium or Token.</summary>
        public string Kind;
        /// <summary>Character, Account or Guild (only Character is kept in the slice).</summary>
        public string Scope;
        public bool Tradable;
        public long Cap;
    }
}
