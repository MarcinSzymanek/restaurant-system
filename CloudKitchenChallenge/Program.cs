using CloudKitchenChallenge.TestApp;
using CloudKitchenChallenge.Utils;
using CloudKitchenChallenge.Models;
using Microsoft.Extensions.Logging;



bool testOnServer = bool.Parse(Environment.GetEnvironmentVariable("SERVER_TEST"));


// Fetch a random test problem
Random rng = new Random();
int seed = rng.Next(1000);

(string, OrderDetails[]?) problem = await ServerUtils.FetchProblem(seed);

int testId = int.Parse(problem.Item1);
var orders = problem.Item2;

if(orders.Length < 1)
{
	Console.Error.WriteLine("Orders were not fetched correctly!");
	return;
}

// Parse console args
long placeRateUs, minPickupDelayUs, maxPickupDelayUs;
long.TryParse(args[0], out placeRateUs);
long.TryParse(args[1], out minPickupDelayUs);
long.TryParse(args[2], out maxPickupDelayUs);

// Run the app (feed orders into the system) 
App app = new App(placeRateUs, minPickupDelayUs, maxPickupDelayUs);
string actionLogFile = await app.Run(orders);

if (!testOnServer) return;

// Test on server
Console.WriteLine($"Testing the solution on server");

ServerUtils.AppOptions options = new ServerUtils.AppOptions(
	placeRateUs,
	minPickupDelayUs,
	maxPickupDelayUs
);

await ServerUtils.TestOnChallengeServer(actionLogFile, testId, options);
