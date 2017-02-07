using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Service.Fabric.Samples.VoicemailBox.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;

namespace Microsoft.Azure.Service.Fabric.Samples.VoicemailBox
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class PrimeNumbersActor : Actor, IPrimeNumbersActor
    {
        ServiceEventSource eventSource = new ServiceEventSource();

        /// <summary>
        /// Initializes a new instance of PrimeNumbersActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public PrimeNumbersActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            var startAndEndNumber = actorId.GetStringId().Split('-');
            startNumber = int.Parse(startAndEndNumber.First());
            endNumber = int.Parse(startAndEndNumber.Last());
            primeNumbers = new List<int>(Math.Abs(endNumber - startNumber));
            nonePrimeNumbers = new List<int>(Math.Abs(endNumber - startNumber));
            eventSource.Message($"created actor with id {actorId.ToString()}");
            eventSource.Message($"starting calculation for {actorId.ToString()}");
            Calculate();
            eventSource.Message($"finished calculation for {actorId.ToString()}");
        }

        private readonly int startNumber;
        private readonly int endNumber;
        private readonly IList<int> primeNumbers;
        private readonly IList<int> nonePrimeNumbers;

        static bool IsPrime(int number)
        {

            if (number == 1) return false;
            if (number == 2) return true;

            for (int i = 2; i <= Math.Ceiling(Math.Sqrt(number)); ++i)
            {
                if (number % i == 0) return false;
            }

            return true;
        }


        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

            return this.StateManager.TryAddStateAsync("count", 0);
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> IPrimeNumbersActor.GetCountAsync(CancellationToken cancellationToken)
        {
            return this.StateManager.GetStateAsync<int>("count", cancellationToken);
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task IPrimeNumbersActor.SetCountAsync(int count, CancellationToken cancellationToken)
        {
            // Requests are not guaranteed to be processed in order nor at most once.
            // The update function here verifies that the incoming count is greater than the current count to preserve order.
            return this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value, cancellationToken);
        }

        public async Task<IList<int>> GetPrimes(CancellationToken cancellationToken)
        {
            if (primeNumbers.Any())
            {
                return primeNumbers;
            }

            Calculate();

            await StateManager.SetStateAsync(nameof(primeNumbers), primeNumbers, cancellationToken);
            return primeNumbers;
        }

        public async Task<IList<int>> GetNonePrimes(CancellationToken cancellationToken)
        {
            if (nonePrimeNumbers.Any())
            {
                return nonePrimeNumbers;
            }

            Calculate();

            await StateManager.SetStateAsync(nameof(nonePrimeNumbers), nonePrimeNumbers, cancellationToken);
            return primeNumbers;
        }

        private void Calculate()
        {
            for (int i = startNumber; i < endNumber; i++)
            {
                if (IsPrime(i))
                {
                    primeNumbers.Add(i);
                }
                else
                {
                    nonePrimeNumbers.Add(i);
                }
            }
        }
    }
}
