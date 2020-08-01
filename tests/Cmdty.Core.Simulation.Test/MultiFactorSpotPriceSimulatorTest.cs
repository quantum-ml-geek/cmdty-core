//#define PRINT_TEST_SIM_INFO
#region License
// Copyright (c) 2019 Jake Fowler
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without 
// restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following 
// conditions:
//
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cmdty.Core.Simulation.MultiFactor;
using Cmdty.TimePeriodValueTypes;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using NUnit.Framework;

namespace Cmdty.Core.Simulation.Test
{
    [TestFixture]
    public sealed class MultiFactorSpotPriceSimulatorTest
    {

        // Tests to do:
        // Input parameters:
        //  - Zero vol - simulation equals curve
        //  - Single non-mean reverting factor
        //  - Two non-mean reverting factors
        //  - Two factors with -1 correlation
        //  - One more factors with high mean reversion:
        //      - Standard deviation of increments far in future go to zero
        // Outputs to test:
        //  - Expectation equals curve
        //  - Variance is expected
        //  - Characteristic function
        //  - Correlation of increments
        //  - Properties of factors:
        //      - Mean with 3 sds of 0 (only if mean reversion is greater than 0?)
        //      - Correlation of 1 day change equals in put correlation
        //      - Auto-correlation is as expected

        private readonly Dictionary<Day, double> _dailyForwardCurve;
        private readonly DateTime _currentDate;
        private readonly int _seed;
        private MultiFactorSpotSimResults<Day> _singleNonMeanRevertingFactorResults;
        private MultiFactorParameters<Day> _singleNonMeanRevertingFactorParams;
        private MultiFactorSpotSimResults<Day> _twoNonMeanRevertingFactorsResults;
        private MultiFactorParameters<Day> _twoNonMeanRevertingFactorsParams;

        private MultiFactorSpotSimResults<Day> _meanAndNonMeanRevertingFactorsResults;
        private MultiFactorParameters<Day> _meanAndNonMeanRevertingFactorsParams;


        public MultiFactorSpotPriceSimulatorTest()
        {
            _seed = 12;
            _currentDate = new DateTime(2020, 07, 27);

            _dailyForwardCurve = new Dictionary<Day, double>
            {
                {new Day(2020, 08, 01), 56.85 },
                {new Day(2021, 01, 15), 59.08 },
                {new Day(2021, 07, 30), 62.453 }
            };

            SimulateForSingleNonMeanRevertingFactor();
            SimulateForTwoNonMeanRevertingFactors();
            SimulateForMeanAndNonMeanRevertingFactors();
        }

