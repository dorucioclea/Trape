﻿using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net;
using CryptoExchange.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache;
using trape.cli.trader.Cache.Models;
using trape.datalayer;
using trape.datalayer.Models;
using trape.mapper;

namespace trape.cli.trader.Market
{
    /// <summary>
    /// Stock exchange class
    /// </summary>
    public class StockExchange : IStockExchange
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
        /// Binance Client
        /// </summary>
        private IBinanceClient _binanceClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>StockExchange</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// /// <param name="buffer">Buffer</param>
        /// <param name="binanceClient">Binance Client</param>
        public StockExchange(ILogger logger, IBuffer buffer, IBinanceClient binanceClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            this._binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            #endregion

            this._logger = logger.ForContext(typeof(StockExchange));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Place the order at Binance
        /// </summary>
        /// <param name="Order">Order</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task PlaceOrder(ClientOrder clientOrder, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _ = clientOrder ?? throw new ArgumentNullException(paramName: nameof(clientOrder));

            #endregion
            // https://nsubstitute.github.io/help/getting-started/
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                this._logger.Information($"{clientOrder.Symbol} @ {clientOrder.Price:0.00}: Issuing {clientOrder.Side.ToString().ToLower()} of {clientOrder.Quantity}");

                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    // Block quantity until order is processed
                    this._buffer.AddOpenOrder(new OpenOrder(clientOrder.Id, clientOrder.Symbol, clientOrder.Quantity));

                    // Place the order with binance tools
                    var binanceSide = (OrderSide)(int)clientOrder.Side;
                    var binanceType = (OrderType)(int)clientOrder.Type;
                    var binanceResponseType = (OrderResponseType)(int)clientOrder.OrderResponseType;
                    var timeInForce = (TimeInForce)(int)clientOrder.TimeInForce;

                    WebCallResult<BinancePlacedOrder> placedOrder;

                    // Market does not require parameter 'timeInForce' and 'price'
                    if (binanceType == OrderType.Market)
                    {
                        placedOrder = await this._binanceClient.PlaceOrderAsync(clientOrder.Symbol, binanceSide, binanceType,
                          quantity: clientOrder.Quantity, newClientOrderId: clientOrder.Id, orderResponseType: binanceResponseType,
                          ct: cancellationToken).ConfigureAwait(true);
                    }
                    else
                    {
                        placedOrder = await this._binanceClient.PlaceOrderAsync(clientOrder.Symbol, binanceSide, binanceType, price: clientOrder.Price,
                          quantity: clientOrder.Quantity, newClientOrderId: clientOrder.Id, orderResponseType: binanceResponseType, timeInForce: timeInForce,
                          ct: cancellationToken).ConfigureAwait(true);
                    }

                    var attempts = 3;
                    while (attempts > 0)
                    {
                        try
                        {
                            // Log order in custom format
                            // Due to timing from Binance this might be executed after updates occurred
                            // So check if it exists in the database and update, otherwise add new
                            var existingClientOrder = database.ClientOrder.FirstOrDefault(c => c.Id == clientOrder.Id);

                            this._logger.Information($"Existing Client Order is null: {existingClientOrder == null}");

                            if (existingClientOrder == null)
                            {
                                database.ClientOrder.Add(clientOrder);

                                this._logger.Information($"Adding new Client Order");
                            }
                            else
                            {
                                existingClientOrder.CreatedOn = clientOrder.CreatedOn;
                                existingClientOrder.Order = clientOrder.Order;
                                existingClientOrder.OrderResponseType = clientOrder.OrderResponseType;
                                existingClientOrder.Price = clientOrder.Price;
                                existingClientOrder.Quantity = clientOrder.Quantity;
                                existingClientOrder.Side = clientOrder.Side;
                                existingClientOrder.Symbol = clientOrder.Symbol;
                                existingClientOrder.TimeInForce = clientOrder.TimeInForce;
                                existingClientOrder.Type = clientOrder.Type;

                                this._logger.Information($"Updating existing Client Order");
                            }

                            this._logger.Debug($"{clientOrder.Symbol}: {clientOrder.Side} {clientOrder.Quantity} {clientOrder.Price:0.00} {clientOrder.Id}");

                            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

                            await LogOrder(database, clientOrder.Id, placedOrder, cancellationToken).ConfigureAwait(true);
                            
                            break;
                        }
                        catch(Exception coe)
                        {
                            attempts--;

                            this._logger.Information($"Failed attempt to store Client Order {clientOrder.Id}; attempt: {attempts}");
                            this._logger.Error(coe, coe.Message);

                            if (attempts == 0)
                            {
                                throw;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    this._logger.Error(e, e.Message);
                }
            }
        }

        /// <summary>
        /// Logs an order in the database
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="newClientOrderId">Generated new client order id</param>
        /// <param name="placedOrder">Placed order or null if none</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        private async Task LogOrder(TrapeContext database, string newClientOrderId, WebCallResult<BinancePlacedOrder> placedOrder, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _ = database ?? throw new ArgumentNullException(paramName: nameof(database));

            if (string.IsNullOrEmpty(newClientOrderId))
            {
                throw new ArgumentNullException(paramName: nameof(newClientOrderId));
            }

            #endregion

            // Check if order placed
            if (placedOrder != null)
            {
                // Check if order is OK and log
                if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                {
                    using (var context1 = LogContext.PushProperty("placedOrder.Error", placedOrder.Error))
                    using (var context2 = LogContext.PushProperty("placedOrder.Data", placedOrder.Data))
                    {
                        this._logger.Error($"Order {newClientOrderId} caused an error");
                    }

                    // Error Codes: https://github.com/binance-exchange/binance-official-api-docs/blob/master/errors.md

                    // Logging
                    this._logger.Error($"{placedOrder.Error?.Code.ToString()}: {placedOrder.Error?.Message}");
                    this._logger.Error(placedOrder.Error?.Data?.ToString());

                    if (placedOrder.Data != null)
                    {
                        this._logger.Warning($"PlacedOrder: {placedOrder.Data.Symbol};{placedOrder.Data.Side};{placedOrder.Data.Type} > ClientOrderId:{placedOrder.Data.ClientOrderId} CummulativeQuoteQuantity:{placedOrder.Data.CummulativeQuoteQuantity} OriginalQuoteOrderQuantity:{placedOrder.Data.OriginalQuoteOrderQuantity} Status:{placedOrder.Data.Status}");
                    }

                    // TODO: 1015 - TOO_MANY_ORDERS
                }
                else
                {
                    var attempts = 3;
                    while (attempts > 0)
                    {
                        try
                        {
                            var newPlacedOrder = Translator.Translate(placedOrder.Data);
                            var existingPlacedOrder = database.PlacedOrders.FirstOrDefault(p => p.OrderId == newPlacedOrder.OrderId);

                            if (existingPlacedOrder == null)
                            {
                                await database.PlacedOrders.AddAsync(newPlacedOrder).ConfigureAwait(true);
                            }
                            else
                            {
                                existingPlacedOrder.ClientOrderId = newPlacedOrder.ClientOrderId;
                                existingPlacedOrder.CummulativeQuoteQuantity = newPlacedOrder.CummulativeQuoteQuantity;
                                existingPlacedOrder.ExecutedQuantity = newPlacedOrder.ExecutedQuantity;
                                existingPlacedOrder.MarginBuyBorrowAmount = newPlacedOrder.MarginBuyBorrowAmount;
                                existingPlacedOrder.MarginBuyBorrowAsset = newPlacedOrder.MarginBuyBorrowAsset;
                                existingPlacedOrder.OrderListId = newPlacedOrder.OrderListId;
                                existingPlacedOrder.OriginalClientOrderId = newPlacedOrder.OriginalClientOrderId;
                                existingPlacedOrder.OriginalQuantity = newPlacedOrder.OriginalQuantity;
                                existingPlacedOrder.OriginalQuoteOrderQuantity = newPlacedOrder.OriginalQuoteOrderQuantity;
                                existingPlacedOrder.Price = newPlacedOrder.Price;
                                existingPlacedOrder.Side = newPlacedOrder.Side;
                                existingPlacedOrder.Status = newPlacedOrder.Status;
                                existingPlacedOrder.StopPrice = newPlacedOrder.StopPrice;
                                existingPlacedOrder.Symbol = newPlacedOrder.Symbol;
                                existingPlacedOrder.TimeInForce = newPlacedOrder.TimeInForce;
                                existingPlacedOrder.TransactionTime = newPlacedOrder.TransactionTime;
                                existingPlacedOrder.Type = newPlacedOrder.Type;
                            }

                            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

                            break;
                        }
                        catch
                        {
                            attempts--;

                            Log.Information($"Failed attempt to store Placed Order {newClientOrderId}; attempt: {attempts}");

                            if (attempts == 0)
                            {
                                throw;
                            }
                        }
                    }

                    this._logger.Information($"{placedOrder.Data.Symbol} @ {placedOrder.Data.Price:0.00}: Issuing sell of {placedOrder.Data.ExecutedQuantity} / {placedOrder.Data.OriginalQuantity}");
                }
            }
        }

        #endregion
    }
}
