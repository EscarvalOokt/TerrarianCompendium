using System;

namespace TerrarianCompendium.Angler
{
    internal readonly struct AnglerQuestSnapshot : IEquatable<AnglerQuestSnapshot>
    {
        public AnglerQuestSnapshot(
            int questItemId,
            bool finishedToday,
            int questsFinished,
            bool isHardMode,
            bool isExpertMode,
            AnglerRewardOwnership ownedRewardComponents)
        {
            if (questItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(questItemId),
                    questItemId,
                    "Quest Item ID must be greater than zero.");
            }

            if (questsFinished < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(questsFinished),
                    questsFinished,
                    "Completed quest count must not be negative.");
            }

            const AnglerRewardOwnership allKnownFlags = AnglerRewardOwnership.HighTestFishingLine |
                                                        AnglerRewardOwnership.AnglerEarring |
                                                        AnglerRewardOwnership.TackleBox |
                                                        AnglerRewardOwnership.FishermansGuide |
                                                        AnglerRewardOwnership.WeatherRadio |
                                                        AnglerRewardOwnership.Sextant |
                                                        AnglerRewardOwnership.FishingBobber;

            if ((ownedRewardComponents & ~allKnownFlags) != AnglerRewardOwnership.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ownedRewardComponents),
                    ownedRewardComponents,
                    "Owned reward components contain unsupported flags.");
            }

            QuestItemId = questItemId;
            FinishedToday = finishedToday;
            QuestsFinished = questsFinished;
            IsHardMode = isHardMode;
            IsExpertMode = isExpertMode;
            OwnedRewardComponents = ownedRewardComponents;
        }

        public int QuestItemId { get; }

        public bool FinishedToday { get; }

        public int QuestsFinished { get; }

        public bool IsHardMode { get; }

        public bool IsExpertMode { get; }

        public AnglerRewardOwnership OwnedRewardComponents { get; }

        public bool Equals(AnglerQuestSnapshot other)
        {
            return QuestItemId == other.QuestItemId &&
                   FinishedToday == other.FinishedToday &&
                   QuestsFinished == other.QuestsFinished &&
                   IsHardMode == other.IsHardMode &&
                   IsExpertMode == other.IsExpertMode &&
                   OwnedRewardComponents == other.OwnedRewardComponents;
        }

        public override bool Equals(object obj)
        {
            return obj is AnglerQuestSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = QuestItemId;
                hashCode = (hashCode * 397) ^ FinishedToday.GetHashCode();
                hashCode = (hashCode * 397) ^ QuestsFinished;
                hashCode = (hashCode * 397) ^ IsHardMode.GetHashCode();
                hashCode = (hashCode * 397) ^ IsExpertMode.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)OwnedRewardComponents;
                return hashCode;
            }
        }

        public static bool operator ==(AnglerQuestSnapshot left, AnglerQuestSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AnglerQuestSnapshot left, AnglerQuestSnapshot right)
        {
            return !left.Equals(right);
        }
    }

    internal sealed class AnglerQuestState
    {
        private bool _hasSnapshot;
        private long _revision;
        private AnglerQuestSnapshot _snapshot;

        public bool HasSnapshot => _hasSnapshot;

        public long Revision => _revision;

        public bool TryGetSnapshot(out AnglerQuestSnapshot snapshot)
        {
            snapshot = _snapshot;
            return _hasSnapshot;
        }

        public bool ReplaceSnapshot(AnglerQuestSnapshot snapshot)
        {
            if (_hasSnapshot && _snapshot == snapshot)
                return false;

            _snapshot = snapshot;
            _hasSnapshot = true;
            _revision++;
            return true;
        }
    }
}