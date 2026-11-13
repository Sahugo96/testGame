namespace Game.Client.UI
{
    /// <summary>What is being dragged between windows and panels (02, 7.13: one drag and drop service; module 18).</summary>
    public enum DragKind
    {
        None,
        /// <summary>A skill from the skill book (Id = skill id).</summary>
        Skill,
        /// <summary>A bag stack (Id = item definition id, Index = bag index).</summary>
        Item,
        /// <summary>A filled skill panel slot (Id = skill id, Index = slot).</summary>
        SkillSlot,
        /// <summary>A filled consumable panel slot (Id = item definition id, Index = slot).</summary>
        ConsumableSlot,
    }

    /// <summary>
    /// The payload of the current drag. uGUI calls OnDrop on the target before OnEndDrag on the source, so the source can tell
    /// from <see cref="Dropped"/> whether anyone took it (a panel slot dragged onto nothing is cleared). References only: what
    /// a drop changes is sent to the server as a command.
    /// </summary>
    public sealed class DragDropService
    {
        public DragKind Kind { get; private set; }

        public string Id { get; private set; }

        public int Index { get; private set; } = -1;

        /// <summary>A target accepted the payload.</summary>
        public bool Dropped { get; private set; }

        public void Begin(DragKind kind, string id, int index)
        {
            Kind = kind;
            Id = id;
            Index = index;
            Dropped = false;
        }

        public void MarkDropped() => Dropped = true;

        public void End()
        {
            Kind = DragKind.None;
            Id = null;
            Index = -1;
            Dropped = false;
        }
    }
}
