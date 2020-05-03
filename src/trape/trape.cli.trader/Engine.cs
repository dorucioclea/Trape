﻿using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Fees;
using trape.cli.trader.Trading;

namespace trape.cli.trader
{
    /// <summary>
    /// The Engine manages proper startup and shutdown of required services
    /// </summary>
    public class Engine : BackgroundService
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private IBuffer _buffer;

        /// <summary>
        /// Analyst
        /// </summary>
        private IAnalyst _analyst;

        /// <summary>
        /// Trading Team
        /// </summary>
        private ITradingTeam _tradingTeam;

        /// <summary>
        /// Accountant
        /// </summary>
        private IAccountant _accountant;

        /// <summary>
        /// Fee Watchdog
        /// </summary>
        private IFeeWatchdog _feeWatchdog;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Engine</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="analyst">Analyst</param>
        /// <param name="tradingTeam">Trading Team</param>
        /// <param name="accountant">Accountant</param>
        public Engine(ILogger logger, IBuffer buffer, IAnalyst analyst, ITradingTeam tradingTeam, IAccountant accountant, IFeeWatchdog feeWatchdog)
        {
            #region Argument checks

            if (logger == null)
            {
                throw new ArgumentNullException(paramName: nameof(logger));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(paramName: nameof(buffer));
            }

            if (analyst == null)
            {
                throw new ArgumentNullException(paramName: nameof(analyst));
            }

            if (tradingTeam == null)
            {
                throw new ArgumentNullException(paramName: nameof(tradingTeam));
            }

            if (accountant == null)
            {
                throw new ArgumentNullException(paramName: nameof(accountant));
            }

            if (feeWatchdog == null)
            {
                throw new ArgumentNullException(paramName: nameof(feeWatchdog));
            }

            #endregion

            this._logger = logger.ForContext<Engine>();
            this._buffer = buffer;
            this._analyst = analyst;
            this._tradingTeam = tradingTeam;
            this._accountant = accountant;
            this._feeWatchdog = feeWatchdog;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts all processes to begin trading
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Engine is starting");

            await this._buffer.Start().ConfigureAwait(true);

            this._analyst.Start();

            await this._accountant.Start().ConfigureAwait(true);

            this._tradingTeam.Start();

            this._feeWatchdog.Start();

            this._logger.Information("Engine is started");
        }

        /// <summary>
        /// Stops all process to end trading
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Information("Engine is stopping");

            this._feeWatchdog.Finish();

            await this._tradingTeam.Finish().ConfigureAwait(true);

            await this._accountant.Finish().ConfigureAwait(true);

            this._analyst.Finish();

            this._buffer.Finish();

            this._logger.Information("Engine is stopped");
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._buffer.Dispose();
                this._accountant.Dispose();
                this._analyst.Dispose();
                this._tradingTeam.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
