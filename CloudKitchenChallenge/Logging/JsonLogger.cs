using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using CloudKitchenChallenge.KitchenSystem.Actions;

namespace CloudKitchenChallenge.Logging
{
	/// <summary>
	/// Logs actions as JSON objects to file asynchronously. Must be started using Run() and finished using await Finish()
	/// Uses a separate thread which waits for messages in a queue before writing to a file
	/// Filename can be accessed when logging is finished via LastFilename property
	/// </summary>
	public class JsonLogger : IActionLogger, IFilelogger
	{
		readonly BlockingCollection<ActionBase> logQueue;
		Task? worker;
		const string filenameSuffix = "actionLog.json";
		string? lastFilename;
		public string? LastFilename
		{
			get => lastFilename;
		}
		readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			Converters = { new JsonStringEnumConverter() }
		};
		public JsonLogger()
		{
			logQueue = new BlockingCollection<ActionBase>();
		}
		public void Run()
		{
			worker = Task.Run(Worker);
		}
		public async Task Finish()
		{
			Console.WriteLine("Finishing json logger task");
			if(worker == null) 
			{
				throw new NullReferenceException("Worker should not be null on Finish call. Did you call 'Run()' beforehand?");
			}
			while(logQueue.Count > 0)
			{
				Console.WriteLine("Waiting for logqueue to empty");
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
		private string BuildFilename()
		{
			return (DateTime.Now.ToFileTime() + filenameSuffix);
		}
		private async Task Worker()
		{
			string filename = BuildFilename();
			lastFilename = filename;
			try
			{
				File.AppendAllText(filename, "[");

				while(!logQueue.IsCompleted) 
				{
					foreach(ActionBase action in logQueue.GetConsumingEnumerable())
					{
						string serialized = JsonSerializer.Serialize(action, serializerOptions);
						await File.AppendAllTextAsync(filename, serialized + ",");
					}
					// After processing items, do a non blocking delay, file logging is not time critical
					await Task.Delay(100);
				}
				File.AppendAllText(filename, "]");
			}
			catch(OperationCanceledException){
				// Thread completed succesfully (cancelled by cancellation token)
				Console.WriteLine("Received cancellation token");
				File.AppendAllText(filename, "]");
			}
			catch(Exception e){
				// An error has occured
				Console.Error.WriteLine($"An unexpected error has occurred: {e.Message}");
			}
		}
	}
}
