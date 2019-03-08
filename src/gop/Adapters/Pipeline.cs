using System;
using System.Collections.Generic;

namespace gop.Adapters
{
    public class DependencyInjection
    {
        readonly Dictionary<Type, object> pools = new Dictionary<Type, object>();

        public void Set<T>(T item)
        {
            pools.Add(typeof(T), item);
        }

        public void Replace<T>(T item)
        {
            var type = typeof(T);
            if (pools.ContainsKey(type)) pools[type] = item;
            else Set(item);
        }

        public T Get<T>()
        {
            return (T)pools[typeof(T)];
        }
    }

    public class PipelineResult<T>
    {
        public bool IsOk()
        {
            return Exception == null;
        }

        public bool IsErr()
        {
            return Exception != null;
        }

        public T Result { get; private set; }
        public Exception Exception { get; private set; }

        public PipelineResult(T result, Exception ex)
        {
            Exception = ex;
            Result = result;
        }
    }

    public class Pipeline<TOrigin, TResult> where TOrigin : class
    {
        string _token = null;

        public DependencyInjection Container { get; private set; } = new DependencyInjection();

        public void SetToken(string token)
        {
            if (_token != null) throw new Exception("Token has been setted.");
            _token = token;
        }

        public void CheckToken(string token)
        {
            if (_token != token) throw new Exception("Tokens do not meet.");
        }

        public TOrigin Current { get; private set; }

        public TResult Result { get; set; }

        public Exception Exception { get; set; }

        readonly List<Func<Pipeline<TOrigin, TResult>, TOrigin, TOrigin>> ops = new List<Func<Pipeline<TOrigin, TResult>, TOrigin, TOrigin>>();

        protected int Position
        {
            get; set;
        }

        public Pipeline(TOrigin origin)
        {
            Current = origin;
            Position = 0;
            Exception = null;
        }

        public Pipeline<TOrigin, TResult> Use(Func<Pipeline<TOrigin, TResult>, TOrigin, TOrigin> operation)
        {
            ops.Add(operation);
            return this;
        }

        public bool Step()
        {
            if (Exception != null || Position >= ops.Count) return false;

            TOrigin next = null;

            try
            {
                next = ops[Position](this, Current);
            }
            catch (Exception ex)
            {
                Exception = ex;
                return false;
            }

            Current = next;
            Position++;
            return true;
        }

        public PipelineResult<TResult> Consume()
        {
            while (Step()) ;
            return new PipelineResult<TResult>(Result, Exception);
        }
    }
}
