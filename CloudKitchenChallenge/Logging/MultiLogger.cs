using CloudKitchenChallenge.KitchenSystem.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CloudKitchenChallenge.Logging
{
	// Wrapper around multiple loggers to easily log to multiple outputs
	public class MultiLogger : IActionLogger, IFilelogger
	{
		List<IActionLogger> loggers;
		public string? LastFilename
		{
			get 
			{
				JsonLogger? jsonLogger = loggers.Find((IActionLogger logger) => 
					{
						return (logger is JsonLogger); 
					}
				) as JsonLogger;

				if (jsonLogger != null) return jsonLogger.LastFilename;
				return null;	
			}
		}
		public MultiLogger(bool useJson = true, bool useConsole = true)
		{
			loggers = new List<IActionLogger>();
			if (useJson) loggers.Add(new JsonLogger());
			if (useConsole) loggers.Add(new ConsoleLogger());
		}
		public void Run()
		{
			foreach(var logger in loggers)
			{
				logger.Run();
			}
		}

		public async Task Finish()
		{
			foreach (var logger in loggers)
			{
				await logger.Finish();
			}
		}

		public void LogAction(ActionBase action)
		{
			foreach (var logger in loggers)
			{
				logger.LogAction(action);
			}
		}
	}
}
