using Binance.Net.Converters;
using Binance.Net.Objects;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Converters;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Interfaces;

namespace Binance.Net
{
    public partial class BinanceClient : RestClient, IBinanceClient
    {
        // Addresses
        private const string FuturesBaseAddress = "https://fapi.binance.com";
        private const string FuturesApi = "fapi";


        // Versions
        private const string FuturesVersion = "1";

        //Position
        private const string PositionRiskEndpoint = "positionRisk";

        /// <summary>
        /// Pings the Binance API
        /// </summary>
        /// <returns>True if successful ping, false if no response</returns>
        public CallResult<long> Ping(CancellationToken ct = default, bool futures = true) => PingAsync(ct, futures).Result;

        /// <summary>
        /// Pings the Binance API
        /// </summary>
        /// <returns>True if successful ping, false if no response</returns>
        public async Task<CallResult<long>> PingAsync(CancellationToken ct = default, bool futures = true)
        {
            var sw = Stopwatch.StartNew();
            var result = await SendRequest<object>(GetUrl(PingEndpoint, FuturesApi, FuturesVersion, futures), HttpMethod.Get, ct).ConfigureAwait(false);
            sw.Stop();
            return new CallResult<long>(result.Error == null ? sw.ElapsedMilliseconds : 0, result.Error);
        }


        /// <summary>
        /// Get's information about the exchange including rate limits and symbol list
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <param name="futures"></param>
        /// <returns>Exchange info</returns>
        public WebCallResult<BinanceExchangeInfo> GetExchangeInfo(CancellationToken ct = default, bool futures = true) => GetExchangeInfoAsync(ct, true).Result;

        /// <summary>
        /// Get's information about the exchange including rate limits and symbol list
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <param name="futures"></param>
        /// <returns>Exchange info</returns>
        public async Task<WebCallResult<BinanceExchangeInfo>> GetExchangeInfoAsync(CancellationToken ct = default, bool futures = true)
        {
            var exchangeInfoResult = await SendRequest<BinanceExchangeInfo>(GetUrl(ExchangeInfoEndpoint, FuturesApi, FuturesVersion, futures), HttpMethod.Get, ct).ConfigureAwait(false);
            if (!exchangeInfoResult)
                return exchangeInfoResult;

            exchangeInfo = exchangeInfoResult.Data;
            lastExchangeInfoUpdate = DateTime.UtcNow;
            log.Write(LogVerbosity.Info, "Trade rules updated");
            return exchangeInfoResult;
        }


        /// <summary>
        /// Gets the order book for the provided symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the order book for</param>
        /// <param name="limit">Max number of results</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The order book for the symbol</returns>
        public WebCallResult<BinanceOrderBook> GetOrderBook_Futures(string symbol, int? limit = null, CancellationToken ct = default) => GetOrderBookAsync_Futures(symbol, limit, ct).Result;

        /// <summary>
        /// Gets the order book for the provided symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the order book for</param>
        /// <param name="limit">Max number of results</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The order book for the symbol</returns>
        public async Task<WebCallResult<BinanceOrderBook>> GetOrderBookAsync_Futures(string symbol, int? limit = null, CancellationToken ct = default)
        {
            symbol.ValidateBinanceSymbol();
            limit?.ValidateIntValues(nameof(limit), 5, 10, 20, 50, 100, 500, 1000, 5000);
            var parameters = new Dictionary<string, object> { { "symbol", symbol } };
            parameters.AddOptionalParameter("limit", limit?.ToString());
            var result = await SendRequest<BinanceOrderBook>(GetUrl(OrderBookEndpoint, FuturesApi, FuturesVersion, true), HttpMethod.Get, ct, parameters).ConfigureAwait(false);
            if (result)
                result.Data.Symbol = symbol;
            return result;
        }


        /// <summary>
        /// Get candlestick data for the provided symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the data for</param>
        /// <param name="interval">The candlestick timespan</param>
        /// <param name="startTime">Start time to get candlestick data</param>
        /// <param name="endTime">End time to get candlestick data</param>
        /// <param name="limit">Max number of results</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The candlestick data for the provided symbol</returns>
        public WebCallResult<IEnumerable<BinanceKline>> GetKlines(string symbol, KlineInterval interval, DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default, bool futures = true) => GetKlinesAsync(symbol, interval, startTime, endTime, limit, ct, futures).Result;

