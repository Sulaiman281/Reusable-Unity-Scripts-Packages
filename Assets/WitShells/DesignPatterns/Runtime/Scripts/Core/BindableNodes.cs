using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WitShells.DesignPatterns.Core
{
    public abstract class NodeController<T> : Builder<T>, IDisposable, IEquatable<T>
    {
        private Bindable<T> _currentNode = new Bindable<T>();
        public ObserverPattern<T> OnRemoved { get; } = new ObserverPattern<T>();

        public NodeController(T node)
        {
            _currentNode.Value = node;
        }

        ~NodeController()
        {
            Dispose();
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

        public void Dispose()
        {
            OnRemoved.NotifyObservers(_currentNode.Value);
            _currentNode = null;
        }
    }

    public interface INodeManager<T>
    {
        public bool AddNode(NodeController<T> node);
        public bool RemoveNode(NodeController<T> node);
        public IEnumerable<NodeController<T>> GetAllNodes();
        public string ToJson();
        public void FromJson(string json);
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
            node.Dispose();
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

        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(_uniqueNodes);
        }

        public virtual void FromJson(string json)
        {
            _uniqueNodes = JsonConvert.DeserializeObject<Dictionary<TKey, NodeController<T>>>(json) ?? new Dictionary<TKey, NodeController<T>>();
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
            node.Dispose();
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

        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(_nodes);
        }

        public virtual void FromJson(string json)
        {
            _nodes = JsonConvert.DeserializeObject<List<NodeController<T>>>(json) ?? new List<NodeController<T>>();
        }
    }
}