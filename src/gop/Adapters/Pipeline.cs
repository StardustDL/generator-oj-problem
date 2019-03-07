using System;
using System.Collections.Generic;

namespace gop.Adapters
{
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

        Dictionary<string, object> flags = new Dictionary<string, object>();

        public T GetFlag<T>(string id)
        {
            return (T)flags[id];
        }

        public void SetFlag<T>(string id, T value)
        {
            if (flags.ContainsKey(id))
                flags[id] = value;
            else flags.Add(id, value);
        }

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
