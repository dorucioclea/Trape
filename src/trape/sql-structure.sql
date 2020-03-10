--drop table binance_stream_tick
CREATE TABLE binance_stream_tick
(
	id bigserial not null,
	event text not null,
	event_time timestamptz not null,
	total_trades int8 not null,
	last_trade_id int8 not null,
	first_trade_id int8 not null,
	total_traded_quote_asset_volume numeric not null,
	total_traded_base_asset_volume numeric not null,
	low_price numeric not null,
	high_price numeric not null,
	open_price numeric not null,
	best_ask_quantity numeric not null,
	best_ask_price numeric not null,
	best_bid_quantity numeric not null,
	best_bid_price numeric not null,
	close_trades_quantity numeric not null,
	current_day_close_price numeric not null,
	prev_day_close_price numeric not null,
	weighted_average numeric not null,
	price_change_percentage numeric not null,
	price_change numeric not null,
	symbol text not null,
	statistics_open_time timestamptz not null,
	statistics_close_time timestamptz not null,
	PRIMARY KEY (id)
);
--DROP INDEX uq_bst_cs;
CREATE INDEX ix_bst_cs ON binance_stream_tick (event_time, symbol);

--DROP FUNCTION insert_binance_stream_tick;
CREATE OR REPLACE FUNCTION insert_binance_stream_tick (
	p_event text,
	p_event_time timestamptz,
	p_total_trades int8,
	p_last_trade_id int8,
	p_first_trade_id int8,
	p_total_traded_quote_asset_volume numeric,
	p_total_traded_base_asset_volume numeric,
	p_low_price numeric,
	p_high_price numeric,
	p_open_price numeric,
	p_best_ask_quantity numeric,
	p_best_ask_price numeric,
	p_best_bid_quantity numeric,
	p_best_bid_price numeric,
	p_close_trades_quantity numeric,
	p_current_day_close_price numeric,
	p_prev_day_close_price numeric,
	p_weighted_average numeric,
	p_price_change_percentage numeric,
	p_price_change numeric,
	p_symbol text,
	p_statistics_open_time timestamptz,
	p_statistics_close_time timestamptz
)
	RETURNS void AS
$$
BEGIN
	INSERT INTO binance_stream_tick (
		event,
		event_time,
		total_trades,
		last_trade_id,
		first_trade_id,
		total_traded_quote_asset_volume,
		total_traded_base_asset_volume,
		low_price,
		high_price,
		open_price,
		best_ask_quantity,
		best_ask_price,
		best_bid_quantity,
		best_bid_price,
		close_trades_quantity,
		current_day_close_price,
		prev_day_close_price,
		weighted_average,
		price_change_percentage,
		price_change,
		symbol,
		statistics_open_time,
		statistics_close_time
	) VALUES (
		p_event,
		p_event_time,
		p_total_trades,
		p_last_trade_id,
		p_first_trade_id,
		p_total_traded_quote_asset_volume,
		p_total_traded_base_asset_volume,
		p_low_price,
		p_high_price,
		p_open_price,
		p_best_ask_quantity,
		p_best_ask_price,
		p_best_bid_quantity,
		p_best_bid_price,
		p_close_trades_quantity,
		p_current_day_close_price,
		p_prev_day_close_price,
		p_weighted_average,
		p_price_change_percentage,
		p_price_change,
		p_symbol,
		p_statistics_open_time,
		p_statistics_close_time
	) ON CONFLICT DO NOTHING;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


select * from binance_stream_tick;



CREATE TABLE binance_stream_kline_data
(
	id bigserial not null,
	event text not null,
	event_time timestamptz not null,
	close numeric not null,
	close_time timestamptz not null,
	final boolean not null,
	first_trade_id int8 not null,
	high_price numeric not null,
	interval text not null,
	last_trade_id int8 not null,
	low_price numeric not null,
	open_price numeric not null,
	open_time timestamptz not null,
	quote_asset_volume numeric not null,
	symbol text not null,
	taker_buy_base_asset_volume numeric not null,
	taker_buy_quote_asset_volume numeric not null,
	trade_count int4 not null,
	volume numeric not null,	
	PRIMARY KEY (id)
);
--DROP INDEX uq_bst_cs;
CREATE INDEX ix_bskd_cs ON binance_stream_kline_data (event_time, symbol, interval);
CREATE UNIQUE INDEX uq_bskd_fi ON binance_stream_kline_data (first_trade_id, interval);


--DROP FUNCTION insert_binance_stream_kline_data;
CREATE OR REPLACE FUNCTION insert_binance_stream_kline_data (
	p_event text,
	p_event_time timestamptz,
	p_close numeric,
	p_close_time timestamptz,
	p_final boolean,
	p_first_trade_id int8,
	p_high_price numeric,
	p_interval text,
	p_last_trade_id int8,
	p_low_price numeric,
	p_open_price numeric,
	p_open_time timestamptz,
	p_quote_asset_volume numeric,
	p_symbol text,
	p_taker_buy_base_asset_volume numeric,
	p_taker_buy_quote_asset_volume numeric,
	p_trade_count int4,
	p_volume numeric
)
	RETURNS void AS
