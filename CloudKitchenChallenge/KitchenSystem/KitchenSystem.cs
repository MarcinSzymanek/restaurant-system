using CloudKitchenChallenge.KitchenSystem.Actions;
using CloudKitchenChallenge.KitchenSystem.Storage;
using CloudKitchenChallenge.Logging;
using CloudKitchenChallenge.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudKitchenChallenge.KitchenSystem
{
	/// <summary>
	/// Main controller class facilitating thread-safe transactions
	/// Public methods are the interface (Place and Remove orders) of the system
	/// </summary>
	public class Kitchen
	{
		readonly StorageController storageController;
		// Lookup to quickly find the appropriate storage of existing orders
		readonly Dictionary<string, Temp> orderLookup;
		// Used to lock transactions for thread safety
		readonly Lock transactionLock;
		readonly IActionLogger logger;
		public Kitchen(IActionLogger logger_)
		{
			logger = logger_;
			orderLookup = new Dictionary<string, Temp>();
			transactionLock = new Lock();
			storageController = new StorageController();
		}
		/// <summary>
		/// Place an order in the storage. If there is no space in storage with required Temp, try to store it in a shelf
		/// If there is no space on the shelf, try to move - or discard an item from the shelf.
		/// </summary>
		/// <param name="orderDetails">Record class with same structure as Json order</param>
		/// <param name="pickupDelayMs">Estimated time until pickup</param>
		public void PlaceOrder(OrderDetails orderDetails, int pickupDelayMs)
		{
			Order order = new Order(orderDetails, pickupDelayMs);
			Temp desiredTemp = order.Temperature;
			IStorage storage = storageController.GetStorage(desiredTemp);
			lock (transactionLock)
			{
				if(orderLookup.ContainsKey(orderDetails.ID))
				{
					return;
				}
				if (TryStoreOrder(storage, order))
				{
					return;
				}

				if(desiredTemp != Temp.Room)
				{
					if(TryStoreOrder(storageController.GetShelf(), order))
					{
						return;
					}
				}

				Temp? ignoredTemp = (desiredTemp != Temp.Room) ? desiredTemp : null;
				bool placeResult = false;
				{
					ShelfStorage shelf = storageController.GetShelf();
					bool moveResult = TryMoveFromShelf(ignoredTemp, shelf);
					if (!moveResult) 
					{
						PopOrderFromShelf(shelf);
					}
					placeResult = TryStoreOrder(shelf, order);
				}
			}

		}

		private bool TryStoreOrder(IStorage storage, Order order)
		{
			bool result = false;
			result = storage.StoreOrder(order);

			if (result)
			{
				orderLookup.Add(order.ID, storage.Temperature);
			}

			if(result)
			{
				LogAction<PlaceAction>(order.ID, storage.Temperature);
			}
			return result;
		}

		private bool TryMoveFromShelf(Temp? ignoreTemp, ShelfStorage shelf)
		{
			IStorage[] others = { storageController.GetStorage(Temp.Hot), storageController.GetStorage(Temp.Cold) };
			IStorage? moveTo = null;

			bool canMove = false;
			foreach (IStorage storage in others)
			{
				if (ignoreTemp == storage.Temperature) continue;

				// Cannot move an order if there is no space in the desired storage or there are no orders of that temp on shelf
				if (!storage.HasSpace() || !shelf.ContainsOrderWithTemp(storage.Temperature))
				{
					continue;
				}

				moveTo = storage;
				canMove = true;
				break;
			}
			if (!canMove) return false;

			bool moveResult = false;
			Order moved = shelf.RemoveOrderWithTemp(moveTo.Temperature)!;
			moveResult = moveTo.StoreOrder(moved);
			if (moveResult)
			{
				LogAction<MoveAction>(moved.ID, moveTo.Temperature);
				orderLookup[moved.ID] = moveTo.Temperature;
			}
			return moveResult;
		}

		private void PopOrderFromShelf(ShelfStorage shelf)
		{
			Order? discarded = shelf.PopOrder();
			// This should never happen : Pop should only be used when shelf is full, so we throw an exception
			if(discarded == null)
			{
				throw new Exception("Unable to pop order from shelf");
			}
			orderLookup.Remove(discarded.ID);
			LogAction<DiscardAction>(discarded.ID, shelf.Temperature);
		}

		/// <summary>
		/// Pickup order by ID, if it exists. Uses lock{} scope for thread safety.
		/// </summary>
		/// <param name="ID">ID of the order</param>
		/// <returns>Order stored with the ID specified, if it exists. Otherwise null</returns>
		public Order? PickupOrder(string ID)
		{
			Order? order;
			Temp? storageTemp = null;
			lock(transactionLock)
			{
				if (!orderLookup.TryGetValue(ID, out Temp storedIn))
				{
					//Console.WriteLine($"Tried to pickup order: {ID}, but it is not in storage");
					return null;
				}

				IStorage storage = storageController.GetStorage(storedIn);
				order = storage.RemoveOrder(ID);

				if (order != null)
				{
					orderLookup.Remove(ID);
					storageTemp = storedIn;
					LogAction<PickupAction>(ID, storageTemp.Value);
				}
			}

			return order;
		}

		private void LogAction<T>(string id, Temp storageParam) where T: ActionBase
		{
			long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			logger.LogAction(ActionFactory.Create<T>(unixTimestamp, id, storageParam));
		}

	}
}