        /// <summary>
        /// Get candlestick data for the provided symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the data for</param>
        /// <param name="interval">The candlestick timespan</param>
        /// <param name="startTime">Start time to get candlestick data</param>
        /// <param name="endTime">End time to get candlestick data</param>
        /// <param name="limit">Max number of results</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The candlestick data for the provided symbol</returns>
        public async Task<WebCallResult<IEnumerable<BinanceKline>>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default, bool futures = true)
        {
            symbol.ValidateBinanceSymbol();
            limit?.ValidateIntBetween(nameof(limit), 1, 1000);
            var parameters = new Dictionary<string, object> {
                { "symbol", symbol },
                { "interval", JsonConvert.SerializeObject(interval, new KlineIntervalConverter(false)) }
            };
            parameters.AddOptionalParameter("startTime", startTime != null ? ToUnixTimestamp(startTime.Value).ToString() : null);
            parameters.AddOptionalParameter("endTime", endTime != null ? ToUnixTimestamp(endTime.Value).ToString() : null);
            parameters.AddOptionalParameter("limit", limit?.ToString());

            return await SendRequest<IEnumerable<BinanceKline>>(GetUrl(KlinesEndpoint, FuturesApi, FuturesVersion, true), HttpMethod.Get, ct, parameters).ConfigureAwait(false);
        }


        public WebCallResult<BinancePlacedOrder> PlaceOrder(string symbol,OrderSide side,OrderType type,decimal? quantity = null,decimal? quoteOrderQuantity = null,string? newClientOrderId = null,decimal? price = null,TimeInForce? timeInForce = null,decimal? stopPrice = null,decimal? icebergQty = null,OrderResponseType? orderResponseType = null,int? receiveWindow = null,CancellationToken ct = default, bool futures = true) 
        => PlaceOrderAsync(symbol, side, type, quantity, quoteOrderQuantity, newClientOrderId, price, timeInForce, stopPrice, icebergQty, orderResponseType, receiveWindow, ct, futures).Result;

        /// <summary>
        /// Places a new order
        /// </summary>
        /// <param name="symbol">The symbol the order is for</param>
        /// <param name="side">The order side (buy/sell)</param>
        /// <param name="type">The order type</param>
        /// <param name="timeInForce">Lifetime of the order (GoodTillCancel/ImmediateOrCancel/FillOrKill)</param>
        /// <param name="quantity">The amount of the symbol</param>
        /// <param name="quoteOrderQuantity">The amount of the quote symbol. Only valid for market orders</param>
        /// <param name="price">The price to use</param>
        /// <param name="newClientOrderId">Unique id for order</param>
        /// <param name="stopPrice">Used for stop orders</param>
        /// <param name="icebergQty">Used for iceberg orders</param>
        /// <param name="orderResponseType">The type of response to receive</param>
        /// <param name="receiveWindow">The receive window for which this request is active. When the request takes longer than this to complete the server will reject the request</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="futures"></param>
        /// <returns>Id's for the placed order</returns>
        public async Task<WebCallResult<BinancePlacedOrder>> PlaceOrderAsync(string symbol,OrderSide side,OrderType type,decimal? quantity = null,decimal? quoteOrderQuantity = null,string? newClientOrderId = null,decimal? price = null,TimeInForce? timeInForce = null,decimal? stopPrice = null,decimal? icebergQty = null,OrderResponseType? orderResponseType = null,int? receiveWindow = null,CancellationToken ct = default, bool futures = true)
        {
            return await PlaceOrderInternal(GetUrl(NewOrderEndpoint, FuturesApi, FuturesVersion, true),
                symbol,
                side,
                type,
                quantity,
                quoteOrderQuantity,
                newClientOrderId,
                price,
                timeInForce,
                stopPrice,
                icebergQty,
                null,
                orderResponseType,
                receiveWindow,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancels a pending order
        /// </summary>
        /// <param name="symbol">The symbol the order is for</param>
        /// <param name="orderId">The order id of the order</param>
        /// <param name="origClientOrderId">The client order id of the order</param>
        /// <param name="newClientOrderId">The new client order id of the order</param>
        /// <param name="receiveWindow">The receive window for which this request is active. When the request takes longer than this to complete the server will reject the request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Id's for canceled order</returns>
        public WebCallResult<BinanceCanceledOrder> CancelOrder(string symbol, long? orderId = null, string? origClientOrderId = null, string? newClientOrderId = null, long? receiveWindow = null, CancellationToken ct = default,bool futures = true) => CancelOrderAsync(symbol, orderId, origClientOrderId, newClientOrderId, receiveWindow, ct,true).Result;

        /// <summary>
        /// Cancels a pending order
        /// </summary>
        /// <param name="symbol">The symbol the order is for</param>
        /// <param name="orderId">The order id of the order</param>
        /// <param name="origClientOrderId">The client order id of the order</param>
        /// <param name="newClientOrderId">Unique identifier for this cancel</param>
        /// <param name="receiveWindow">The receive window for which this request is active. When the request takes longer than this to complete the server will reject the request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Id's for canceled order</returns>
        public async Task<WebCallResult<BinanceCanceledOrder>> CancelOrderAsync(string symbol, long? orderId = null, string? origClientOrderId = null, string? newClientOrderId = null, long? receiveWindow = null, CancellationToken ct = default,bool futures= true)
        {
            symbol.ValidateBinanceSymbol();
            var timestampResult = await CheckAutoTimestamp(ct).ConfigureAwait(false);
            if (!timestampResult)
                return new WebCallResult<BinanceCanceledOrder>(timestampResult.ResponseStatusCode, timestampResult.ResponseHeaders, null, timestampResult.Error);

            if (!orderId.HasValue && string.IsNullOrEmpty(origClientOrderId))
                throw new ArgumentException("Either orderId or origClientOrderId must be sent");

            var parameters = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "timestamp", GetTimestamp() }
            };
            parameters.AddOptionalParameter("orderId", orderId?.ToString());
            parameters.AddOptionalParameter("origClientOrderId", origClientOrderId);
            parameters.AddOptionalParameter("newClientOrderId", newClientOrderId);
            parameters.AddOptionalParameter("recvWindow", receiveWindow?.ToString(CultureInfo.InvariantCulture) ?? defaultReceiveWindow.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));

