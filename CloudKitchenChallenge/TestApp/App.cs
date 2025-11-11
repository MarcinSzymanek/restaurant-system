using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudKitchenChallenge.Logging;
using CloudKitchenChallenge.Models;
using CloudKitchenChallenge.KitchenSystem;

namespace CloudKitchenChallenge.TestApp
{
	public class App
	{
		int placeRateMs;
		int minPickupIntervalMs;
		int maxPickupIntervalMs;
		Random rng;
		Kitchen kitchen;
		ConcurrentQueue<Task> pickupTasks;
		MultiLogger logger;

		public App(long placeRateUs, long minPickupIntUs, long maxPickupIntUs)
		{
			placeRateMs = (int)(placeRateUs / 1000);
			minPickupIntervalMs = (int)(minPickupIntUs / 1000);
			maxPickupIntervalMs = (int)(maxPickupIntUs / 1000);
			rng = new Random();
			logger = new MultiLogger();
			kitchen = new Kitchen(logger);
			pickupTasks = new ConcurrentQueue<Task>();
		}
		public async Task<string> Run(OrderDetails[] orders)
		{
			Console.WriteLine("Running test harness with following parameters:");
			Console.WriteLine($"place rate: {placeRateMs}ms");
			Console.WriteLine($"min pickup interval: {minPickupIntervalMs}ms");
			Console.WriteLine($"max pickup interval: {maxPickupIntervalMs}ms");
			logger.Run();
			Task main = PlaceOrdersContinuously(orders);

			// Remove PickupOrder tasks from task queue periodically
			while (!main.IsCompleted)
			{
				if (pickupTasks.Count > 0)
				{
					Task next;
					pickupTasks.TryDequeue(out next);
					await next;
				}
				else await Task.Delay(2000);
			}

			while (pickupTasks.Count > 0)
			{
				Task next;
				pickupTasks.TryDequeue(out next);
				await next;
			}
			await logger.Finish();
			Console.WriteLine($"All orders processed. Action log file: {logger.LastFilename}");
			return logger.LastFilename;
		}

		private async Task PlaceOrdersContinuously(OrderDetails[] orders)
		{
			for (int i = 0; i < orders.Length; i++)
			{
				OrderDetails order = orders[i];
				int pickupDelay = rng.Next(minPickupIntervalMs, maxPickupIntervalMs);
				kitchen.PlaceOrder(order, pickupDelay);
				pickupTasks.Enqueue(PickupAfterDelay(order.ID, pickupDelay));
				await Task.Delay(placeRateMs);
			}
			Console.WriteLine("Finished placing orders");
		}

		private async Task PickupAfterDelay(string id, int delayMs)
		{
			await Task.Delay(delayMs);
			kitchen.PickupOrder(id);
		}
	}
}
