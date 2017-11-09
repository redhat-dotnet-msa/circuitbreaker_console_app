using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Net.Http;
using Polly;
using Polly.Timeout;
using Polly.CircuitBreaker;

namespace circuitbreaker_console_app 
{
    public class Program
    {
        public static void Main(string[] args)
	    {
		    RunAsync().Wait();
	    }

	static async Task RunAsync()
	{
		CancellationToken cancellationToken = new CancellationToken();

        double timeoutvalue = 200;
        TimeoutStrategy timeoutStrategy = TimeoutStrategy.Pessimistic;
        
		var client = new HttpClient();

        var timeoutPolicy = Policy
            .TimeoutAsync(TimeSpan.FromMilliseconds(timeoutvalue), timeoutStrategy);
            
		var circuitBreakerPolicy = Policy
			.Handle<Exception>()
            .Or<TimeoutRejectedException>()
            .Or<TimeoutException>()
			.CircuitBreakerAsync(
				exceptionsAllowedBeforeBreaking: 4,
				durationOfBreak: TimeSpan.FromSeconds(8),
				onBreak: (ex, breakDelay) =>
				{
//					Console.WriteLine(".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!");
//					Console.WriteLine("...due to: " + ex.Message);
					Console.WriteLine("Hello!");
				},
//				onReset: () => Console.WriteLine(".Breaker logging: Call ok! Closed the circuit again."),
//				onHalfOpen: () => Console.WriteLine(".Breaker logging: Half-open; next call is a trial.")

				onReset: () => Console.WriteLine("Hello!"),
				onHalfOpen: () => Console.WriteLine("Hello!")
				);
            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
            {
                i++;
                try
                {
                    // Retry the following call according to the policy - 3 times.
                    string msg = await Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy).ExecuteAsync<String>(() =>
                    {
                        return client.GetStringAsync("http://10.1.2.2:5000/greeting");
                    });
                    Console.WriteLine("Request " + i + ": " + msg);
                }
                catch (BrokenCircuitException b) {
                    Console.WriteLine("Request " + i + ": Hello!");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Request " + i + ": Hello!");
                }

                // Wait one second
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            };
            }
        }
    }
