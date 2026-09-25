using System;
using Terraria;

namespace TerrarianCompendium.Shimmer
{
    [Flags]
    internal enum ShimmerProgressionRequirement
    {
        None = 0,
        PostSkeletron = 1 << 0,
        PostGolem = 1 << 1,
        PostMoonLord = 1 << 2
    }

    internal readonly struct ShimmerRuntimeContextKey(
        bool downedSkeletron,
        bool downedGolem,
        bool downedMoonLord,
        bool worldIsCrimson,
        int moonPhase) : IEquatable<ShimmerRuntimeContextKey>
    {
        public bool DownedSkeletron { get; } = downedSkeletron;

        public bool DownedGolem { get; } = downedGolem;

        public bool DownedMoonLord { get; } = downedMoonLord;

        public bool WorldIsCrimson { get; } = worldIsCrimson;

        public int MoonPhase { get; } = moonPhase;

        public bool Equals(ShimmerRuntimeContextKey other)
        {
            return DownedSkeletron == other.DownedSkeletron &&
                   DownedGolem == other.DownedGolem &&
                   DownedMoonLord == other.DownedMoonLord &&
                   WorldIsCrimson == other.WorldIsCrimson &&
                   MoonPhase == other.MoonPhase;
        }

        public override bool Equals(object obj)
        {
            return obj is ShimmerRuntimeContextKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = DownedSkeletron ? 1 : 0;
                hashCode = (hashCode * 397) ^ (DownedGolem ? 1 : 0);
                hashCode = (hashCode * 397) ^ (DownedMoonLord ? 1 : 0);
                hashCode = (hashCode * 397) ^ (WorldIsCrimson ? 1 : 0);
                hashCode = (hashCode * 397) ^ MoonPhase;
                return hashCode;
            }
        }

        public static bool operator ==(ShimmerRuntimeContextKey left, ShimmerRuntimeContextKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShimmerRuntimeContextKey left, ShimmerRuntimeContextKey right)
        {
            return !left.Equals(right);
        }
    }

    internal static class VanillaShimmerNativeBridge
    {
        public static ShimmerRuntimeContextKey GetRuntimeContextKey()
        {
            return new ShimmerRuntimeContextKey(
                NPC.downedBoss3,
                NPC.downedGolemBoss,
                NPC.downedMoonlord,
                WorldGen.crimson,
                (int)Main.GetMoonPhase());
        }

        public static bool IsProgressionLocked(ShimmerTransformationVariant variant)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            return IsProgressionLocked(variant.ProgressionRequirement, GetRuntimeContextKey());
        }

        internal static bool IsProgressionLocked(
            ShimmerProgressionRequirement requirement,
            ShimmerRuntimeContextKey context)
        {
            if ((requirement & ShimmerProgressionRequirement.PostSkeletron) != 0 && !context.DownedSkeletron)
                return true;
            if ((requirement & ShimmerProgressionRequirement.PostGolem) != 0 && !context.DownedGolem)
                return true;
            if ((requirement & ShimmerProgressionRequirement.PostMoonLord) != 0 && !context.DownedMoonLord)
                return true;

            return false;
        }
    }
}