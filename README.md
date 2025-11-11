# CloudKitchen Backend Engineering Challenge

This is my proposal for a solution for M1d Engineering Challenge. The system implements a single process in-memory kitchen order storage, with 3 different storage types as per specification. The test harness should be run in a docker container by using instructions specified below.

The program will output actions as well as program flow to console, and output actions as a json file. The actions will be packed in an http request and sent to the challenge server at the end of the program's runtime.


## Discard Logic
I had two main ideas for the discard logic:

1. Have a task running in the background which updates current order freshness on the shelf every n seconds and sorts the data storage.
2. Calculate 'estimated freshness at pickuptime' when receiving the order based on either average pickup time OR by taking a pickup time parameter. The orders can now be stored in a Sorted Dictionary, and the first value will have lowest estimated freshness (highest spoil rate)

I went with option 2. While not the most accurate, it is relatively fast with $O(log(n))$ discard time, and simple to understand. I felt that adding another thread and sorting elements at regular interval would be disruptive for the system, and potentially too slow in a multithreaded setting - no other operation on the shelf could be performed during that time.

## Build instructions


Navigate to the dockerfile directory (same as this README) and build the project using the command below. By default unit tests are also run in this stage.

```
docker build -t ck-test-harness .
```
Run the test harness with default parameters (500 ms place rate, 400-800 ms pickup interval)
```
docker run -it ck-test-harness
```
You can specify the parameters by overriding the Dockerfile entrypoint as such:
```
docker run -it --entrypoint dotnet ck-test-harness CloudKitchenChallenge.dll <placeRateUs> <minPickupIntervalUs> <maxPickupIntervalUs>
```
For example:
```
docker run -it --entrypoint dotnet ck-test-harness CloudKitchenChallenge.dll 100000 3000000 6000000
```
Alternatively, you can manually enter the container after it's been built, and run the program from the container:
```
\CKChallenge> docker run -it --entrypoint /bin/bash ck-test-harness
app@3a3bbb60fb0f:/app$ dotnet CloudKitchenChallenge.dll 80000 1000000 2000000
```

## Environment variables

By default the program sends the log of actions to the challenge server at the end of its runtime. This functionality can be disabled by setting the env variable SERVER_TEST=bool .

Additionally, the console log timestamps are by default using +0 UTC time. The hour can be offset by setting the variable UTC_OFFSET=int . This only affects timestamps in console logs.

These can both be set either in the Dockerfile itself, or in the container while it's running, or using the -e docker run parameter.