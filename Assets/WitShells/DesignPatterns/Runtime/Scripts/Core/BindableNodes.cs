using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Abstract reactive node wrapper that combines the <b>Builder</b>, <b>Observer</b>,
    /// and <b>Bindable</b> patterns into a single unit.
    /// Each node wraps a value of type <typeparamref name="T"/> inside a <see cref="Bindable{T}"/>
    /// so that listeners are automatically notified on change.
    /// When the node is no longer needed, call <see cref="Dispose"/> to broadcast its removal
    /// via <see cref="OnRemoved"/>.
    /// </summary>
    /// <typeparam name="T">The data type wrapped by this node.</typeparam>
    public abstract class NodeController<T> : Builder<T>, IEquatable<T>
    {
        private Bindable<T> _currentNode = new Bindable<T>();

        /// <summary>Fires when this node is disposed, passing the final node value to all listeners.</summary>
        public ObserverPattern<T> OnRemoved { get; } = new ObserverPattern<T>();

        /// <summary>Initialises the node with an initial value.</summary>
        /// <param name="node">The initial data value for this node.</param>
        public NodeController(T node)
        {
            _currentNode.Value = node;
        }

        ~NodeController()
        {
        }

        /// <summary>The underlying reactive value wrapped by this node.</summary>
        public Bindable<T> CurrentNode => _currentNode;

        /// <summary>
        /// Updates the node's value. Triggers <see cref="Bindable{T}.OnValueChanged"/> if the value changed.
        /// </summary>
        /// <param name="newValue">The new value to assign.</param>
        public virtual void UpdateNode(T newValue)
        {
            _currentNode.Value = newValue;
        }

        /// <inheritdoc />
        public virtual bool Equals(T other)
        {
            if (other == null) return false;
            return _currentNode.Value.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _currentNode.Value.GetHashCode();
        }

        /// <summary>Returns the current node value (satisfies the <see cref="Builder{T}"/> contract).</summary>
        public override T Build()
        {
            return _currentNode.Value;
        }

        /// <summary>
        /// Forces <see cref="Bindable{T}.OnValueChanged"/> to fire with the current value
        /// even if the value has not changed. Useful for initialisation-time binding.
        /// </summary>
        public virtual void ForceTrigger()
        {
            _currentNode.OnValueChanged.Invoke(_currentNode.Value);
        }

        /// <summary>
        /// Signals removal of this node by notifying <see cref="OnRemoved"/> observers
        /// with the current node value.
        /// </summary>
        public void Dispose()
        {
            OnRemoved.NotifyObservers(_currentNode.Value);
        }
    }

    /// <summary>
    /// Defines the contract for a collection manager that stores and queries
    /// <see cref="NodeController{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">The data type wrapped by each node.</typeparam>
    public interface INodeManager<T>
    {
        /// <summary>Adds a node to the collection. Returns <c>true</c> on success.</summary>
        public bool AddNode(NodeController<T> node);

        /// <summary>Removes a node from the collection. Returns <c>true</c> on success.</summary>
        public bool RemoveNode(NodeController<T> node);

        /// <summary>Removes all nodes whose data satisfies <paramref name="predicate"/>.</summary>
        public void RemoveWhere(Func<T, bool> predicate);

        /// <summary>Removes all nodes and disposes each one.</summary>
        public void Clear();

        /// <summary>Returns an enumerable of all nodes currently in the collection.</summary>
        public IEnumerable<NodeController<T>> GetAllNodes();

        /// <summary>Returns a new <see cref="List{T}"/> copy of all nodes.</summary>
        public List<NodeController<T>> GetAllNodesList() => new(GetAllNodes());

        /// <summary>Serialises the collection to a JSON string.</summary>
        public string ToJson();

        /// <summary>Deserialises and replaces the collection from a JSON string.</summary>
        public void FromJson(string json);

        /// <summary>Returns the first node value that satisfies <paramref name="predicate"/>, or default.</summary>
        public T GetNode(Func<T, bool> predicate);
    }

    /// <summary>
    /// A dictionary-backed <see cref="INodeManager{T}"/> that groups nodes by a computed key
    /// of type <typeparamref name="TKey"/>. Multiple nodes may share the same key.
    /// </summary>
    /// <typeparam name="T">The node data type.</typeparam>
    /// <typeparam name="TKey">The grouping key type derived from each node's data.</typeparam>
    public class UniqueNodesManager<T, TKey> : INodeManager<T>
    {
        private Dictionary<TKey, List<NodeController<T>>> _uniqueNodes = new Dictionary<TKey, List<NodeController<T>>>();
        private readonly Func<T, TKey> _keySelector;

        /// <summary>
        /// Creates a new <see cref="UniqueNodesManager{T,TKey}"/>.
        /// </summary>
        /// <param name="keySelector">A delegate that extracts the grouping key from a node's data value.</param>
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

    /// <summary>
    /// A simple list-backed <see cref="INodeManager{T}"/> that stores nodes in insertion order.
    /// Duplicate nodes (by reference) are rejected.
    /// </summary>
    /// <typeparam name="T">The node data type.</typeparam>
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