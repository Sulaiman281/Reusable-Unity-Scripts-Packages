using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WitShells.DesignPatterns.Core
{
    public abstract class NodeController<T> : Builder<T>, IEquatable<T>
    {
        private Bindable<T> _currentNode = new Bindable<T>();
        public ObserverPattern<T> OnRemoved { get; } = new ObserverPattern<T>();

        public NodeController(T node)
        {
            _currentNode.Value = node;
        }

        ~NodeController()
        {
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
        }
    }

    public interface INodeManager<T>
    {
        public bool AddNode(NodeController<T> node);
        public bool RemoveNode(NodeController<T> node);
        public void RemoveWhere(Func<T, bool> predicate);
        public void Clear();
        public IEnumerable<NodeController<T>> GetAllNodes();
        public List<NodeController<T>> GetAllNodesList() => new(GetAllNodes());
        public string ToJson();
        public void FromJson(string json);
        public T GetNode(Func<T, bool> predicate);
    }

    public class UniqueNodesManager<T, TKey> : INodeManager<T>
    {
        private Dictionary<TKey, List<NodeController<T>>> _uniqueNodes = new Dictionary<TKey, List<NodeController<T>>>();
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
                _uniqueNodes[key] = new List<NodeController<T>> { node };
                return true;
            }
            _uniqueNodes[key].Add(node);
            return true;
        }

        public bool RemoveNode(NodeController<T> node)
        {
            var key = _keySelector(node.CurrentNode.Value);
            if (_uniqueNodes.TryGetValue(key, out var nodeList))
            {
                bool removed = nodeList.Remove(node);
                if (nodeList.Count == 0)
                {
                    _uniqueNodes.Remove(key);
                }
                if (removed)
                {
                    node.Dispose();
                }
                return removed;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var nodeList in _uniqueNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    node.Dispose();
                }
            }
            _uniqueNodes.Clear();
        }

        public IEnumerable<NodeController<T>> GetAllNodes()
        {
            foreach (var nodeList in _uniqueNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    yield return node;
                }
            }
        }

        public virtual T GetNode(Func<T, bool> predicate)
        {
            foreach (var nodeList in _uniqueNodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (predicate(node.CurrentNode.Value))
                    {
                        return node.CurrentNode.Value;
                    }
                }
            }
            return default;
        }

        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(_uniqueNodes);
        }

        public virtual void FromJson(string json)
        {
            _uniqueNodes = JsonConvert.DeserializeObject<Dictionary<TKey, List<NodeController<T>>>>(json) ?? new Dictionary<TKey, List<NodeController<T>>>();
        }

        public void RemoveWhere(Func<T, bool> predicate)
        {
            var keysToRemove = new List<TKey>();
            foreach (var kvp in _uniqueNodes)
            {
                kvp.Value.RemoveAll(node =>
                {
                    if (predicate(node.CurrentNode.Value))
                    {
                        node.Dispose();
                        return true;
                    }
                    return false;
                });
                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _uniqueNodes.Remove(key);
            }
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

        public void Clear()
        {
            foreach (var node in _nodes)
            {
                node.Dispose();
            }
            _nodes.Clear();
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

        public virtual T GetNode(Func<T, bool> predicate)
        {
            var node = _nodes.Find(n => predicate(n.CurrentNode.Value));
            return node != null ? node.CurrentNode.Value : default;
        }

        public void RemoveWhere(Func<T, bool> predicate)
        {
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (predicate(_nodes[i].CurrentNode.Value))
                {
                    _nodes[i].Dispose();
                    _nodes.RemoveAt(i);
                }
            }
        }
    }
}