$$
BEGIN
	INSERT INTO binance_stream_kline_data (
		event,
		event_time,
		close,
		close_time,
		final,
		first_trade_id,
		high_price,
		interval,
		last_trade_id,
		low_price,
		open_price,
		open_time,
		quote_asset_volume,
		symbol,
		taker_buy_base_asset_volume,
		taker_buy_quote_asset_volume,
		trade_count,
		volume
	) VALUES (
		p_event,
		p_event_time,
		p_close,
		p_close_time,
		p_final,
		p_first_trade_id,
		p_high_price,
		p_interval,
		p_last_trade_id,
		p_low_price,
		p_open_price,
		p_open_time,
		p_quote_asset_volume,
		p_symbol,
		p_taker_buy_base_asset_volume,
		p_taker_buy_quote_asset_volume,
		p_trade_count,
		p_volume
	) ON CONFLICT (first_trade_id, interval) DO UPDATE SET
		event_time = p_event_time,
		close = p_close,
		close_time = p_close_time,
		final = p_final,
		high_price = p_high_price,
		last_trade_id = p_last_trade_id,
		low_price = p_low_price,
		quote_asset_volume = p_quote_asset_volume,
		taker_buy_base_asset_volume = p_taker_buy_base_asset_volume,
		taker_buy_quote_asset_volume = p_taker_buy_quote_asset_volume,
		trade_count = p_trade_count,
		volume = p_volume;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;




CREATE TABLE binance_book_tick
(
	update_id int8 not null,
	symbol text not null,
	event_time timestamptz not null,
	best_ask_price numeric not null,
	best_ask_quantity numeric not null,
	best_bid_price numeric not null,
	best_bid_quantity numeric not null,
	PRIMARY KEY (update_id)
);

CREATE INDEX ix_bbt_ets ON binance_book_tick (event_time, symbol);
CREATE INDEX ix_bbt_et ON binance_book_tick USING BRIN (event_time);

--DROP FUNCTION insert_binance_book_tick;
CREATE OR REPLACE FUNCTION insert_binance_book_tick (
	p_update_id int8,
	p_symbol text,
	p_event_time timestamptz,
	p_best_ask_price numeric,
	p_best_ask_quantity numeric,
	p_best_bid_price numeric,
	p_best_bid_quantity numeric
)
	RETURNS void AS
$$
BEGIN
	INSERT INTO binance_book_tick (
		update_id,
		symbol,
		event_time,
		best_ask_price,
		best_ask_quantity,
		best_bid_price,
		best_bid_quantity
	) VALUES (
		p_update_id,
		p_symbol,
		p_event_time,
		p_best_ask_price,
		p_best_ask_quantity,
		p_best_bid_price,
		p_best_bid_quantity
	) ON CONFLICT DO NOTHING;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


CREATE OR REPLACE FUNCTION cleanup_book_ticks()
RETURNS int4 AS
$$
	DECLARE i_deleted int4;
BEGIN
	WITH deleted AS (DELETE FROM binance_book_tick WHERE event_time < NOW() - INTERVAL '48 hours' RETURNING *)
	SELECT COUNT(*) INTO i_deleted FROM deleted;
	
	RETURN i_deleted;
	
END;
$$
LANGUAGE plpgsql VOLATILE STRICT;



CREATE OR REPLACE FUNCTION trends_3sec() 
RETURNS TABLE (
	r_symbol TEXT,
	r_5seconds NUMERIC,
	r_10seconds NUMERIC,
	r_15seconds NUMERIC,
	r_30seconds NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '5 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '10 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '15 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '30 seconds' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



CREATE OR REPLACE FUNCTION trends_15sec() 
RETURNS TABLE (
	r_symbol TEXT,
	r_45seconds NUMERIC,
	r_1minute NUMERIC,
	r_2minutes NUMERIC,
	r_3minutes NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '45 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 minute' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '2 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '3 minutes' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;




CREATE OR REPLACE FUNCTION trends_2min() 
RETURNS TABLE (
	r_symbol TEXT,
	r_5minutes NUMERIC,
	r_7minutes NUMERIC,
	r_10minutes NUMERIC,
	r_15minutes NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '5 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '7 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '10 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '15 minutes' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;




CREATE OR REPLACE FUNCTION trends_10min()
RETURNS TABLE (
	r_symbol TEXT,
	r_30minutes NUMERIC,
	r_1hour NUMERIC,
	r_2hours NUMERIC,
	r_3hours NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '30 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 hour' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '2 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '3 hours' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;




CREATE OR REPLACE FUNCTION trends_10min()
RETURNS TABLE (
	r_symbol TEXT,
	r_6hours NUMERIC,
	r_12hours NUMERIC,
	r_18hours NUMERIC,
	r_1day NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '6 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '12 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '18 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(best_ask_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 day' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;

