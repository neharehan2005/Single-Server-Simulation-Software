using System;
using System.Collections.Generic;
using System.Linq;

namespace Member1_DataGeneration
{
    public class SimulationResult
    {
        public int CustomerId { get; set; }
        public double ArrivalTime { get; set; }
        public double ServiceTime { get; set; }
        public double ServiceStartTime { get; set; }
        public double ServiceEndTime { get; set; }
        public double WaitingTime { get; set; }
        public double TimeInSystem { get; set; }
        public int QueueLength { get; set; }
        public string ServerStatus { get; set; } = "";
    }

    public class QueueServerSimulation
    {
        private readonly Queue<Customer> customerQueue = new Queue<Customer>();

        public List<SimulationResult> Run(List<Customer> customers)
        {
            if (customers == null || customers.Count == 0)
                throw new ArgumentException("Customer list cannot be empty.");

            // Customers must be processed in arrival order
            var sortedCustomers = customers
                .OrderBy(c => c.ArrivalTime)
                .ToList();

            var results = new List<SimulationResult>();

            customerQueue.Clear();

            double currentTime = 0.0;
            bool serverBusy = false;

            int nextCustomerIndex = 0;

            while (nextCustomerIndex < sortedCustomers.Count ||
                   customerQueue.Count > 0)
            {
                // -------------------------------------------------
                // CASE 1:
                // Server is idle and queue is empty.
                // Move time to the next customer's arrival.
                // -------------------------------------------------
                if (!serverBusy && customerQueue.Count == 0)
                {
                    Customer customer = sortedCustomers[nextCustomerIndex];

                    currentTime = customer.ArrivalTime;

                    // Server was idle until this customer arrived
                    double serviceStartTime = currentTime;
                    double serviceEndTime =
                        serviceStartTime + customer.ServiceTime;

                    results.Add(new SimulationResult
                    {
                        CustomerId = customer.Id,
                        ArrivalTime = customer.ArrivalTime,
                        ServiceTime = customer.ServiceTime,
                        ServiceStartTime = serviceStartTime,
                        ServiceEndTime = serviceEndTime,
                        WaitingTime = 0.0,
                        TimeInSystem = customer.ServiceTime,
                        QueueLength = 0,
                        ServerStatus = "Idle"
                    });

                    currentTime = serviceEndTime;
                    serverBusy = true;

                    nextCustomerIndex++;

                    // Continue processing arrivals while server is busy
                }

                // -------------------------------------------------
                // CASE 2:
                // Add all customers who have arrived by current time
                // into the waiting queue.
                // -------------------------------------------------
                while (nextCustomerIndex < sortedCustomers.Count &&
                       sortedCustomers[nextCustomerIndex].ArrivalTime <= currentTime)
                {
                    customerQueue.Enqueue(
                        sortedCustomers[nextCustomerIndex]
                    );

                    nextCustomerIndex++;
                }

                // -------------------------------------------------
                // CASE 3:
                // Server is busy and there are customers waiting.
                // Serve the next customer using FCFS.
                // -------------------------------------------------
                if (serverBusy && customerQueue.Count > 0)
                {
                    Customer nextCustomer = customerQueue.Dequeue();

                    double serviceStartTime = currentTime;

                    double waitingTime =
                        serviceStartTime - nextCustomer.ArrivalTime;

                    double serviceEndTime =
                        serviceStartTime + nextCustomer.ServiceTime;

                    // Queue length AFTER taking this customer
                    // from the queue
                    int queueLength = customerQueue.Count;

                    results.Add(new SimulationResult
                    {
                        CustomerId = nextCustomer.Id,
                        ArrivalTime = nextCustomer.ArrivalTime,
                        ServiceTime = nextCustomer.ServiceTime,
                        ServiceStartTime = serviceStartTime,
                        ServiceEndTime = serviceEndTime,
                        WaitingTime = waitingTime,
                        TimeInSystem =
                            serviceEndTime - nextCustomer.ArrivalTime,
                        QueueLength = queueLength,
                        ServerStatus = "Busy"
                    });

                    currentTime = serviceEndTime;
                    serverBusy = true;
                }

                // -------------------------------------------------
                // CASE 4:
                // No one is currently waiting.
                // Check whether the next customer arrives after
                // the server becomes free.
                // -------------------------------------------------
                if (serverBusy &&
                    customerQueue.Count == 0 &&
                    nextCustomerIndex < sortedCustomers.Count &&
                    sortedCustomers[nextCustomerIndex].ArrivalTime > currentTime)
                {
                    serverBusy = false;
                }

                // -------------------------------------------------
                // CASE 5:
                // If server is busy but there are more arrivals,
                // they will be added to the queue in the next loop.
                // -------------------------------------------------
            }

            return results
                .OrderBy(r => r.CustomerId)
                .ToList();
        }

        public void PrintResults(List<SimulationResult> results)
        {
            Console.WriteLine();
            Console.WriteLine(
                "=========================================================================="
            );

            Console.WriteLine(
                "                    SINGLE SERVER QUEUE SIMULATION"
            );

            Console.WriteLine(
                "=========================================================================="
            );

            Console.WriteLine(
                "ID\tArrival\tService\tStart\tEnd\tWait\tSystem\tQueue\tServer"
            );

            Console.WriteLine(
                "--------------------------------------------------------------------------"
            );

            foreach (SimulationResult result in results)
            {
                Console.WriteLine(
                    $"{result.CustomerId}\t" +
                    $"{result.ArrivalTime:F2}\t" +
                    $"{result.ServiceTime:F2}\t" +
                    $"{result.ServiceStartTime:F2}\t" +
                    $"{result.ServiceEndTime:F2}\t" +
                    $"{result.WaitingTime:F2}\t" +
                    $"{result.TimeInSystem:F2}\t" +
                    $"{result.QueueLength}\t" +
                    $"{result.ServerStatus}"
                );
            }

            Console.WriteLine(
                "=========================================================================="
            );
        }
    }
}