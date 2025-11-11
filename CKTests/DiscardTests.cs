using CloudKitchenChallenge.KitchenSystem;
using CloudKitchenChallenge.Models;
using CKTests.Fakes;

namespace CKTests
{
	[TestClass]
	public sealed class DiscardTests
	{
		[TestMethod]
		public void SystemDiscardsPredictedLeastFreshShelfElements()
		{
			const int pickupDelay = 500;
			const int startOrderFreshness = 50;
			const int replaceOrderFreshness = 300;
			const int iterations = 100;
			const int shelfStorage = 12;

			for(int i = 0; i < iterations; i++)
			{
				// These orders should be popped in order of freshness
				OrderDetails[] startOrders = GenerateTestData(shelfStorage);
				OrderDetails[] checkOrders = startOrders.OrderBy((OrderDetails order) => order.Freshness).ToArray();
				// These orders all have more freshness then start orders.
				OrderDetails[] replaceOrders = GenerateTestData(shelfStorage, Temp.Room, replaceOrderFreshness);
				Kitchen kitchen = new Kitchen(new Fakes.StubLogger());
			
				for(int j = 0; j < startOrders.Length; j++)
				{
					kitchen.PlaceOrder(startOrders[j], pickupDelay);
				}

				// Place new orders. Old orders should no longer be available in order of freshness
				for(int j = 0; j < replaceOrders.Length; j++)
				{
					kitchen.PlaceOrder(replaceOrders[j], pickupDelay);
					Order? order = kitchen.PickupOrder(checkOrders[j].ID);
					Assert.IsNull(order);
				}
			}
		}

		[TestMethod]
		public void SystemDiscardsHotElementsBeforeRoomElements()
		{
			const int iterations = 100;
			const int pickupDelay = 500;
			OrderDetails[] replaceOrders = GenerateTestData(12, Temp.Room, 999);
			OrderDetails[] hotDummyOrders = GenerateTestData(6, Temp.Hot, 999);

			for(int i = 0; i < iterations; i++)
			{
				(OrderDetails[] roomOrders, OrderDetails[] hotOrders) = GenerateSameFreshnessData(Temp.Room, Temp.Hot);

				Kitchen kitchen = new Kitchen(new Fakes.StubLogger());
				for(int j = 0; j < hotDummyOrders.Length; j++)
				{
					kitchen.PlaceOrder(hotDummyOrders[j], pickupDelay);
				}

				for(int j = 0; j < roomOrders.Length; j++)
				{
					kitchen.PlaceOrder(roomOrders[j], pickupDelay);
					kitchen.PlaceOrder(hotOrders[j], pickupDelay);
				}

				for(int j = 0; j < 6; j++)
				{
					kitchen.PlaceOrder(replaceOrders[j], pickupDelay);
					Order? order = kitchen.PickupOrder(hotOrders[j].ID);
					Assert.IsNull(order);
				}

				for(int j = 0; j < 6; j++)
				{
					Order? order = kitchen.PickupOrder(roomOrders[j].ID);
					Assert.IsNotNull(order);
					Assert.AreEqual(order.ID, roomOrders[j].ID);
				}
			}

		}

		// Generate two sets of orderdetails with same freshness, but different temperature
		private (OrderDetails[], OrderDetails[]) GenerateSameFreshnessData(Temp first, Temp second, int count = 6)
		{
			OrderDetails[] firstOrders = new OrderDetails[count];
			OrderDetails[] secondOrders = new OrderDetails[count];
			Random rng = new Random();
			const int startFreshness = 50;

			for(int i = 0; i < count; i++)
			{
				int freshness = startFreshness + i;
				firstOrders[i] = new OrderDetails(
					first.ToString() + i.ToString() + freshness.ToString(),
					first.ToString() + freshness.ToString(),
					first,
					freshness
				);

				secondOrders[i] = new OrderDetails(
					second.ToString() + i.ToString() + freshness.ToString(),
					second.ToString() + freshness.ToString(),
					second,
					freshness
				);
			}

			return (firstOrders, secondOrders);
		}

		// Generate Room temp orders with unique random freshness values
		private OrderDetails[] GenerateTestData(
			int count, 
			Temp temp = Temp.Room, 
			int startFreshness = 50, 
			int randomness = 100)
		{
			Random rng = new Random();
			const string namePrefix = "test";
			int freshness;
			List<int> freshValues = new List<int>();

			OrderDetails[] data = new OrderDetails[count];

			for (int i = 0; i < count; i++)
			{
				freshness = startFreshness + rng.Next(randomness);
				// Make sure freshness value is unique
				while (freshValues.Contains(freshness)) freshness = startFreshness + rng.Next(randomness);
				freshValues.Add(freshness);

				data[i] = new OrderDetails(
					namePrefix + i.ToString() + freshness.ToString(),
					namePrefix + freshness.ToString(),
					temp,
					freshness
				);
			}

			return data;
		}
	}
}
