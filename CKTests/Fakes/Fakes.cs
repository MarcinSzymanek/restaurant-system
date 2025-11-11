using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudKitchenChallenge.KitchenSystem.Actions;

namespace CKTests.Fakes
{
	internal class StubLogger: CloudKitchenChallenge.Logging.IActionLogger
	{

		public void Run() { }
		public Task Finish() 
		{
			return Task.Run(() => { });
		}
		public void LogAction(ActionBase _) { }
	}
}