            return await SendRequest<BinanceCanceledOrder>(GetUrl(CancelOrderEndpoint, FuturesApi, FuturesVersion,true), HttpMethod.Delete, ct, parameters, true).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the price of a symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the price for</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Price of symbol</returns>
        public WebCallResult<BinancePrice> GetPrice(string symbol, CancellationToken ct = default,bool futures = true) => GetPriceAsync(symbol, ct,true).Result;

        /// <summary>
        /// Gets the price of a symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the price for</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Price of symbol</returns>
        public async Task<WebCallResult<BinancePrice>> GetPriceAsync(string symbol, CancellationToken ct = default,bool futures = true)
        {
            symbol.ValidateBinanceSymbol();
            var parameters = new Dictionary<string, object>
            {
                { "symbol", symbol }
            };

            return await SendRequest<BinancePrice>(GetUrl(AllPricesEndpoint, FuturesApi, FuturesVersion,true), HttpMethod.Get, ct, parameters).ConfigureAwait(false);
        }


        /// <summary>
        /// Pings the Binance API
        /// </summary>
        /// <returns>True if successful ping, false if no response</returns>
        public CallResult<List<PositionData>> GetPositions(CancellationToken ct = default, bool futures = true) => GetPositionsAsync(ct, futures).Result;

        /// <summary>
        /// Pings the Binance API
        /// </summary>
        /// <returns>True if successful ping, false if no response</returns>
        public async Task<CallResult<List<PositionData>>> GetPositionsAsync(CancellationToken ct = default, bool futures = true)
        {
            var parameters = new Dictionary<string, object>
            {
              { "timestamp", GetTimestamp() },
//            { "recvWindow", 10000000 }
              //'recvWindow': 10000000
            };
            
            var result = await SendRequest<List<PositionData>>(GetUrl(PositionRiskEndpoint, FuturesApi, FuturesVersion, futures), HttpMethod.Get, ct, parameters, true).ConfigureAwait(false);

            return result;
        }



        #region -helpers-
        private Uri GetUrl(string endpoint, string api, string version, bool futures = true)
        {
            var result = $"{FuturesBaseAddress}/{api}/v{version}/{endpoint}";
            return new Uri(result);
        }
        #endregion



    }


    public class PositionData
    {
        //[JsonProperty(PropertyName = "entryPrice")]
        public decimal entryPrice { get; set; }

        //[JsonProperty(PropertyName = "marginType")]
        public string marginType { get; set; }

        //[JsonProperty(PropertyName = "isAutoAddMargin")]
        public string isAutoAddMargin { get; set; }

        //[JsonProperty(PropertyName = "isolatedMargin")]
        public string isolatedMargin { get; set; }

        //[JsonProperty(PropertyName = "leverage")]
        public string leverage { get; set; }

        //[JsonProperty(PropertyName = "liquidationPrice")]
        public string liquidationPrice { get; set; }

        //[JsonProperty(PropertyName = "markPrice")]
        public decimal markPrice { get; set; }

        //[JsonProperty(PropertyName = "maxNotionalValue")]
        public string maxNotionalValue { get; set; }

        //[JsonProperty(PropertyName = "positionAmt")]
        public string positionAmt { get; set; }

        //[JsonProperty(PropertyName = "symbol")]
        public string symbol { get; set; }

        //[JsonProperty(PropertyName = "unRealizedProfit")]
        public decimal unRealizedProfit { get; set; }

        //[JsonProperty(PropertyName = "positionSide")]
        public string positionSide { get; set; }
    }

}
