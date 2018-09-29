﻿using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Caching
{
    /// <summary>Redis缓存</summary>
    public class FullRedis : Redis
    {
        #region 静态
        static FullRedis()
        {
            ObjectContainer.Current.AutoRegister<Redis, FullRedis>();
        }

        /// <summary>注册</summary>
        public static void Register() { }

        /// <summary>根据连接字符串创建</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static FullRedis Create(String config)
        {
            var rds = new FullRedis();
            rds.Init(config);

            return rds;
        }
        #endregion

        #region 属性
        /// <summary>性能计数器</summary>
        public PerfCounter Counter { get; set; } = new PerfCounter();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>重载执行，统计性能</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="write">是否写入操作</param>
        /// <returns></returns>
        public override T Execute<T>(Func<RedisClient, T> func, Boolean write = false)
        {
            var sw = Counter.StartCount();
            try
            {
                return base.Execute(func, write);
            }
            finally
            {
                Counter.StopCount(sw);
            }
        }
        #endregion

        #region 集合操作
        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IList<T> GetList<T>(String key) => new RedisList<T>(this, key);

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetDictionary<T>(String key) => new RedisHash<String, T>(this, key);

        /// <summary>获取队列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetQueue<T>(String key) => new RedisQueue<T>(this, key);

        /// <summary>获取Set</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override ICollection<T> GetSet<T>(String key) => new RedisSet<T>(this, key);
        #endregion

        #region 字符串操作
        /// <summary>附加字符串</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>返回字符串长度</returns>
        public virtual Int32 Append(String key, String value) => Execute(r => r.Execute<Int32>("APPEND", key, value), true);

        /// <summary>获取字符串区间</summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public virtual String GetRange(String key, Int32 start, Int32 end) => Execute(r => r.Execute<String>("GETRANGE", key, start, end));

        /// <summary>设置字符串区间</summary>
        /// <param name="key"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual String SetRange(String key, Int32 offset, String value) => Execute(r => r.Execute<String>("SETRANGE", key, offset, value), true);

        /// <summary>字符串长度</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Int32 StrLen(String key) => Execute(r => r.Execute<Int32>("STRLEN", key));
        #endregion

        #region 高级操作
        /// <summary>重命名指定键</summary>
        /// <param name="key"></param>
        /// <param name="newKey"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public virtual Boolean Rename(String key, String newKey, Boolean overwrite = true)
        {
            var cmd = overwrite ? "RENAME" : "RENAMENX";

            return Execute(r => r.Execute<Boolean>(cmd, key, newKey), true);
        }

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public virtual String[] Search(String pattern) => Execute(r => r.Execute<String[]>("KEYS", pattern));

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="pattern"></param>
        /// <param name="count"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual String[] Search(String pattern, Int32 count, ref Int32 position)
        {
            var p = position;
            var rs = Execute(r => r.Execute<Object[]>("SCAN", p, "MATCH", pattern + "", "COUNT", count));

            if (rs != null)
            {
                position = (rs[0] as Packet).ToStr().ToInt();

                var ps = rs[1] as Object[];
                var ss = ps.Select(e => (e as Packet).ToStr()).ToArray();
                return ss;
            }

            return null;
        }
        #endregion
    }
}