        private void SimulateForMeanAndNonMeanRevertingFactors()
        {
            int numSims = 100000;
            
            _meanAndNonMeanRevertingFactorsParams = new MultiFactorParameters<Day>(new[]
            {
                new Factor<Day>(0.0, new Dictionary<Day, double>
                {
                    {new Day(2020, 08, 01), 0.35},
                    {new Day(2021, 01, 15), 0.29},
                    {new Day(2021, 07, 30), 0.32}
                }),
                new Factor<Day>(2.5, new Dictionary<Day, double>
                {
                    {new Day(2020, 08, 01), 0.15},
                    {new Day(2021, 01, 15), 0.18},
                    {new Day(2021, 07, 30), 0.21}
                }),
                new Factor<Day>(16.2, new Dictionary<Day, double>
                {
                    {new Day(2020, 08, 01), 0.95},
                    {new Day(2021, 01, 15), 0.92},
                    {new Day(2021, 07, 30), 0.89}
                })
            }, new[,]
            {
                {1.0, 0.6, 0.3},
                {0.6, 1.0, 0.4},
                {0.3, 0.4, 1.0}
            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);

            var simulator = new MultiFactorSpotPriceSimulator<Day>(_meanAndNonMeanRevertingFactorsParams, _currentDate, _dailyForwardCurve,
                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            _meanAndNonMeanRevertingFactorsResults = simulator.Simulate(numSims);

        }

        private void SimulateForSingleNonMeanRevertingFactor()
        {
            int numSims = 1000000;
            _singleNonMeanRevertingFactorParams = new MultiFactorParameters<Day>(new[]
            {
                new Factor<Day>(0.0, new Dictionary<Day, double>
                {
                    {new Day(2020, 08, 01), 0.45},
                    {new Day(2021, 01, 15), 0.42},
                    {new Day(2021, 07, 30), 0.33}
                })
            }, new [,]
                            {
                                {1.0},
                            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);

            var simulator = new MultiFactorSpotPriceSimulator<Day>(_singleNonMeanRevertingFactorParams, _currentDate, _dailyForwardCurve,
                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            _singleNonMeanRevertingFactorResults = simulator.Simulate(numSims);

        }

        private void SimulateForTwoNonMeanRevertingFactors()
        {
            int numSims = 100000;
            _twoNonMeanRevertingFactorsParams = new MultiFactorParameters<Day>(new[]
            {
                new Factor<Day>(0.0, new Dictionary<Day, double>
                    {
                        {new Day(2020, 08, 01), 0.15},
                        {new Day(2021, 01, 15), 0.12},
                        {new Day(2021, 07, 30), 0.13}
                    }),
                new Factor<Day>(0.0, new Dictionary<Day, double>
                    {
                        {new Day(2020, 08, 01), 0.11},
                        {new Day(2021, 01, 15), 0.19},
                        {new Day(2021, 07, 30), 0.15}
                    })
            }, new [,]
            {
                {1.0, 0.74},
                {0.74, 1.0},
            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);
 
            var simulator = new MultiFactorSpotPriceSimulator<Day>(_twoNonMeanRevertingFactorsParams, _currentDate, _dailyForwardCurve,
                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            _twoNonMeanRevertingFactorsResults = simulator.Simulate(numSims);

        }

        private MultiFactorSpotSimResults<Day> SimulateForZeroVolatility()
        {
            int numSims = 10;
            var zeroFactorVols = new Dictionary<Day, double>
            {
                {new Day(2020, 08, 01), 0.0},
                {new Day(2021, 01, 15), 0.0},
                {new Day(2021, 07, 30), 0.0}
            };
            var multiFactorParameters = new MultiFactorParameters<Day>(new []
            {
                new Factor<Day>(0.0, zeroFactorVols), 
                new Factor<Day>(2.5, zeroFactorVols), 
                new Factor<Day>(16.2, zeroFactorVols) 
            }, new [,]
                            {
                                {1.0, 0.6, 0.3},
                                {0.6, 1.0, 0.4},
                                {0.3, 0.4, 1.0}
                            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);

            var simulator = new MultiFactorSpotPriceSimulator<Day>(multiFactorParameters, _currentDate, _dailyForwardCurve, 
                                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            return simulator.Simulate(numSims);

        }

        [Test]
        public void Simulate_ZeroVolatility_PricesEqualForwardCurve()
        {
            MultiFactorSpotSimResults<Day> simResults = SimulateForZeroVolatility();

            IReadOnlyList<Day> simulatedPeriods = simResults.SimulatedPeriods;
            for (int periodIndex = 0; periodIndex < simulatedPeriods.Count; periodIndex++)
            {
                double forwardPrice = _dailyForwardCurve[simulatedPeriods[periodIndex]];
                ReadOnlyMemory<double> simulatedSpotPrices = simResults.SpotPricesForStepIndex(periodIndex);
                for (int simIndex = 0; simIndex < simulatedSpotPrices.Length; simIndex++)
                {
                    Assert.AreEqual(forwardPrice, simulatedSpotPrices.Span[simIndex]);
                }
            }
        }

        [Test]
        public void Simulate_SingleNonMeanRevertingFactor_Within3StanDevsOfForwardPrice()
        {
            AssertAverageSimSpotPricesWithin3StanDevsOfForwardPrice(_singleNonMeanRevertingFactorResults, _dailyForwardCurve);
        }

        [Test]
        [Ignore("Redo this test either: normalising the spot prices around their means or just doing this test on the factors.")]
        public void Simulate_SingleNonMeanRevertingFactor_NonOverlappingIncrementReturnsZeroCorrelation()
        {
            ReadOnlySpan<double> step1SpotPriceSims = _singleNonMeanRevertingFactorResults.SpotPricesForStepIndex(0).Span;
            ReadOnlySpan<double> step2SpotPriceSims = _singleNonMeanRevertingFactorResults.SpotPricesForStepIndex(1).Span;
            ReadOnlySpan<double> step3SpotPriceSims = _singleNonMeanRevertingFactorResults.SpotPricesForStepIndex(2).Span;

            double correlation = LogReturnsCorrelation(step1SpotPriceSims, step2SpotPriceSims, 
                step2SpotPriceSims, step3SpotPriceSims);

            Console.WriteLine(correlation);
        }

        [Test]
        public void Simulate_MeanAndNonMeanRevertingFactors_Within3StanDevsOfForwardPrice()
        {
            AssertAverageSimSpotPricesWithin3StanDevsOfForwardPrice(_meanAndNonMeanRevertingFactorsResults, _dailyForwardCurve);
        }

        [Test]
        public void Simulate_TwoNonMeanRevertingFactors_Within3StanDevsOfForwardPrice()
        {
            AssertAverageSimSpotPricesWithin3StanDevsOfForwardPrice(_twoNonMeanRevertingFactorsResults, _dailyForwardCurve);
        }

        [Test]
        public void Simulate_SingleNonMeanRevertingFactor_StandDevEqualsVolSquRootTimeToMaturity()
        {
            for (int i = 0; i < _singleNonMeanRevertingFactorResults.NumSteps; i++)
            {
                ReadOnlyMemory<double> simulatedSpotPrices = _singleNonMeanRevertingFactorResults.SpotPricesForStepIndex(i);
                Day period = _singleNonMeanRevertingFactorResults.SimulatedPeriods[i];
                double factorVolForPeriod = _singleNonMeanRevertingFactorParams.Factors.Single().Volatility[period];
                double timeToMaturity = TimeFunctions.Act365(_currentDate, period.Start);
                double expectedLogStanDev = factorVolForPeriod * Math.Sqrt(timeToMaturity);
                double simulatedLogStanDev = LogStandardDeviation(simulatedSpotPrices.Span);
                double percentageError = Math.Abs((simulatedLogStanDev - expectedLogStanDev) / expectedLogStanDev);
                Assert.LessOrEqual(percentageError, 0.001);
            }
        }

        private static void AssertAverageSimSpotPricesWithin3StanDevsOfForwardPrice<T>(MultiFactorSpotSimResults<T> simResults, Dictionary<T, double> forwardCurve)
            where T : ITimePeriod<T>
        {

            IReadOnlyList<T> simulatedPeriods = simResults.SimulatedPeriods;
            for (int periodIndex = 0; periodIndex < simulatedPeriods.Count; periodIndex++)
            {
                double forwardPrice = forwardCurve[simulatedPeriods[periodIndex]];
                ReadOnlyMemory<double> simulatedSpotPrices = simResults.SpotPricesForStepIndex(periodIndex);

                (double averageSimSpotPrice, double sampleStanDev) = SampleStandardDeviationAndMean(simulatedSpotPrices.Span);
                double standardError = sampleStanDev / Math.Sqrt(simResults.NumSims);
                double error = averageSimSpotPrice - forwardPrice;
                double numStanDevsError = error / standardError;
                Assert.LessOrEqual(numStanDevsError, 3.0);

#if PRINT_TEST_SIM_INFO
                Console.WriteLine("forward price: " + forwardPrice);
                Console.WriteLine("average sim price: " + averageSimSpotPrice);
                Console.WriteLine("error: " + error);
                Console.WriteLine("standard error: " + standardError);
                Console.WriteLine("num stan devs: " + error / standardError);
                Console.WriteLine();
#endif
            }
        }

        private static double Mean(ReadOnlySpan<double> span)
        {
            double sum = 0;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < span.Length; i++)
            {
                sum += span[i];
            }
            return sum / span.Length;
        }

        private static (double Mean, double SampleStanDev) SampleStandardDeviationAndMean(ReadOnlySpan<double> span)
        {
            double mean = Mean(span);
            double variance = 0.0;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < span.Length; i++)
            {
                variance += Math.Pow(span[i] - mean, 2);
            }

            double stanDev = Math.Sqrt(variance / (span.Length - 1));
            return (Mean: mean, SampleStanDev: stanDev);
        }

        private static double LogStandardDeviation(ReadOnlySpan<double> span)
        {
            var logVector = Vector<double>.Build.DenseOfArray(span.ToArray());
            logVector.PointwiseLog(logVector);
            return logVector.StandardDeviation();
        }

        private static double LogReturnsCorrelation(ReadOnlySpan<double> priceSims1Start, ReadOnlySpan<double> priceSims1End,
                        ReadOnlySpan<double> priceSims2Start, ReadOnlySpan<double> priceSims2End)
        {
            Vector<double> logChanges1 = LogChange(priceSims1Start, priceSims1End);
            Vector<double> logChanges2 = LogChange(priceSims2Start, priceSims2End);

            double correlation = Correlation.Pearson(logChanges1, logChanges2);
            return correlation;
        }

        private static Vector<double> LogChange(ReadOnlySpan<double> priceSimsStart, ReadOnlySpan<double> priceSimsEnd)
        {
            Vector<double> startVector = ToVector(priceSimsStart);
            Vector<double> endVector = ToVector(priceSimsEnd);
            Vector<double> returns = endVector.PointwiseDivide(startVector);
            returns.PointwiseLog(returns);
            return returns;
        }

        private static Vector<double> ToVector(ReadOnlySpan<double> span)
        {
            return Vector<double>.Build.DenseOfArray(span.ToArray());
        }

    }
}