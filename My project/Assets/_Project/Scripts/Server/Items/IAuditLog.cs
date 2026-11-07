namespace Game.Server.Items
{
    /// <summary>Audit log of item operations. In-memory now; a repository-backed log arrives with the database (module 13).</summary>
    public interface IAuditLog
    {
        void Write(AuditEntry entry);
    }
}
