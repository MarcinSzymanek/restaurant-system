using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudKitchenChallenge.KitchenSystem.Actions;

namespace CloudKitchenChallenge.Logging
{
	public interface IActionLogger
	{
		void LogAction(ActionBase action);
		void Run();
		Task Finish();
	}

	public interface IFilelogger
	{
		string? LastFilename{ get; }
	}
}
