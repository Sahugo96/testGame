namespace Game.Server.Persistence
{
    /// <summary>
    /// Schema migrations of saved characters (module 13; 01 7.10 principle 4). Each step upgrades a record by one version;
    /// loading always brings a record to Current, saving always writes Current. Add a step here when the record changes.
    /// </summary>
    public static class CharacterMigrations
    {
        public const int Current = 2;

        public static CharacterRecord Upgrade(CharacterRecord record)
        {
            if (record.SchemaVersion < 1)
            {
                // v0 (before statuses and position were saved): nothing to carry over.
                record.Statuses = record.Statuses ?? new StatusRecord[0];
                record.SchemaVersion = 1;
            }
            if (record.SchemaVersion < 2)
            {
                // v1 (before wallets, sockets and affixes): an empty wallet; items keep no sockets or affixes.
                record.Currencies = record.Currencies ?? new CurrencyRecord[0];
                record.SchemaVersion = 2;
            }
            return record;
        }
    }
}
