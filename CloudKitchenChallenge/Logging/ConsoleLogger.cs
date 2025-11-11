using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudKitchenChallenge.KitchenSystem.Actions;

namespace CloudKitchenChallenge.Logging
{
	/// <summary>
	/// Logs to console asynchronously. Must be started using Run() and finished using await Finish()
	/// Uses a separate thread which waits for messages in a queue before outputting them in console
	/// </summary>
	public class ConsoleLogger : IActionLogger
	{
		int utcHourOffset = 0;
		const string offsetEnv = "UTC_OFFSET";
		Task worker;
		BlockingCollection<ActionBase> logQueue;
		public ConsoleLogger()
		{
			string? offset = Environment.GetEnvironmentVariable(offsetEnv);
			if (offset != null) utcHourOffset = int.Parse(offset);
			logQueue = new BlockingCollection<ActionBase>();
		}
		DateTime UnixTimeStampToDateTime(long unixTimeStamp)
		{
			DateTime dateTime = DateTime.UnixEpoch;
			dateTime = dateTime.AddMilliseconds(unixTimeStamp).AddHours(utcHourOffset);
			return dateTime;
		}
		public void Run()
		{
			worker = Task.Run(Worker);
		}

		public async Task Finish()
		{
			Console.WriteLine("Finishing console logger task");
			if (worker == null)
			{
				throw new NullReferenceException("Worker should not be null on Finish call. Did you call 'Run()' beforehand?");
			}
			while (logQueue.Count > 0)
			{
				await Task.Delay(500);
			}
			logQueue.CompleteAdding();
			Console.WriteLine("Waiting for worker to finish");
			await worker;
		}

		public void LogAction(ActionBase action)
		{
			logQueue.Add(action);
		}

		private void AddConditionalInfo(ActionBase action, ref string msg)
		{
			string append = "";
			if(action is PlaceAction)
			{
				append = $" in {action.StorageParam}";
			}
			else if(action is MoveAction)
			{
				append = $" from Shelf to {action.StorageParam}";
			}
			else if(action is PickupAction || action is DiscardAction)
			{
				append = $" from {action.StorageParam}";
			}
			msg += append;
		}

		private async Task Worker()
		{
			try
			{
				while (!logQueue.IsCompleted)
				{
					foreach (ActionBase action in logQueue.GetConsumingEnumerable())
					{
						DateTime time = UnixTimeStampToDateTime(action.Timestamp);

						string msg = $"{time.ToString("hh:mm:ss.ff")} orderId: {action.ID}, action: {action.Action},";
						AddConditionalInfo(action, ref msg);
						Console.WriteLine(msg);
						
					}
					await Task.Delay(100);
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"An unexpected error has occurred: {e.Message}");
			}
		}
	}
}
