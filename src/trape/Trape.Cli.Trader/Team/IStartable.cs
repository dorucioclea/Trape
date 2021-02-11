﻿namespace Trape.Cli.Trader.Team
{
    using Binance.Net.Objects.Spot.MarketData;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for start/stopable classes.
    /// </summary>
    public interface IStartable : IDisposable
    {
        /// <summary>
        /// Symbol
        /// </summary>
        BinanceSymbol? Symbol { get; }

        /// <summary>
        /// Base Asset
        /// </summary>
        string BaseAsset { get; }

        /// <summary>
        /// Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Indicates if agent is faulty
        /// </summary>
        bool IsFaulty { get; }

        /// <summary>
        /// Last time item was active
        /// </summary>
        public DateTime LastActive { get; }

        /// <summary>
        /// Starts an instance
        /// </summary>
        /// <param name="symbol">Symbol</param>
        Task Start(BinanceSymbol symbol);

        /// <summary>
        /// Stops an instance
        /// </summary>
        Task Terminate();
    }
}
