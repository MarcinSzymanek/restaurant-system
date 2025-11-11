using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using CloudKitchenChallenge.Models;

namespace CloudKitchenChallenge.KitchenSystem.Storage
{
	/// <summary>
	/// Simple storage for Hot/Cold storage types. Essentially a wrapper around a Dictionary for storing orders with limited capacity
	/// </summary>
	public class Storage : IStorage
	{
		readonly Temp temperature;
		readonly int maxStorage;
		readonly Dictionary<string, Order> map;

		public Temp Temperature
		{
			get => temperature;
		}

		public Storage(Temp temperature_, int maxStorage_)
		{
			temperature = temperature_;
			maxStorage = maxStorage_;
			map = new Dictionary<string, Order>();
		}

		public bool StoreOrder(Order order)
		{
			if (!HasSpace()) return false;
			string key = order.ID;
			return map.TryAdd(key, order);
		}

		public bool HasSpace()
		{
			return map.Count < maxStorage;
		}

		public Order? RemoveOrder(string key)
		{
			map.TryGetValue(key, out Order? order);
			if (order != null)
			{
				map.Remove(key);
			}
			return order;
		}
	}


	/// <summary>
	/// Shelf storage - uses a SortedDictionary to facilitate discarding 'potentially least fresh' elements in O(log(n)) time
	/// 
	/// </summary>
	public class ShelfStorage : IStorage, ISortedStorage
	{
		private record ShelfMapKey(
			string ID,
			int FreshnessAtPickup
		);
		/// <summary>
		/// IComparer implementation to sort by estimated freshness at pickup time
		/// </summary>
		private class SortByEstFreshnessComparer : IComparer<ShelfMapKey>
		{
			public int Compare(ShelfMapKey? first, ShelfMapKey? second)
			{
				if (first == null || second == null)
				{
					throw new ArgumentNullException("ShelfMapKey may not be null when used in a Comparer");
				}
				return first.FreshnessAtPickup.CompareTo(second.FreshnessAtPickup);
			}
		}

		public Temp Temperature
		{
			get => Temp.Room;
		}

		/// <summary>
		/// Lookup table allowing retrieving values by string ID only
		/// </summary>
		readonly Dictionary<string, ShelfMapKey> keyMap;
		/// <summary>
		/// 
		/// </summary>
		readonly SortedDictionary<ShelfMapKey, Order> map;
		readonly int maxStorage;
		const int tempTypeCount = 3;

		/// <summary>
		/// Keeps track of how many orders of different Temp are stored on the shelf
		/// Serves to avoid O(n) loop to search for an order that might not be there, when an order should be moved to a better storage
		/// </summary>
		readonly int[] orderCountPerTemp;
		public ShelfStorage(int maxStorage_)
		{
			maxStorage = maxStorage_;
			orderCountPerTemp = new int[tempTypeCount];
			map = new SortedDictionary<ShelfMapKey, Order>(new SortByEstFreshnessComparer());
			keyMap = new Dictionary<string, ShelfMapKey>();
		}
		public bool HasSpace()
		{
			return map.Count < maxStorage;
		}
		public bool ContainsOrderWithTemp(Temp temperature)
		{
			return orderCountPerTemp[GetOrderTempIdx(temperature)] > 0;
		}
		public Order? RemoveOrderWithTemp(Temp temperature)
		{
			foreach (var key in map.Keys)
			{
				if (map[key].Temperature == temperature)
				{
					Order order = map[key];
					map.Remove(key);
					orderCountPerTemp[GetOrderTempIdx(temperature)]--;
					return order;
				}
			}

			return null;
		}
		public bool StoreOrder(Order order)
		{
			if (!HasSpace() || keyMap.ContainsKey(order.ID)) return false;
			ShelfMapKey key = new ShelfMapKey(order.ID, order.ShelfFreshnessAtPickup);
			if (!keyMap.TryAdd(order.ID, key)) return false; ;
			map[key] = order;
			orderCountPerTemp[GetOrderTempIdx(order.Temperature)]++;
			return true;
		}
		public Order? RemoveOrder(string id)
		{
			keyMap.TryGetValue(id, out ShelfMapKey? key);
			if (key == null) return null;

			Order order = map[key];
			keyMap.Remove(id);
			map.Remove(key);
			orderCountPerTemp[GetOrderTempIdx(order.Temperature)]--;
			return order;
		}

		/// <summary>
		/// Remove order with least estimated freshness - first element of the sorted dictionary
		/// </summary>
		/// <returns></returns>
		public Order? PopOrder()
		{
			var keyValPair = map.First();
			keyMap.Remove(keyValPair.Key.ID);
			map.Remove(keyValPair.Key);
			orderCountPerTemp[GetOrderTempIdx(keyValPair.Value.Temperature)]--;
			return keyValPair.Value;
		}
		private int GetOrderTempIdx(Temp temp) =>
			temp switch
			{
				Temp.Room => 0,
				Temp.Hot => 1,
				Temp.Cold => 2,
				_ => throw new ArgumentException("Invalid enum value for temp", nameof(temp))
			};




	}
}
