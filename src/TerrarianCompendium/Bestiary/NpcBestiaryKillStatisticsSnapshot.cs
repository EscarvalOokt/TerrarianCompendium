using System;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcBestiaryKillStatisticsSnapshot
    {
        public NpcBestiaryKillStatisticsSnapshot(
            bool hasKillCounter,
            int slainCount,
            int? bannerItemId,
            int bannerKillCount,
            int killsPerBanner)
        {
            if (slainCount < 0)
                throw new ArgumentOutOfRangeException(nameof(slainCount));

            if (!hasKillCounter && slainCount != 0)
            {
                throw new ArgumentException(
                    "Slain count must be zero when the Bestiary kill counter is unavailable.",
                    nameof(slainCount));
            }

            if (bannerKillCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bannerKillCount));

            if (bannerItemId.HasValue)
            {
                if (bannerItemId.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(bannerItemId));
                if (killsPerBanner <= 0)
                    throw new ArgumentOutOfRangeException(nameof(killsPerBanner));
            }
            else if (bannerKillCount != 0 || killsPerBanner != 0)
            {
                throw new ArgumentException(
                    "Banner counters must be zero when the NPC has no banner item.",
                    nameof(bannerKillCount));
            }

            HasKillCounter = hasKillCounter;
            SlainCount = slainCount;
            BannerItemId = bannerItemId;
            BannerKillCount = bannerKillCount;
            KillsPerBanner = killsPerBanner;
        }

        public bool HasKillCounter { get; }

        public int SlainCount { get; }

        public int? BannerItemId { get; }

        public int BannerKillCount { get; }

        public int KillsPerBanner { get; }

        public bool HasBanner => BannerItemId.HasValue;
    }
}