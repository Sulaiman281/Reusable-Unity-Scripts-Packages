using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands;

namespace WitShells.DesignPatterns.Core
{
    public abstract class NodeController<T> : Builder<T>, IEquatable<T>
    {
        private Bindable<T> _currentNode = new Bindable<T>();

        public NodeController(T node)
        {
            _currentNode.Value = node;
        }

        public Bindable<T> CurrentNode => _currentNode;

        public virtual void UpdateNode(T newValue)
        {
            _currentNode.Value = newValue;
        }

        public virtual bool Equals(T other)
        {
            if (other == null) return false;
            return _currentNode.Value.Equals(other);
        }

        public override int GetHashCode()
        {
            return _currentNode.Value.GetHashCode();
        }
        public override T Build()
        {
            return _currentNode.Value;
        }

        // A Force Trigger Update to invoke the event
        public virtual void ForceTrigger()
        {
            _currentNode.OnValueChanged.Invoke(_currentNode.Value);
        }
    }

    public interface INodeManager<T>
    {
        public bool AddNode(NodeController<T> node);
        public bool RemoveNode(NodeController<T> node);
        public IEnumerable<NodeController<T>> GetAllNodes();
    }

    public class UniqueNodesManager<T, TKey> : INodeManager<T>
    {
        private Dictionary<TKey, NodeController<T>> _uniqueNodes = new Dictionary<TKey, NodeController<T>>();
        private readonly Func<T, TKey> _keySelector;

        public UniqueNodesManager(Func<T, TKey> keySelector)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        public bool AddNode(NodeController<T> node)
        {
            var key = _keySelector(node.CurrentNode.Value);
            if (!_uniqueNodes.ContainsKey(key))
            {
                _uniqueNodes[key] = node;
                return true;
            }
            return false;
        }

        public bool RemoveNode(NodeController<T> node)
        {
            var key = _keySelector(node.CurrentNode.Value);
            return _uniqueNodes.Remove(key);
        }

        public NodeController<T> GetNode(T value)
        {
            var key = _keySelector(value);
            _uniqueNodes.TryGetValue(key, out var node);
            return node;
        }

        public NodeController<T> GetNodeByKey(TKey key)
        {
            _uniqueNodes.TryGetValue(key, out var node);
            return node;
        }

        public IEnumerable<NodeController<T>> GetAllNodes()
        {
            return _uniqueNodes.Values;
        }
    }

    public class NodesManager<T> : INodeManager<T>
    {
        private List<NodeController<T>> _nodes = new List<NodeController<T>>();

        public bool AddNode(NodeController<T> node)
        {
            if (!_nodes.Contains(node))
            {
                _nodes.Add(node);
                return true;
            }
            return false;
        }

        public bool RemoveNode(NodeController<T> node)
        {
            return _nodes.Remove(node);
        }

        public NodeController<T> GetNode(T value)
        {
            return _nodes.Find(node => node.CurrentNode.Value.Equals(value));
        }

        public IEnumerable<NodeController<T>> GetAllNodes()
        {
            return _nodes;
        }
    }
}