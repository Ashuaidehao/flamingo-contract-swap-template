﻿using System.ComponentModel;

namespace FlamingoSwapRouter
{
    partial class FlamingoSwapRouterContract
    {
        /// <summary>
        /// params: message, extend data
        /// </summary>
        [DisplayName("fault")]
        public static event FaultEvent onFault;
        public delegate void FaultEvent(string message, params object[] paras);

    }
}
