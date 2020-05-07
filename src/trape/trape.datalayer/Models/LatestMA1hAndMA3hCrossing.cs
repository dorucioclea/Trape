﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Has information when slope10m and slope30m last crossed
    /// </summary>
    public class LatestMA1hAndMA3hCrossing
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        [Column("symbol")]
        public string Symbol { get; }

        /// <summary>
        /// Time of crossing
        /// </summary>
        [Column("event_time")]
        public DateTime EventTime { get; }

        /// <summary>
        /// Slope 1h
        /// </summary>
        [Column("slope1h")]
        public decimal Slope1h { get; }

        /// <summary>
        /// Slope 3h
        /// </summary>
        [Column("slope3h")]
        public decimal Slope3h { get; }

        #endregion
    }
}
