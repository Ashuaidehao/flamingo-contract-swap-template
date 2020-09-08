﻿using System;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

namespace FlamingoSwapFactory
{
    partial class FlamingoSwapFactoryContract : SmartContract
    {

        static readonly byte[] superAdmin = "AZaCs7GwthGy9fku2nFXtbrdKBRmrUQoFP".ToScriptHash();


        /// <summary>
        /// 收益地址的StoreKey
        /// </summary>
        private const string FeeToKey = "FeeTo";

        /// <summary>
        /// 交易对Map的StoreKey
        /// </summary>
        private const string ExchangeMapKey = "ExchangeMap";


        #region 通知

        /// <summary>
        /// params: tokenA,tokenB,exchangeContractHash
        /// </summary>
        [DisplayName("createExchange")]
        public static event Action<byte[], byte[], byte[]> onCreateExchange;

        /// <summary>
        /// params: tokenA,tokenB
        /// </summary>
        [DisplayName("removeExchange")]
        public static event Action<byte[], byte[]> onRemoveExchange;


        #endregion

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(superAdmin);
            }
            if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "createExchangePair")
                {
                    byte[] tokenA = (byte[])args[0];
                    byte[] tokenB = (byte[])args[1];
                    byte[] exchangeContractHash = (byte[])args[2];
                    return CreateExchangePair(tokenA, tokenB, exchangeContractHash);
                }
                if (method == "removeExchangePair")
                {
                    byte[] tokenA = (byte[])args[0];
                    byte[] tokenB = (byte[])args[1];
                    return RemoveExchangePair(tokenA, tokenB);
                }
                if (method == "getExchangePair")
                {
                    byte[] tokenA = (byte[])args[0];
                    byte[] tokenB = (byte[])args[1];
                    return GetExchangePair(tokenA, tokenB);
                }
                if (method == "setFeeTo")
                {
                    return SetFeeTo((byte[])args[0]);
                }
                if (method == "getFeeTo")
                {
                    return GetFeeTo();
                }
                //转发
                //{
                //    StorageMap exchangeMap = Storage.CurrentContext.CreateMap("exchange");
                //    byte[] exchangeContractHash = exchangeMap.Get(tokenHash.Concat(assetHash));
                //    if (exchangeContractHash.Length == 0)
                //        throw new InvalidOperationException("exchangeContractHash inexistence");
                //    deleDyncall _dyncall = (deleDyncall)exchangeContractHash.ToDelegate();
                //    return _dyncall(operation, _args);
                //}
            }
            return false;
        }



        /// <summary>
        /// 增加nep5资产的exchange合约映射
        /// </summary>
        /// <param name="tokenA">Nep5 tokenA</param>
        /// <param name="tokenB">Nep5 tokenB</param>
        /// <param name="exchangeContractHash"></param>
        /// <returns></returns>
        public static bool CreateExchangePair(byte[] tokenA, byte[] tokenB, byte[] exchangeContractHash)
        {
            Assert(Runtime.CheckWitness(superAdmin), "Forbidden");
            Assert(tokenA != tokenB, "Identical Address", tokenA);
            AssertAddress(tokenA, nameof(tokenA));
            AssertAddress(tokenB, nameof(tokenB));
            AssertAddress(exchangeContractHash, nameof(exchangeContractHash));

            var pair = GetTokenPair(tokenA, tokenB);
            StorageMap exchangeMap = Storage.CurrentContext.CreateMap(ExchangeMapKey);
            var key = pair.Token0.Concat(pair.Token1);
            var value = exchangeMap.Get(key);
            Assert(value.Length == 0, "Exchange had created");

            exchangeMap.Put(key, exchangeContractHash);
            onCreateExchange(tokenA, tokenB, exchangeContractHash);
            return true;
        }

        /// <summary>
        /// 删除nep5资产的exchange合约映射
        /// </summary>
        /// <param name="tokenA"></param>
        /// <param name="tokenB"></param>
        /// <returns></returns>
        public static bool RemoveExchangePair(byte[] tokenA, byte[] tokenB)
        {
            Assert(Runtime.CheckWitness(superAdmin), "FORBIDDEN");
            AssertAddress(tokenA, nameof(tokenA));
            AssertAddress(tokenB, nameof(tokenB));

            StorageMap exchangeMap = Storage.CurrentContext.CreateMap(ExchangeMapKey);
            var pair = GetTokenPair(tokenA, tokenB);
            var key = pair.Token0.Concat(pair.Token1);
            var value = exchangeMap.Get(key);
            if (value.Length > 0)
            {
                exchangeMap.Delete(key);
                onRemoveExchange(tokenA, tokenB);
            }
            return true;
        }




        /// <summary>
        /// 获得nep5资产的exchange合约映射
        /// </summary>
        /// <param name="tokenA"></param>
        /// <param name="tokenB"></param>
        /// <returns></returns>
        public static byte[] GetExchangePair(byte[] tokenA, byte[] tokenB)
        {
            var pair = GetTokenPair(tokenA, tokenB);
            StorageMap exchangeMap = Storage.CurrentContext.CreateMap(ExchangeMapKey);
            return exchangeMap.Get(pair.Token0.Concat(pair.Token1));
        }


        /// <summary>
        /// 获取手续费收益地址
        /// </summary>
        /// <returns></returns>
        private static byte[] GetFeeTo()
        {
            return Storage.Get(FeeToKey);
        }


        /// <summary>
        /// 设置手续费收益地址
        /// </summary>
        /// <param name="feeTo"></param>
        /// <returns></returns>
        private static bool SetFeeTo(byte[] feeTo)
        {
            Assert(Runtime.CheckWitness(superAdmin), "FORBIDDEN");
            Storage.Put(FeeToKey, feeTo);
            return true;
        }


        /// <summary>
        /// token排序
        /// </summary>
        /// <param name="tokenA"></param>
        /// <param name="tokenB"></param>
        /// <returns></returns>
        private static TokenPair GetTokenPair(byte[] tokenA, byte[] tokenB)
        {
            return tokenA.AsBigInteger() < tokenB.AsBigInteger()
                ? new TokenPair { Token0 = tokenA, Token1 = tokenB }
                : new TokenPair { Token0 = tokenB, Token1 = tokenA };
        }
    }
}
