using System;
using UnityEngine;

namespace Game.EditorTools.Content
{
    /// <summary>
    /// A content card (module 4, question 80 — decision of the developer: ScriptableObject cards in Unity). One asset per
    /// record (item, mob, skill, …) or per single file (config, world) under Assets/_Project/Content/&lt;pack&gt;/. The card
    /// holds the record's DTO itself, so the inspector edits exactly what the game reads; ContentSync writes the cards to
    /// the JSON the server, tests and tools load (and reads JSON changed outside Unity back into the cards).
    /// </summary>
    public abstract class ContentCard : ScriptableObject
    {
        /// <summary>Position of the record in its file (bag sections, levels, slots and windows keep their order).</summary>
        [HideInInspector]
        public int Order;

        public abstract object Record { get; set; }

        public abstract Type RecordType { get; }
    }

    /// <summary>Card of one DTO type; a concrete card class per type is needed because Unity serializes only non-generic assets.</summary>
    public abstract class ContentCard<T> : ContentCard where T : class, new()
    {
        public T Data = new T();

        public override object Record
        {
            get => Data;
            set => Data = (T)value;
        }

        public override Type RecordType => typeof(T);
    }
}
