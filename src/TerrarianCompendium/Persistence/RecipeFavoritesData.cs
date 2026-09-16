using System.Runtime.Serialization;

namespace TerrarianCompendium.Persistence
{
    [DataContract]
    internal sealed class RecipeFavoritesData
    {
        [DataMember(Name = "version", Order = 1)]
        public int Version { get; set; }

        [DataMember(Name = "favoriteRecipeKeys", Order = 2)]
        public string[] FavoriteRecipeKeys { get; set; }
    }
}