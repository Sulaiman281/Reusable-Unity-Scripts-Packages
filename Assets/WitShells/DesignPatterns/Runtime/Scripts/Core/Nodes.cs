using System;
using System.Collections.Generic;

namespace WitShells.DesignPatterns.Core
{
    public abstract class DataNode : IEquatable<DataNode>
    {
        public string Identity { get; set; }
        public int Type { get; set; }
        public string Data { get; set; } // Serialized data

        public bool Equals(DataNode other)
        {
            if (other == null) return false;
            return string.Equals(Identity, other.Identity) && Type == other.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identity, Type);
        }
    }

    public class NodeController : IEquatable<DataNode>
    {
        private Bindable<DataNode> _currentNode = new Bindable<DataNode>(null);

        public NodeController(DataNode node)
        {
            _currentNode.Value = node;
        }

        public Bindable<DataNode> CurrentNode => _currentNode;

        public bool Equals(DataNode other)
        {
            if (other == null) return false;
            return _currentNode.Value.Equals(other);
        }

        public override int GetHashCode()
        {
            return _currentNode.Value.GetHashCode();
        }
    }

    public interface INodeManager
    {
        public bool AddNode(NodeController node);
        public bool RemoveNode(NodeController node);
        public IEnumerable<NodeController> GetAllNodes();
    }

    public class UniqueNodesManager : INodeManager
    {
        private HashSet<NodeController> _uniqueNodes = new HashSet<NodeController>();

        public bool AddNode(NodeController node)
        {
            return _uniqueNodes.Add(node);
        }

        public bool RemoveNode(NodeController node)
        {
            return _uniqueNodes.Remove(node);
        }

        public IEnumerable<NodeController> GetAllNodes()
        {
            return _uniqueNodes;
        }
    }

    public class NodesManager : INodeManager
    {
        private List<NodeController> _nodes = new List<NodeController>();

        public bool AddNode(NodeController node)
        {
            if (!_nodes.Contains(node))
            {
                _nodes.Add(node);
                return true;
            }
            return false;
        }

        public bool RemoveNode(NodeController node)
        {
            return _nodes.Remove(node);
        }

        public IEnumerable<NodeController> GetAllNodes()
        {
            return _nodes;
        }
    }

    public class NodeSystem
    {
        private INodeManager _nodeManager;

        public NodeSystem(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }

        public INodeManager NodeManager => _nodeManager;
    }
}