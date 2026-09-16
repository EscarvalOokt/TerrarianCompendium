using System.Runtime.Serialization;

namespace TerrarianCompendium.Persistence
{
    [DataContract]
    internal sealed class ChecklistProgressData
    {
        [DataMember(Name = "version", Order = 1)]
        public int Version { get; set; }

        [DataMember(Name = "foundItemIds", Order = 2)]
        public int[] FoundItemIds { get; set; }
    }
}