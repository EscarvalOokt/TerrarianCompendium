using System;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Filtering
{
    internal enum ChecklistNavigationFilterKind
    {
        Node,
        Other
    }

    internal readonly struct ChecklistNavigationFilter : IEquatable<ChecklistNavigationFilter>
    {
        private ChecklistNavigationFilter(ChecklistNavigationFilterKind kind, ItemNavigationNodeId nodeId)
        {
            Kind = kind;
            NodeId = nodeId;
        }

        public static ChecklistNavigationFilter AllItems =>
            new(ChecklistNavigationFilterKind.Node, ItemNavigationNodeId.AllItems);

        public ChecklistNavigationFilterKind Kind { get; }

        public ItemNavigationNodeId NodeId { get; }

        public bool IsNode => Kind == ChecklistNavigationFilterKind.Node;

        public bool IsOther => Kind == ChecklistNavigationFilterKind.Other;

        public static ChecklistNavigationFilter ForNode(ItemNavigationNodeId nodeId)
        {
            if (nodeId == ItemNavigationNodeId.None)
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "Navigation node must not be None.");

            return new ChecklistNavigationFilter(ChecklistNavigationFilterKind.Node, nodeId);
        }

        public static ChecklistNavigationFilter ForOther(ItemNavigationNodeId nodeId)
        {
            if (nodeId == ItemNavigationNodeId.None)
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "Navigation node must not be None.");

            return new ChecklistNavigationFilter(ChecklistNavigationFilterKind.Other, nodeId);
        }

        public bool Equals(ChecklistNavigationFilter other)
        {
            return Kind == other.Kind && NodeId == other.NodeId;
        }

        public override bool Equals(object obj)
        {
            return obj is ChecklistNavigationFilter other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (int)NodeId;
            }
        }

        public static bool operator ==(ChecklistNavigationFilter left, ChecklistNavigationFilter right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChecklistNavigationFilter left, ChecklistNavigationFilter right)
        {
            return !left.Equals(right);
        }
    }
}