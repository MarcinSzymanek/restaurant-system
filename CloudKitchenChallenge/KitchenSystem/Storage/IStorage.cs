using CloudKitchenChallenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudKitchenChallenge.KitchenSystem.Storage
{
	// Common interface for all storage
	public interface IStorage
	{
		public Temp Temperature { get; }
		public bool StoreOrder(Order order);
		public bool HasSpace();
		public Order? RemoveOrder(string id);
	}
	// Shelf-specific interface
	public interface ISortedStorage
	{
		public bool ContainsOrderWithTemp(Temp temperature);
		public Order? PopOrder();
		public Order? RemoveOrderWithTemp(Temp temp);
	}
}
